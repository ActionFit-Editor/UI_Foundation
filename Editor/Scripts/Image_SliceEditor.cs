#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.UI;
using UnityEngine.UI;

/// <summary>
/// Image_Slice 인스펙터. Unity 기본 Image 인스펙터(Sprite/Color/Type/Fill 등) 위에 isSliceImage 토글을 추가로 그린다.
/// </summary>
[CustomEditor(typeof(Image_Slice))]
[CanEditMultipleObjects]
public class Image_SliceEditor : ImageEditor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI(); // 기본 Image GUI (Sprite/Color/Material/RaycastTarget/Type/Fill...)

        serializedObject.Update();
        SerializedProperty sliceProperty = serializedObject.FindProperty("isSliceImage");
        SerializedProperty typeProperty = serializedObject.FindProperty("m_Type");
        SerializedProperty fillMethodProperty = serializedObject.FindProperty("m_FillMethod");
        EditorGUILayout.PropertyField(sliceProperty);

        bool isFilled = !typeProperty.hasMultipleDifferentValues
            && typeProperty.enumValueIndex == (int)Image.Type.Filled;
        bool isLinear = !fillMethodProperty.hasMultipleDifferentValues
            && fillMethodProperty.enumValueIndex is (int)Image.FillMethod.Horizontal or (int)Image.FillMethod.Vertical;
        if (sliceProperty.boolValue && isFilled && isLinear)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_FillCenter"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("m_PixelsPerUnitMultiplier"));
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.HelpBox(
            "isSliceImage: Type=Filled + 가로/세로 Fill에서 9-slice 테두리를 유지하며 채웁니다. (방사형 Radial은 미지원 → 기본 동작)",
            MessageType.None);
        serializedObject.ApplyModifiedProperties();
    }
}
#endif
