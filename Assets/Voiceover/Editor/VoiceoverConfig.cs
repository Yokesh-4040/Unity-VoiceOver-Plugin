using UnityEngine;

namespace FF.Voiceover
{
    //[CreateAssetMenu(fileName = "VoiceoverConfig", menuName = "Voiceover/Configuration")]
    public class VoiceoverConfig : ScriptableObject
    {
        public enum VoiceProvider
        {
            Voiceover,
            SarvamAI
        }

        [Header("Provider Configuration")]
        public VoiceProvider activeProvider = VoiceProvider.Voiceover;

        [Header("API Configuration")]

        [Tooltip("The ID of the default model to use for generation")]
        public string defaultModelId = "eleven_multilingual_v2";

        [Header("Default Voice Settings")]
        [Range(0f, 1f)]
        public float defaultStability = 0.5f;
        [Range(0f, 1f)]
        public float defaultSimilarity = 0.75f;
        
        [Header("Voice Generation")]
        public bool autoSaveAudio = true;
        public string saveFolderPath = "Assets/Voiceover/GeneratedVoices";
        [Header("Custom Voices")]
        public System.Collections.Generic.List<Voice> customVoices = new System.Collections.Generic.List<Voice>();
        
#if UNITY_EDITOR
        public static VoiceoverConfig FindOrCreate()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:VoiceoverConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<VoiceoverConfig>(path);
            }
            else
            {
                VoiceoverConfig config = ScriptableObject.CreateInstance<VoiceoverConfig>();
                string path = "Assets/Voiceover/Resources/VoiceoverConfig.asset";
                
                if (!System.IO.Directory.Exists("Assets/Voiceover/Resources"))
                    System.IO.Directory.CreateDirectory("Assets/Voiceover/Resources");
                    
                UnityEditor.AssetDatabase.CreateAsset(config, path);
                UnityEditor.AssetDatabase.SaveAssets();
                return config;
            }
        }
#endif
    }
}
