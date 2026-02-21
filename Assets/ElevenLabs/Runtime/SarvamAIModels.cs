using System;
using System.Collections.Generic;
using UnityEngine;

namespace FF.ElevenLabs
{
    [Serializable]
    public class SarvamAITTSRequest
    {
        public string text;
        public string target_language_code = "en-IN";
        public string speaker = "shubh";
        public string model = "bulbul:v3";
        public float pace = 1.0f;
        public int speech_sample_rate = 44100;

        public bool enable_preprocessing = true;
    }

    [Serializable]
    public class SarvamAITTSResponse
    {
        public string request_id;
        public List<string> audios;
    }

    [Serializable]
    public class SarvamVoice
    {
        public string speaker;
        public string language_code;
        public string gender;
        // The ElevenLabs Voice model uses voice_id and name. 
        // We might want to map these to make the UI consistent.
    }
}
