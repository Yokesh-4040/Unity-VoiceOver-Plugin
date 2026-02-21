using System;
using System.Collections.Generic;
using UnityEngine;

namespace FF.Voiceover
{
    [Serializable]
    public class Voice
    {
        public string voice_id;
        public string name;
        public string category;
        public VoiceSettings settings;
    }

    [Serializable]
    public class VoiceSettings
    {
        public float stability = 0.5f;
        public float similarity_boost = 0.75f;
        public float style = 0.0f;
        public bool use_speaker_boost = true;
    }

    [Serializable]
    public class VoicesResponse
    {
        public List<Voice> voices;
    }

    [Serializable]
    public class TextToSpeechRequest
    {
        public string text;
        public string model_id = "eleven_monolingual_v1";
        public VoiceSettings voice_settings;
    }

    [Serializable]
    public class SubscriptionResponse
    {
        public string tier;
        public int character_count;
        public int character_limit;
    }

    [Serializable]
    public class HistoryItemApiModel
    {
        public string history_item_id;
        public string voice_id;
        public string voice_name;
        public string text;
        public long date_unix;
        public string character_count_change_from;
        public string character_count_change_to;
        public string content_type;
        public string state;
    }

    [Serializable]
    public class HistoryResponse
    {
        public List<HistoryItemApiModel> history;
    }
}
