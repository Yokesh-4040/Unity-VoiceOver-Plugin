using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using FF.Voiceover;

namespace FF.Voiceover.Editor
{
    public class VoiceoverQuickGen : EditorWindow
    {
        private SerializedProperty targetProperty;
        private string textToGenerate = "Hello, this is a test voice over.";
        private int selectedVoiceIndex = 0;
        private List<Voice> voices;
        private string[] voiceNames;
        private bool isGenerating = false;

        public static void Init(SerializedProperty property)
        {
            VoiceoverQuickGen window = GetWindow<VoiceoverQuickGen>(true, "Quick Voice Generator", true);
            window.targetProperty = property;
            window.minSize = new Vector2(400, 300);
            window.ShowUtility();
        }

        private void OnEnable()
        {
            FetchVoices();
        }

        private async void FetchVoices()
        {
            voices = await VoiceoverAPI.GetVoicesAsync();
            if (voices != null)
            {
                var config = VoiceoverConfig.FindOrCreate();
                if (config != null && config.customVoices != null)
                {
                    voices.AddRange(config.customVoices);
                }
                voiceNames = voices.Select(v => v.name).ToArray();
            }
            Repaint();
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("Quick Generate Voice Over", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (targetProperty != null)
            {
                try 
                {
                    // Verify property is still valid
                    if (targetProperty.serializedObject == null || targetProperty.serializedObject.targetObject == null)
                    {
                         EditorGUILayout.HelpBox("Target property is no longer valid.", MessageType.Warning);
                         return;
                    }
                    EditorGUILayout.LabelField("Target Field:", ObjectNames.NicifyVariableName(targetProperty.name));
                    EditorGUILayout.ObjectField("Context:", targetProperty.serializedObject.targetObject, typeof(Object), true);
                }
                catch
                {
                    EditorGUILayout.HelpBox("Target property is lost.", MessageType.Error);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("No target property selected.", MessageType.Warning);
            }

            GUILayout.Space(10);

            if (voices == null)
            {
                GUILayout.Label("Loading voices...");
                return;
            }

            selectedVoiceIndex = EditorGUILayout.Popup("Voice", selectedVoiceIndex, voiceNames);
            
            GUILayout.Label("Text to Speak:");
            textToGenerate = EditorGUILayout.TextArea(textToGenerate, GUILayout.Height(100));

            GUILayout.Space(10);

            GUI.enabled = !isGenerating && !string.IsNullOrEmpty(textToGenerate);
            if (GUILayout.Button(isGenerating ? "Generating..." : "Generate & Assign", GUILayout.Height(30)))
            {
                GenerateAndAssign();
            }
            GUI.enabled = true;
        }

        private async void GenerateAndAssign()
        {
            if (voices == null || voices.Count == 0) return;
            
            isGenerating = true;
            string voiceId = voices[selectedVoiceIndex].voice_id;

            AudioClip clip = await VoiceoverAPI.GenerateVoiceAsync(textToGenerate, voiceId);
            
            if (clip != null)
            {
                // Save Asset
                string folderPath = "Assets/Voiceover/GeneratedVoices";
                var config = VoiceoverConfig.FindOrCreate();
                if (config != null && !string.IsNullOrEmpty(config.saveFolderPath))
                {
                    folderPath = config.saveFolderPath;
                }

                if (!System.IO.Directory.Exists(folderPath))
                {
                    System.IO.Directory.CreateDirectory(folderPath);
                }

                string fileName = $"VO_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
                string fullPath = System.IO.Path.Combine(folderPath, fileName);
                
                // Use SavWav to save the WAV file
                SavWav.Save(fullPath, clip);
                
                AssetDatabase.Refresh();
                
                AudioClip savedClip = AssetDatabase.LoadAssetAtPath<AudioClip>(fullPath);
                
                if (savedClip != null && targetProperty != null && targetProperty.serializedObject != null)
                {
                    targetProperty.serializedObject.Update();
                    targetProperty.objectReferenceValue = savedClip;
                    targetProperty.serializedObject.ApplyModifiedProperties();
                    Debug.Log($"Generated and assigned VO to {targetProperty.name}");
                    Close();
                }
                else
                {
                    Debug.LogError("Failed to load saved asset or target property is invalid.");
                }
            }
            else
            {
                Debug.LogError("Generation failed.");
            }

            isGenerating = false;
            Repaint();
        }
    }
}
