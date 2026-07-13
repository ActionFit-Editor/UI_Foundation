#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// UI_Mask / UI_Mask2D (UI_MaskBase 파생) 공통 인스펙터 — isAnimationMask를 켜야 애니 설정이 노출됩니다.
/// targetRect를 지정하면 펼침 치수가 그 Rect를 자동 추종하므로 expandedHeight/Width 수동 입력은 숨깁니다.
/// (UI_Text의 isSettingOutline 조건부 노출 패턴과 동일.)
/// </summary>
[CustomEditor(typeof(UI_MaskBase), true)]
[CanEditMultipleObjects]
public class UI_MaskBaseEditor : Editor
{
    private SerializedProperty _script;
    private SerializedProperty _isAnimationMask, _animPivot, _targetRect, _expandedHeight, _expandedWidth, _animDuration, _animEase;

    private void OnEnable()
    {
        _script = serializedObject.FindProperty("m_Script");
        _isAnimationMask = serializedObject.FindProperty("isAnimationMask");
        _animPivot = serializedObject.FindProperty("animPivot");
        _targetRect = serializedObject.FindProperty("targetRect");
        _expandedHeight = serializedObject.FindProperty("expandedHeight");
        _expandedWidth = serializedObject.FindProperty("expandedWidth");
        _animDuration = serializedObject.FindProperty("animDuration");
        _animEase = serializedObject.FindProperty("animEase");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(_script);

        // 애니 설정(아래 게이트로 노출)을 제외한 나머지 직렬화 필드는 기본 표시
        DrawPropertiesExcluding(serializedObject, "m_Script", "isAnimationMask",
            "animPivot", "targetRect", "expandedHeight", "expandedWidth", "animDuration", "animEase");

        // IsAnimationMask 체크 시에만 애니 설정 노출
        EditorGUILayout.PropertyField(_isAnimationMask);
        if (_isAnimationMask.boolValue)
        {
            Indented(_animPivot);
            Indented(_targetRect);
            // targetRect 지정 시 치수는 자동 추종 → 수동 입력 숨김
            if (_targetRect.objectReferenceValue == null)
            {
                Indented(_expandedHeight);
                Indented(_expandedWidth);
            }
            Indented(_animDuration);
            Indented(_animEase);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void Indented(SerializedProperty prop)
    {
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(prop);
        EditorGUI.indentLevel--;
    }
}
#endif
