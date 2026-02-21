using UnityEngine;

namespace FF.ElevenLabs
{
    //[CreateAssetMenu(fileName = "ElevenLabsConfig", menuName = "ElevenLabs/Configuration")]
    public class ElevenLabsConfig : ScriptableObject
    {
        public enum VoiceProvider
        {
            ElevenLabs,
            SarvamAI
        }

        [Header("Provider Configuration")]
        public VoiceProvider activeProvider = VoiceProvider.ElevenLabs;

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
        public string saveFolderPath = "Assets/ElevenLabs/GeneratedVoices";
        [Header("Custom Voices")]
        public System.Collections.Generic.List<Voice> customVoices = new System.Collections.Generic.List<Voice>();
        
#if UNITY_EDITOR
        public static ElevenLabsConfig FindOrCreate()
        {
            var guids = UnityEditor.AssetDatabase.FindAssets("t:ElevenLabsConfig");
            if (guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                return UnityEditor.AssetDatabase.LoadAssetAtPath<ElevenLabsConfig>(path);
            }
            else
            {
                ElevenLabsConfig config = ScriptableObject.CreateInstance<ElevenLabsConfig>();
                string path = "Assets/ElevenLabs/Resources/ElevenLabsConfig.asset";
                
                if (!System.IO.Directory.Exists("Assets/ElevenLabs/Resources"))
                    System.IO.Directory.CreateDirectory("Assets/ElevenLabs/Resources");
                    
                UnityEditor.AssetDatabase.CreateAsset(config, path);
                UnityEditor.AssetDatabase.SaveAssets();
                return config;
            }
        }
#endif
    }
}
