#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEngine;

/// <summary>
/// UI_Image 및 그 파생 클래스(UI_Button 등) 공용 인스펙터.
/// [ShowIf] 마커 attribute를 리플렉션으로 읽어 조건부 필드를 표시/숨김한다.
/// PropertyDrawer가 아니라 에디터에서 직접 처리하므로 [ChainedVector3] 등 기존 드로어와 충돌하지 않고,
/// editorForChildClasses:true 덕분에 파생 클래스에도 동일 로직이 적용된다(파생 에디터가 조건부를 누락하던 문제 차단).
/// </summary>
[CustomEditor(typeof(UI_Image), true)]
[CanEditMultipleObjects]
public class UI_ImageEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var prop = serializedObject.GetIterator();
        prop.NextVisible(true); // m_Script
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(prop);

        while (prop.NextVisible(false))
        {
            var showIf = GetShowIf(prop.name);
            if (showIf != null)
            {
                // 조건 bool이 true일 때만, 토글 바로 아래에 한 단계 들여써서 표시
                var cond = serializedObject.FindProperty(showIf.Condition);
                bool visible = cond != null
                               && cond.propertyType == SerializedPropertyType.Boolean
                               && cond.boolValue;
                if (!visible) continue;

                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(prop, true);
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUILayout.PropertyField(prop, true);
            }
        }

        DrawExtras();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>파생 에디터가 Apply 버튼·배열 자동채움 등 커스텀 GUI를 덧붙이는 훅 (base 호출로 Cover Fill 버튼 유지).</summary>
    protected virtual void DrawExtras()
    {
        // coverFill이 켜진 동안만 에디트 모드 미리보기 버튼 제공 (UI_Image는 ExecuteAlways가 아니라 자동 적용이 없음)
        var coverFill = serializedObject.FindProperty("coverFill");
        if (coverFill != null && coverFill.boolValue && GUILayout.Button("Apply Cover Fill (Editor Preview)"))
        {
            foreach (var t in targets)
            {
                if (t is not UI_Image img) continue;
                Undo.RecordObject(img.transform, "UI_Image ApplyCoverFill");
                img.ApplyCoverFill();
                EditorUtility.SetDirty(img.transform);
            }
        }
    }

    /// <summary>최상위 직렬화 필드명으로 [ShowIf]를 조회 (상속 필드 포함). 없으면 null.</summary>
    private ShowIfAttribute GetShowIf(string fieldName)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        for (var t = target.GetType(); t != null && t != typeof(object); t = t.BaseType)
        {
            var fi = t.GetField(fieldName, flags);
            if (fi != null) return fi.GetCustomAttribute<ShowIfAttribute>();
        }
        return null;
    }
}

[CustomPropertyDrawer(typeof(UI_Image.Editor_Resizer))]
public class UI_Image_EditorResizerDrawer : PropertyDrawer
{
    private const float ButtonHeight = 22f;
    private const float ButtonSpace = 2f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = foldoutRect.yMax + EditorGUIUtility.standardVerticalSpacing;

            var endProp = property.GetEndProperty();
            var iter = property.Copy();
            if (iter.NextVisible(true))
            {
                while (!SerializedProperty.EqualContents(iter, endProp))
                {
                    float h = EditorGUI.GetPropertyHeight(iter, true);
                    var r = new Rect(position.x, y, position.width, h);
                    EditorGUI.PropertyField(r, iter, true);
                    y += h + EditorGUIUtility.standardVerticalSpacing;
                    if (!iter.NextVisible(false)) break;
                }
            }
            EditorGUI.indentLevel--;

            y += ButtonSpace;
            var btnRect = EditorGUI.IndentedRect(new Rect(position.x, y, position.width, ButtonHeight));
            if (GUI.Button(btnRect, "Apply Resize"))
            {
                foreach (var t in property.serializedObject.targetObjects)
                {
                    if (t is UI_Image ui_image)
                    {
                        ui_image.ApplyResize();
                    }
                }
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return h;

        var endProp = property.GetEndProperty();
        var iter = property.Copy();
        if (iter.NextVisible(true))
        {
            while (!SerializedProperty.EqualContents(iter, endProp))
            {
                h += EditorGUI.GetPropertyHeight(iter, true) + EditorGUIUtility.standardVerticalSpacing;
                if (!iter.NextVisible(false)) break;
            }
        }
        h += ButtonSpace + ButtonHeight;
        return h;
    }
}
#endif
