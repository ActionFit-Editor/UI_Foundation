#if UNITY_EDITOR
using UnityEditor;

[CustomEditor(typeof(UI_Text))]
[CanEditMultipleObjects]
public class UI_TextEditor : Editor
{
    private SerializedProperty _script;
    private SerializedProperty _isLocalizeText, _localizedString;
    private SerializedProperty _isResizeText, _resizePadding;
    private SerializedProperty _isSettingFace, _faceColor, _faceDilate;
    private SerializedProperty _isSettingOutline, _outlineColor, _outlineWidth;
    private SerializedProperty _isSettingUnderlay, _underlayColor, _underlayOffsetX, _underlayOffsetY, _underlayDilate, _underlaySoftness;

    private void OnEnable()
    {
        _script = serializedObject.FindProperty("m_Script");
        _isLocalizeText = serializedObject.FindProperty("isLocalizeText");
        _localizedString = serializedObject.FindProperty("localizedString");
        _isResizeText = serializedObject.FindProperty("isResizeText");
        _resizePadding = serializedObject.FindProperty("resizePadding");
        _isSettingFace = serializedObject.FindProperty("isSettingFace");
        _faceColor = serializedObject.FindProperty("faceColor");
        _faceDilate = serializedObject.FindProperty("faceDilate");
        _isSettingOutline = serializedObject.FindProperty("isSettingOutline");
        _outlineColor = serializedObject.FindProperty("outlineColor");
        _outlineWidth = serializedObject.FindProperty("outlineWidth");
        _isSettingUnderlay = serializedObject.FindProperty("isSettingUnderlay");
        _underlayColor = serializedObject.FindProperty("underlayColor");
        _underlayOffsetX = serializedObject.FindProperty("underlayOffsetX");
        _underlayOffsetY = serializedObject.FindProperty("underlayOffsetY");
        _underlayDilate = serializedObject.FindProperty("underlayDilate");
        _underlaySoftness = serializedObject.FindProperty("underlaySoftness");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(_script);

        // Localization — isLocalizeText ON일 때만 LocalizedString 피커(테이블+엔트리) 표시. 런타임 OnEnable에서 적용+언어변경 자동 갱신.
        EditorGUILayout.PropertyField(_isLocalizeText);
        if (_isLocalizeText.boolValue) Indented(_localizedString);

        // Resize
        EditorGUILayout.PropertyField(_isResizeText);
        if (_isResizeText.boolValue) Indented(_resizePadding);

        // Face — ON일 때만 색/dilate 표시
        EditorGUILayout.PropertyField(_isSettingFace);
        if (_isSettingFace.boolValue)
        {
            Indented(_faceColor);
            Indented(_faceDilate);
        }

        // Outline — ON일 때만 색/두께 표시
        EditorGUILayout.PropertyField(_isSettingOutline);
        if (_isSettingOutline.boolValue)
        {
            Indented(_outlineColor);
            Indented(_outlineWidth);
        }

        // Underlay(그림자) — ON일 때만 색/오프셋/Dilate/Softness 표시
        EditorGUILayout.PropertyField(_isSettingUnderlay);
        if (_isSettingUnderlay.boolValue)
        {
            Indented(_underlayColor);
            Indented(_underlayOffsetX);
            Indented(_underlayOffsetY);
            Indented(_underlayDilate);
            Indented(_underlaySoftness);
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
