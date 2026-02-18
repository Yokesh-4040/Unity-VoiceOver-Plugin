using UnityEngine;
using UnityEditor;

namespace FF.ElevenLabs.Editor.Popups
{
    public class CustomVoicePopup : EditorWindow
    {
        private string voiceId = "";
        private string voiceName = "";
        private System.Action<string, string> onAdd;

        public static void Init(System.Action<string, string> onAddCallback)
        {
            CustomVoicePopup window = ScriptableObject.CreateInstance<CustomVoicePopup>();
            window.titleContent = new GUIContent("Add Custom Voice");
            window.onAdd = onAddCallback;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 150);
            window.ShowUtility();
        }

        void OnGUI()
        {
            GUILayout.Label("Add Custom Voice", EditorStyles.boldLabel);
            voiceName = EditorGUILayout.TextField("Voice Name", voiceName);
            voiceId = EditorGUILayout.TextField("Voice ID", voiceId);
            
            GUILayout.Space(20);
            if (GUILayout.Button("Add"))
            {
                onAdd?.Invoke(voiceId, voiceName);
                Close();
            }
        }
    }

    public class ImportVoiceStepsPopup : EditorWindow
    {
        public enum ImportMode { SimpleLines, ScriptFormat, CSV }
        private string textToImport = "";
        private ImportMode mode = ImportMode.SimpleLines;
        private System.Action<string, ImportMode> onImport;

        public static void Init(System.Action<string, ImportMode> onCallback)
        {
            ImportVoiceStepsPopup window = ScriptableObject.CreateInstance<ImportVoiceStepsPopup>();
            window.titleContent = new GUIContent("Import Steps");
            window.onImport = onCallback;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 350);
            window.ShowUtility();
        }

        void OnGUI()
        {
            GUILayout.Label("Import Steps", EditorStyles.boldLabel);
            if (GUILayout.Button("Load from File"))
            {
                string path = EditorUtility.OpenFilePanel("Open Script", "", "csv,txt");
                if (!string.IsNullOrEmpty(path))
                {
                    textToImport = System.IO.File.ReadAllText(path);
                    if (path.EndsWith(".csv")) mode = ImportMode.CSV;
                }
            }
            
            textToImport = EditorGUILayout.TextArea(textToImport, GUILayout.Height(150));
            mode = (ImportMode)EditorGUILayout.EnumPopup("Mode", mode);
            
            if (GUILayout.Button("Import"))
            {
                onImport?.Invoke(textToImport, mode);
                Close();
            }
        }
    }
}
