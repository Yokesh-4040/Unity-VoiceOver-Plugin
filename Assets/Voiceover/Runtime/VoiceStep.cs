using UnityEngine;
using System;

namespace FF.Voiceover
{
    [Serializable]
    public class VoiceStep
    {
        [UnityEngine.Serialization.FormerlySerializedAs("stepName")]
        public string title = "New Step";
        
        [TextArea(3, 10)]
        [UnityEngine.Serialization.FormerlySerializedAs("textContent")]
        public string voText = "";
        
        // Optional override. If empty, uses Module default.
        public string assignedVoiceId = "";
        
        [UnityEngine.Serialization.FormerlySerializedAs("audioClip")]
        public AudioClip generatedAudio;
        
        // True if text has changed since last generation
        public bool isDirty = true;
        
        // Timestamp of last generation to help track versioning
        public string lastGeneratedTime = "";
        
        // Runtime only flag for UI feedback
        [System.NonSerialized]
        public bool isProcessing = false;
    }
}
