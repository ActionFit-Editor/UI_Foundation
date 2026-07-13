#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UI_Button 인스펙터. 조건부 필드 표시(useDisableSprite/useDisableColor/useDisableTextColor/useEnableAnimation
/// 및 상속받은 coverFill/setMaxSize/updateAspectRatio)는 모두 베이스(UI_ImageEditor)의 [ShowIf] 처리에 위임한다.
/// 여기서는 UI_Button 전용 편의 기능만 덧붙인다: useDisableColor/TextOutline이 켜져 있고 대상 배열이 비어 있으면
/// 자식 계층에서 Image/TMP를 자동 수집.
/// </summary>
[CustomEditor(typeof(UI_Button))]
[CanEditMultipleObjects]
public class UI_ButtonEditor : UI_ImageEditor
{
    protected override void DrawExtras()
    {
        base.DrawExtras(); // 공용 (Apply Cover Fill 버튼 등)

        var useColor = serializedObject.FindProperty("useDisableColor");
        var targetImages = serializedObject.FindProperty("targetImages");
        if (useColor != null && useColor.boolValue && targetImages != null && targetImages.arraySize == 0)
            AutoFill(targetImages, (target as UI_Button)?.GetComponentsInChildren<Image>(true));

        var useText = serializedObject.FindProperty("useDisableTextOutlineColor");
        if (useText == null) useText = serializedObject.FindProperty("useDisableTextColor");
        var targetTexts = serializedObject.FindProperty("targetTexts");
        if (useText != null && useText.boolValue && targetTexts != null && targetTexts.arraySize == 0)
            AutoFill(targetTexts, (target as UI_Button)?.GetComponentsInChildren<TextMeshProUGUI>(true));

        var preview = serializedObject.FindProperty("editorDisablePreview");
        if (!Application.isPlaying && preview != null && serializedObject.ApplyModifiedProperties())
        {
            foreach (var t in targets)
            {
                if (t is not UI_Button button) continue;
                button.ApplyEditorDisablePreview();
                EditorUtility.SetDirty(button);
            }
            serializedObject.Update();
        }
    }

    // 배열 SerializedProperty를 주어진 컴포넌트들로 채움 (토글 ON 직후 비어 있을 때 자동 수집)
    private static void AutoFill(SerializedProperty arrayProp, Component[] items)
    {
        if (items == null) return;
        arrayProp.arraySize = items.Length;
        for (int i = 0; i < items.Length; i++)
            arrayProp.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
    }
}
#endif
