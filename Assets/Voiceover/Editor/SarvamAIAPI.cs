using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;
using FF.Voiceover.Editor;

namespace FF.Voiceover
{
    public static class SarvamAIAPI
    {
        private const string BASE_URL = "https://api.sarvam.ai/text-to-speech";

        public static async Task<List<Voice>> GetVoicesAsync()
        {
            // Currently Sarvam AI does not seem to have a public 'list voices' endpoint.
            // We will provide a hardcoded list of the most popular voices.
            // Documentation: https://docs.sarvam.ai/api-reference-docs/api-guides-tutorials/text-to-speech/overview
            
            var voices = new List<Voice>();
            
            string[] speakers = { "shubh", "shreya", "manan", "ishita", "ritu", "amit", "sumit", "pooja", "simran", "rahul", "kavya", "ratan", "priya", "shruti" };
            
            foreach (var speaker in speakers)
            {
                voices.Add(new Voice
                {
                    voice_id = speaker,
                    name = char.ToUpper(speaker[0]) + speaker.Substring(1),
                    category = "Sarvam AI",
                    settings = new VoiceSettings() // Default settings
                });
            }

            return await Task.FromResult(voices);
        }

        public static async Task<AudioClip> GenerateVoiceAsync(string text, string speakerId)
        {
            if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(speakerId))
            {
                Debug.LogError("Text and Speaker ID are required for Sarvam AI generation.");
                return null;
            }

            string apiKey = VoiceoverUtilities.GetSarvamAPIKey();
            if (string.IsNullOrEmpty(apiKey))
            {
                Debug.LogError("Sarvam AI API Key is missing. Please configure it in the settings.");
                return null;
            }

            var requestBody = new SarvamAITTSRequest
            {
                text = text,
                speaker = speakerId,
                target_language_code = "en-IN" // Default, can be improved to detect or be selectable
            };

            string json = JsonUtility.ToJson(requestBody);
            
            using (UnityWebRequest request = new UnityWebRequest(BASE_URL, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                
                request.SetRequestHeader("api-subscription-key", apiKey);
                request.SetRequestHeader("Content-Type", "application/json");

                var operation = request.SendWebRequest();

                while (!operation.isDone)
                    await Task.Yield();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var response = JsonUtility.FromJson<SarvamAITTSResponse>(request.downloadHandler.text);
                        if (response != null && response.audios != null && response.audios.Count > 0)
                        {
                            byte[] audioData = Convert.FromBase64String(response.audios[0]);
                            
                            // Save to temp file to load as AudioClip
                            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, "temp_sarvam_voice.wav");
                            System.IO.File.WriteAllBytes(tempPath, audioData);
                            
                            using (UnityWebRequest audioReq = UnityWebRequestMultimedia.GetAudioClip("file://" + tempPath, AudioType.WAV))
                            {
                                var audioOp = audioReq.SendWebRequest();
                                while (!audioOp.isDone) await Task.Yield();

                                if (audioReq.result == UnityWebRequest.Result.Success)
                                {
                                    AudioClip clip = DownloadHandlerAudioClip.GetContent(audioReq);
                                    clip.name = "Sarvam Generated Voice";
                                    return clip;
                                }
                                else
                                {
                                    Debug.LogError($"Failed to load generated Sarvam audio clip: {audioReq.error}");
                                    return null;
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogError($"Failed to parse Sarvam AI response: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Failed to generate Sarvam voice: {request.error}\n{request.downloadHandler.text}");
                }
            }
            return null;
        }
    }
}
