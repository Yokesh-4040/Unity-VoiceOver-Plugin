using UnityEngine;
using System.Collections.Generic;

namespace FF.Voiceover
{
    [CreateAssetMenu(fileName = "New Voice Module", menuName = "Voiceover/Voice Module")]
    public class VoiceModule : ScriptableObject
    {
        public string defaultVoiceId = "";
        
        // Settings for this module (can be set to defaults)
        public VoiceSettings defaultVoiceSettings = new VoiceSettings();

        public List<VoiceStep> steps = new List<VoiceStep>();
        
        // Helper to check if any step needs generation
        public bool HasPendingChanges()
        {
            foreach(var step in steps)
            {
                if(step.isDirty) return true;
            }
            return false;
        }
    }
}
