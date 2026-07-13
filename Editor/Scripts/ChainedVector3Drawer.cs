using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ChainedVector3Attribute))]
public class ChainedVector3Drawer : PropertyDrawer
{
    private static bool isChained = true;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float labelWidth = EditorGUIUtility.labelWidth;
        const float chainIconWidth = 20f;

        var labelRect = new Rect(position.x, position.y, labelWidth, position.height);
        var chainButtonRect = new Rect(labelRect.xMax, position.y, chainIconWidth, position.height);
        var fieldRect = new Rect(
            chainButtonRect.xMax,
            position.y,
            position.width - labelWidth - chainIconWidth,
            position.height);

        EditorGUI.PrefixLabel(labelRect, label);

        GUIContent buttonContent = EditorGUIUtility.IconContent(isChained ? "d_Linked" : "d_Unlinked");
        if (GUI.Button(chainButtonRect, buttonContent, EditorStyles.iconButton))
        {
            isChained = !isChained;
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        EditorGUI.BeginChangeCheck();
        Vector3 previousValue = property.vector3Value;
        Vector3 newValue = EditorGUI.Vector3Field(fieldRect, GUIContent.none, previousValue);

        if (EditorGUI.EndChangeCheck())
        {
            if (isChained)
            {
                newValue = GetChainedValue(previousValue, newValue);
            }

            property.vector3Value = newValue;
            GUI.changed = true;
            property.serializedObject.ApplyModifiedProperties();
        }

        EditorGUI.EndProperty();
    }

    private static Vector3 GetChainedValue(Vector3 previousValue, Vector3 newValue)
    {
        Vector3 change = newValue - previousValue;
        if (change == Vector3.zero) return newValue;

        if (previousValue == Vector3.zero)
        {
            float changedValue = change.x != 0f
                ? newValue.x
                : change.y != 0f
                    ? newValue.y
                    : newValue.z;
            return new Vector3(changedValue, changedValue, changedValue);
        }

        float ratio = 1f;
        if (previousValue.x != 0f && change.x != 0f) ratio = newValue.x / previousValue.x;
        else if (previousValue.y != 0f && change.y != 0f) ratio = newValue.y / previousValue.y;
        else if (previousValue.z != 0f && change.z != 0f) ratio = newValue.z / previousValue.z;
        return previousValue * ratio;
    }
}
