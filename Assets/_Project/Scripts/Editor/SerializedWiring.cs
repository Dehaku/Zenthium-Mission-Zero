using System;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Zenthium.Editor
{
    /// <summary>
    /// Sets serialized fields from setup commands, the way the Inspector would, so private fields stay private.
    /// </summary>
    internal static class SerializedWiring
    {
        public static void Wire(Object component, Action<SerializedObject> assign)
        {
            var serialized = new SerializedObject(component);
            assign(serialized);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static void SetReference(SerializedObject serialized, string field, Object value)
        {
            Find(serialized, field).objectReferenceValue = value;
        }

        public static SerializedProperty Find(SerializedObject serialized, string field)
        {
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null)
            {
                throw new InvalidOperationException(
                    $"{serialized.targetObject.GetType().Name} has no serialized field '{field}'. "
                    + "Update the setup command's field names to match it.");
            }

            return property;
        }
    }
}
