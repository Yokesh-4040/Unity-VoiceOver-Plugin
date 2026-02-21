using UnityEngine;
using UnityEditor;

namespace FF.Voiceover.Editor
{
    [InitializeOnLoad]
    public static class VoiceoverInspectorIntegration
    {
        static VoiceoverInspectorIntegration()
        {
            EditorApplication.contextualPropertyMenu += OnPropertyContextMenu;
        }

        private static void OnPropertyContextMenu(GenericMenu menu, SerializedProperty property)
        {
            // Debug logging to see what properties are being clicked
            // Debug.Log($"[Voiceover] Context Menu: {property.name} ({property.type})");

            /*
             * Note: property.type usually returns "PPtr<$AudioClip>" for AudioClip fields.
             * We check for "AudioClip" as well just in case.
             */
            if (property.propertyType == SerializedPropertyType.ObjectReference && 
                (property.type == "PPtr<$AudioClip>" || property.type == "AudioClip"))
            {
                var propertyCopy = property.Copy();
                menu.AddItem(new GUIContent("Generate Voiceover VO"), false, () => 
                {
                    VoiceoverQuickGen.Init(propertyCopy);
                });
            }
        }
    }
}
