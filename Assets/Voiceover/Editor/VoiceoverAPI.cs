using System.Collections.Generic;
using System.Threading.Tasks;
using FF.Voiceover;
using UnityEngine;
using UnityEngine.Networking;
using FF.Voiceover.Editor;

namespace FF.Voiceover
{
    public static class VoiceoverAPI
    {
        private const string BASE_URL = "https://api.voiceover.io/v1";

        public static async Task<List<Voice>> GetVoicesAsync()
        {
            string url = $"{BASE_URL}/voices";
            string apiKey = VoiceoverUtilities.GetAPIKey();

            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("Voiceover API Key is missing. Please configure it in the settings.");
                return null;
            }

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("xi-api-key", apiKey);
                request.SetRequestHeader("Accept", "application/json");

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<VoicesResponse>(request.downloadHandler.text);
                        return response.voices;
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to parse voices response: {e.Message}");
                        return null;
                    }
                }
                else
                {
                    Debug.LogError($"Failed to fetch voices: {request.error}\n{request.downloadHandler.text}");
                    return null;
                }
            }
        }

        public static async Task<AudioClip> GenerateVoiceAsync(string text, string voiceId, VoiceSettings settings = null)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(voiceId))
            {
                Debug.LogError("Text and Voice ID are required for generation.");
                return null;
            }

            string apiKey = VoiceoverUtilities.GetAPIKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("Voiceover API Key is missing.");
                return null;
            }

            // Ensure output format is specified to match what we expect (mp3)
            string url = $"{BASE_URL}/text-to-speech/{voiceId}?output_format=mp3_44100_128";

            if (settings == null) settings = new VoiceSettings();

            var requestBody = new TextToSpeechRequest
            {
                text = text,
                model_id = "eleven_multilingual_v2",
                voice_settings = settings
            };

            string json = JsonUtility.ToJson(requestBody);
            
            // Use DownloadHandlerBuffer to allow reading error text
            using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                
                request.SetRequestHeader("xi-api-key", apiKey);
                request.SetRequestHeader("Content-Type", "application/json");
                request.SetRequestHeader("Accept", "audio/mpeg");

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    byte[] audioData = request.downloadHandler.data;
                    
                    // Save to temp file to load as AudioClip
                    string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "temp_voice_preview.mp3");
                    System.IO.File.WriteAllBytes(tempPath, audioData);
                    
                    // Load the temp file as AudioClip
                    using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.MPEG))
                    {
                        var audioOp = audioReq.SendWebRequest();
                        while (!audioOp.isDone) await Task.Yield();

                        if (audioReq.result == UnityWebRequest.Result.Success)
                        {
                            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioReq);
                            clip.name = "Generated Voice";
                            return clip;
                        }
                        else
                        {
                            Debug.LogError($"Failed to load generated audio clip: {audioReq.error}");
                            return null;
                        }
                    }
                }
                else
                {
                    string errorMessage = request.error;
                    if (request.downloadHandler != null)
                    {
                        // Now we can safely access text because it's DownloadHandlerBuffer
                        errorMessage += $"\n{request.downloadHandler.text}";
                    }
                    
                    Debug.LogError($"Failed to generate voice: {errorMessage}");
                    return null;
                }
            }
        }


        public static async Task<List<HistoryItemApiModel>> GetHistoryAsync()
        {
            string url = $"{BASE_URL}/history";
            string apiKey = VoiceoverUtilities.GetAPIKey();

            if (string.IsNullOrEmpty(apiKey)) return null;

            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("xi-api-key", apiKey);
                request.SetRequestHeader("Accept", "application/json");

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                     try
                     {
                         var response = JsonUtility.FromJson<HistoryResponse>(request.downloadHandler.text);
                         return response.history;
                     }
                     catch (System.Exception e)
                     {
                         Debug.LogError($"Failed to parse history: {e.Message}");
                         return null;
                     }
                }
                else
                {
                    Debug.LogError($"Failed to fetch history: {request.error}");
                    return null;
                }
            }
        }

        public static async Task<AudioClip> GetHistoryAudioAsync(string historyItemId)
        {
            string url = $"{BASE_URL}/history/{historyItemId}/audio";
            string apiKey = VoiceoverUtilities.GetAPIKey();

            if (string.IsNullOrEmpty(apiKey)) return null;
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("xi-api-key", apiKey);
                request.SetRequestHeader("Accept", "audio/mpeg");
                request.downloadHandler = new DownloadHandlerBuffer();

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                     byte[] audioData = request.downloadHandler.data;
                     
                     // Similar to GenerateVoiceAsync, save to temp and load
                     string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"history_{historyItemId}.mp3");
                     System.IO.File.WriteAllBytes(tempPath, audioData);
                     
                     string fileUrl = "file://" + tempPath;
                     using (UnityWebRequest audioReq = UnityWebRequest.Get(fileUrl))
                     {
                         var dh = new DownloadHandlerAudioClip(fileUrl, AudioType.MPEG);
                         dh.streamAudio = false; // Force full load for waveform visualization
                         audioReq.downloadHandler = dh;

                         var audioOp = audioReq.SendWebRequest();
                         while (!audioOp.isDone) await Task.Yield();

                         if (audioReq.result == UnityWebRequest.Result.Success)
                         {
                             return DownloadHandlerAudioClip.GetContent(audioReq);
                         }
                     }
                }
                else
                {
                    Debug.LogError($"Failed to fetch history audio: {request.error}");
                }
                return null;
            }
        }
        public static async Task<byte[]> GetHistoryAudioBytesAsync(string historyItemId)
        {
            string url = $"{BASE_URL}/history/{historyItemId}/audio";
            string apiKey = VoiceoverUtilities.GetAPIKey();

            if (string.IsNullOrEmpty(apiKey)) return null;
            
            using (UnityWebRequest request = UnityWebRequest.Get(url))
            {
                request.SetRequestHeader("xi-api-key", apiKey);
                request.SetRequestHeader("Accept", "audio/mpeg");
                request.downloadHandler = new DownloadHandlerBuffer();

                var operation = request.SendWebRequest();
                while (!operation.isDone) await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                     return request.downloadHandler.data;
                }
                else
                {
                    Debug.LogError($"Failed to fetch history audio bytes: {request.error}");
                    return null;
                }
            }
        }
    }
}
