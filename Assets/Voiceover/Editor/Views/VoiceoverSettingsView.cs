using UnityEngine;
using UnityEditor;
using FF.Voiceover.Editor.Styles;
using FF.Voiceover.Editor.Components;
using FF.Voiceover;

namespace FF.Voiceover.Editor.Views
{
    public class VoiceoverSettingsView
    {
        private VoiceoverAudioPlayer audioPlayer;
        private System.Action onAuthenticated;
        private System.Action onLogout;

        private string apiKey = "";
        private string sarvamApiKey = "";
        private bool isVerifying = false;
        private string verificationError = "";
        
        private string voicePermissionStatus = "Not Checked";
        private string ttsPermissionStatus = "Not Checked";
        private string historyPermissionStatus = "Not Checked";

        public VoiceoverSettingsView(VoiceoverAudioPlayer player, System.Action onAuthSuccess, System.Action onLogoutCallback)
        {
            this.audioPlayer = player;
            this.onAuthenticated = onAuthSuccess;
            this.onLogout = onLogoutCallback;
            
            // Load existing keys
            this.apiKey = VoiceoverUtilities.GetAPIKey();
            this.sarvamApiKey = VoiceoverUtilities.GetSarvamAPIKey();
        }

        public void DrawSettingsUI()
        {
            VoiceoverEditorStyles.Init();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Browser Configuration", EditorStyles.boldLabel);
            GUILayout.Space(10);

            var config = VoiceoverConfig.FindOrCreate();
            EditorGUI.BeginChangeCheck();
            config.activeProvider = (VoiceoverConfig.VoiceProvider)EditorGUILayout.EnumPopup("Active Provider", config.activeProvider);
            if (EditorGUI.EndChangeCheck())
            {
                EditorUtility.SetDirty(config);
                AssetDatabase.SaveAssets();
                VerifyCredentials();
            }

            GUILayout.Space(10);
            
            if (config.activeProvider == VoiceoverConfig.VoiceProvider.Voiceover)
            {
                GUILayout.Label("Voiceover API Key", EditorStyles.label);
                GUILayout.BeginHorizontal();
                apiKey = EditorGUILayout.PasswordField(apiKey, GUILayout.Width(300));
                if (GUILayout.Button("Update & Verify", GUILayout.Height(18)))
                {
                    VerifyCredentials();
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Sarvam AI API Key", EditorStyles.label);
                GUILayout.BeginHorizontal();
                sarvamApiKey = EditorGUILayout.PasswordField(sarvamApiKey, GUILayout.Width(300));
                if (GUILayout.Button("Update & Verify", GUILayout.Height(18)))
                {
                    VerifyCredentials();
                }
                GUILayout.EndHorizontal();
            }
            
            GUILayout.Space(10);
            GUILayout.Label("Preferences", EditorStyles.boldLabel);
            if (audioPlayer != null)
            {
                audioPlayer.AutoPlayAudio = EditorGUILayout.Toggle("Auto-play on Generate", audioPlayer.AutoPlayAudio);
            }
            
            GUILayout.Space(20);
            
            GUILayout.Label("Permissions Check", EditorStyles.boldLabel);
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            if (isVerifying)
            {
                GUILayout.Label("Verifying Permissions...", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                if (voicePermissionStatus == "Not Checked")
                {
                    if (GUILayout.Button("Check Permissions Now"))
                    {
                        VerifyCredentials();
                    }
                }
                else
                {
                    DrawStatusLabel("Voice API:", voicePermissionStatus);
                    DrawStatusLabel("TTS API:", ttsPermissionStatus);
                    DrawStatusLabel("History API:", historyPermissionStatus);
                    
                    GUILayout.Space(5);
                    if (GUILayout.Button("Re-Verify Permissions"))
                    {
                        VerifyCredentials();
                    }
                }
            }
            GUILayout.EndVertical();
            
            GUILayout.Space(20);
            
            GUILayout.Label("Account Actions", EditorStyles.boldLabel);
            GUI.backgroundColor = new Color(1f, 0.7f, 0.7f);
            if (GUILayout.Button("Logout / Clear All Keys", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Logout", "Are you sure you want to remove all API Keys?", "Yes", "Cancel"))
                {
                    VoiceoverUtilities.Logout();
                    apiKey = "";
                    sarvamApiKey = "";
                    onLogout?.Invoke();
                }
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(20);
            
            // --- Found an issue? ---
            GUILayout.Label("Found an issue?", EditorStyles.boldLabel);
            GUILayout.BeginHorizontal();
            
            float btnWidth = 120;

            if (GUILayout.Button(new GUIContent(" Help / Talk", EditorGUIUtility.IconContent("d_console.infoicon.sml").image), GUILayout.Height(30), GUILayout.Width(btnWidth)))
            {
               Application.OpenURL("https://github.com/Yokesh-4040/Voiceover-Unity-Plugin/issues"); 
            }
            
            if (GUILayout.Button(new GUIContent(" Bug Report", EditorGUIUtility.IconContent("d_console.erroricon.sml").image), GUILayout.Height(30), GUILayout.Width(btnWidth)))
            {
                Application.OpenURL("https://github.com/Yokesh-4040/Voiceover-Unity-Plugin/issues/new");
            }
            
            if (GUILayout.Button("Check Updates", GUILayout.Height(30), GUILayout.Width(btnWidth)))
            {
                 Application.OpenURL("https://github.com/Yokesh-4040/Voiceover-Unity-Plugin/releases");
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            // --- Thanks & Star ---
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            
            // Use LabelField with word wrap instead of Label for better wrapping control
            EditorGUILayout.LabelField("Thanks for using Voiceover Voice Generator.\n If you like it, please give the project a star on GitHub.", EditorStyles.wordWrappedLabel);
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Sincerely,", EditorStyles.miniLabel);
            GUILayout.Label("Yokesh", EditorStyles.boldLabel);
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
            GUILayout.Space(5);
            GUILayout.EndVertical();

            GUILayout.EndVertical();
        }

        private void DrawStatusLabel(string label, string status)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(label, GUILayout.Width(80));
            Color c = GUI.color;
            if (status.Contains("Success") || status.Contains("Active")) GUI.color = Color.green;
            else if (status.Contains("Failed") || status.Contains("Disabled")) GUI.color = Color.red;
            GUILayout.Label(status, EditorStyles.boldLabel);
            GUI.color = c;
            GUILayout.EndHorizontal();
        }

        public async void VerifyCredentials()
        {
            var config = VoiceoverConfig.FindOrCreate();
            
            if (config.activeProvider == VoiceoverConfig.VoiceProvider.Voiceover)
            {
                if (string.IsNullOrEmpty(apiKey))
                {
                    verificationError = "Voiceover API Key cannot be empty.";
                    return;
                }
                VoiceoverUtilities.SaveAPIKey(apiKey);
            }
            else
            {
                if (string.IsNullOrEmpty(sarvamApiKey))
                {
                    verificationError = "Sarvam AI API Key cannot be empty.";
                    return;
                }
                VoiceoverUtilities.SaveSarvamAPIKey(sarvamApiKey);
            }

            isVerifying = true;
            verificationError = "";
            voicePermissionStatus = "Checking...";
            ttsPermissionStatus = "Checking...";
            historyPermissionStatus = "Checking...";

            if (config.activeProvider == VoiceoverConfig.VoiceProvider.Voiceover)
            {
                // 1. Test Voice Access
                var voices = await VoiceoverAPI.GetVoicesAsync();
                if (voices != null)
                {
                    voicePermissionStatus = "Active";
                }
                else
                {
                    voicePermissionStatus = "Failed or Access Denied";
                }

                // 2. Test TTS Access
                if (voicePermissionStatus == "Active" && voices.Count > 0)
                {
                    var clip = await VoiceoverAPI.GenerateVoiceAsync("a", voices[0].voice_id);
                    if (clip != null) ttsPermissionStatus = "Active";
                    else ttsPermissionStatus = "Failed or Access Denied";
                }
                else
                {
                    ttsPermissionStatus = "Cannot Check";
                }

                // 3. Test History Access
                var history = await VoiceoverAPI.GetHistoryAsync();
                if (history != null) historyPermissionStatus = "Active";
                else historyPermissionStatus = "Failed or Access Denied";
            }
            else
            {
                // Sarvam AI Verification
                var voices = await SarvamAIAPI.GetVoicesAsync();
                if (voices != null)
                {
                    voicePermissionStatus = "Active (Static List)";
                }
                else
                {
                    voicePermissionStatus = "Failed";
                }

                // Test TTS with a dummy text
                var clip = await SarvamAIAPI.GenerateVoiceAsync("Test", "shubh");
                if (clip != null) ttsPermissionStatus = "Active";
                else ttsPermissionStatus = "Failed";

                historyPermissionStatus = "Not Supported by Sarvam AI";
            }

            isVerifying = false;

            if (voicePermissionStatus.Contains("Active") && ttsPermissionStatus == "Active")
            {
                onAuthenticated?.Invoke();
            }
            else
            {
                verificationError = "Could not verify full access for the selected provider.";
            }
        }
        
        public void DrawLoginUI()
        {
            VoiceoverEditorStyles.Init();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Select Voice Provider", EditorStyles.boldLabel);
            
            var config = VoiceoverConfig.FindOrCreate();
            config.activeProvider = (VoiceoverConfig.VoiceProvider)EditorGUILayout.EnumPopup("Provider", config.activeProvider);
            
            GUILayout.Space(10);
            
            if (config.activeProvider == VoiceoverConfig.VoiceProvider.Voiceover)
            {
                GUILayout.Label("Voiceover Setup", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. Create an Account on Voiceover.io", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Open Voiceover Website", GUILayout.Width(200)))
                    VoiceoverUtilities.OpenWebsite("https://voiceover.io/?from=partnerunity4928");
                
                GUILayout.Space(10);
                EditorGUILayout.LabelField("2. Create a new API Key", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Open API Keys Page", GUILayout.Width(200)))
                    VoiceoverUtilities.OpenWebsite("https://voiceover.io/app/settings/api-keys");

                GUILayout.Space(20);
                GUILayout.Label("TwelveLabs API Key", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                apiKey = EditorGUILayout.PasswordField(apiKey);
                if (GUILayout.Button("Paste", GUILayout.Width(60))) apiKey = GUIUtility.systemCopyBuffer;
                GUILayout.EndHorizontal();
            }
            else
            {
                GUILayout.Label("Sarvam AI Setup", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("1. Create an Account on Sarvam.ai", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Open Sarvam AI Website", GUILayout.Width(200)))
                    VoiceoverUtilities.OpenWebsite("https://www.sarvam.ai/");
                
                GUILayout.Space(10);
                EditorGUILayout.LabelField("2. Get your API Subscription Key from Dashboard", EditorStyles.wordWrappedLabel);
                if (GUILayout.Button("Open Sarvam Dashboard", GUILayout.Width(200)))
                    VoiceoverUtilities.OpenWebsite("https://dashboard.sarvam.ai/");

                GUILayout.Space(20);
                GUILayout.Label("Sarvam AI API Key", EditorStyles.boldLabel);
                GUILayout.BeginHorizontal();
                sarvamApiKey = EditorGUILayout.PasswordField(sarvamApiKey);
                if (GUILayout.Button("Paste", GUILayout.Width(60))) sarvamApiKey = GUIUtility.systemCopyBuffer;
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(10);

            if (isVerifying)
            {
                GUILayout.Label("Verifying Credentials...", EditorStyles.centeredGreyMiniLabel);
            }
            else
            {
                if (GUILayout.Button("Verify & Connect", GUILayout.Height(30)))
                {
                    VerifyCredentials();
                }
            }

            if (!string.IsNullOrEmpty(verificationError))
            {
                EditorGUILayout.HelpBox(verificationError, MessageType.Error);
            }
            
            if (voicePermissionStatus != "Not Checked")
            {
                 GUILayout.Space(5);
                 GUILayout.Label("Permissions Status:", EditorStyles.boldLabel);
                 DrawStatusLabel("Voice API:", voicePermissionStatus);
                 DrawStatusLabel("TTS API:", ttsPermissionStatus);
                 if (config.activeProvider == VoiceoverConfig.VoiceProvider.Voiceover)
                    DrawStatusLabel("History API:", historyPermissionStatus);
            }
            
            GUILayout.EndVertical();
        }
    }
}

