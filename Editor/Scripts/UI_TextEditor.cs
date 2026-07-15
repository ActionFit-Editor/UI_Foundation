#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(UI_Text))]
[CanEditMultipleObjects]
public class UI_TextEditor : Editor
{
    private SerializedProperty _script;
    private SerializedProperty _isLocalizeText, _localizedString;
    private SerializedProperty _isResizeText, _resizePadding;
    private SerializedProperty _isSpriteAsset, _sprite, _spriteGlyphSettings;
    private SerializedProperty _spriteGlyphInitialized, _overrideGlyphRect;
    private SerializedProperty _glyphRectX, _glyphRectY, _glyphRectWidth, _glyphRectHeight;
    private SerializedProperty _glyphWidth, _glyphHeight, _bearingX, _bearingY, _advance, _scale;
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
        _isSpriteAsset = serializedObject.FindProperty("isSpriteAsset");
        _sprite = serializedObject.FindProperty("sprite");
        _spriteGlyphSettings = serializedObject.FindProperty("spriteGlyphSettings");
        _spriteGlyphInitialized = _spriteGlyphSettings.FindPropertyRelative("initialized");
        _overrideGlyphRect = _spriteGlyphSettings.FindPropertyRelative("overrideGlyphRect");
        _glyphRectX = _spriteGlyphSettings.FindPropertyRelative("glyphRectX");
        _glyphRectY = _spriteGlyphSettings.FindPropertyRelative("glyphRectY");
        _glyphRectWidth = _spriteGlyphSettings.FindPropertyRelative("glyphRectWidth");
        _glyphRectHeight = _spriteGlyphSettings.FindPropertyRelative("glyphRectHeight");
        _glyphWidth = _spriteGlyphSettings.FindPropertyRelative("glyphWidth");
        _glyphHeight = _spriteGlyphSettings.FindPropertyRelative("glyphHeight");
        _bearingX = _spriteGlyphSettings.FindPropertyRelative("bearingX");
        _bearingY = _spriteGlyphSettings.FindPropertyRelative("bearingY");
        _advance = _spriteGlyphSettings.FindPropertyRelative("advance");
        _scale = _spriteGlyphSettings.FindPropertyRelative("scale");
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

        EditorGUI.BeginChangeCheck();

        // Sprite — 프로젝트 Sprite로 런타임 TMP Sprite Asset과 Editor preview를 생성.
        EditorGUILayout.PropertyField(_isSpriteAsset, new GUIContent("Is Sprite Asset"));
        bool spriteReferenceChanged = false;
        bool glyphSettingsChanged = false;
        bool resetSpriteSettings = false;
        if (_isSpriteAsset.boolValue)
        {
            EditorGUI.indentLevel++;
            UnityEngine.Object spriteBefore = _sprite.objectReferenceValue;
            EditorGUILayout.PropertyField(_sprite, new GUIContent("Sprite"));
            spriteReferenceChanged = spriteBefore != _sprite.objectReferenceValue;

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(_overrideGlyphRect, new GUIContent("Override Glyph Rect"));
            using (new EditorGUI.DisabledScope(!_overrideGlyphRect.boolValue))
            {
                EditorGUILayout.LabelField("Glyph Rect");
                DrawPair(_glyphRectX, "X", _glyphRectY, "Y");
                DrawPair(_glyphRectWidth, "W", _glyphRectHeight, "H");
            }

            EditorGUILayout.LabelField("Glyph Metrics");
            DrawPair(_glyphWidth, "W", _glyphHeight, "H");
            DrawPair(_bearingX, "BX", _bearingY, "BY");
            DrawPair(_advance, "AD", _scale, "Scale");
            glyphSettingsChanged = EditorGUI.EndChangeCheck();

            using (new EditorGUI.DisabledScope(_sprite.objectReferenceValue == null && !_sprite.hasMultipleDifferentValues))
                resetSpriteSettings = GUILayout.Button("Reset Glyph Settings From Sprite");
            EditorGUI.indentLevel--;
        }

        if (glyphSettingsChanged) _spriteGlyphInitialized.boolValue = true;

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

        bool previewChanged = EditorGUI.EndChangeCheck();
        bool applied = serializedObject.ApplyModifiedProperties();
        if (resetSpriteSettings || (spriteReferenceChanged && applied))
        {
            ResetSpriteGlyphSettingsForTargets();
            previewChanged = true;
            applied = true;
        }

        if (previewChanged && applied)
        {
            foreach (UnityEngine.Object currentTarget in targets)
            {
                if (currentTarget is UI_Text text)
                {
                    if (Application.isPlaying) text.ApplyRuntimeSpriteAsset();
                    else UI_TextEditorPreviewCoordinator.RequestRefresh(text);
                }
            }
        }
    }

    private static void Indented(SerializedProperty prop)
    {
        EditorGUI.indentLevel++;
        EditorGUILayout.PropertyField(prop);
        EditorGUI.indentLevel--;
    }

    private static void DrawPair(
        SerializedProperty left,
        string leftLabel,
        SerializedProperty right,
        string rightLabel)
    {
        Rect row = EditorGUILayout.GetControlRect();
        float gap = 4f;
        float width = (row.width - gap) * 0.5f;
        EditorGUI.PropertyField(new Rect(row.x, row.y, width, row.height), left, new GUIContent(leftLabel));
        EditorGUI.PropertyField(new Rect(row.x + width + gap, row.y, width, row.height), right, new GUIContent(rightLabel));
    }

    private void ResetSpriteGlyphSettingsForTargets()
    {
        foreach (UnityEngine.Object currentTarget in targets)
        {
            if (currentTarget is not UI_Text text) continue;
            Undo.RecordObject(text, "Reset UI Text Sprite Glyph Settings");
            text.ResetRuntimeSpriteGlyphSettings();
            EditorUtility.SetDirty(text);
        }
        serializedObject.Update();
    }
}
#endif
