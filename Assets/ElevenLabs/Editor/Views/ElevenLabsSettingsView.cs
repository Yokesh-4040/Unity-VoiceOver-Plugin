using UnityEngine;
using UnityEditor;
using FF.ElevenLabs.Editor.Styles;
using FF.ElevenLabs.Editor.Components;

namespace FF.ElevenLabs.Editor.Views
{
    public class ElevenLabsSettingsView
    {
        private ElevenLabsAudioPlayer audioPlayer;
        private System.Action onAuthenticated;
        private System.Action onLogout;

        private string apiKey = "";
        private bool isVerifying = false;
        private string verificationError = "";
        
        private string voicePermissionStatus = "Not Checked";
        private string ttsPermissionStatus = "Not Checked";
        private string historyPermissionStatus = "Not Checked";

        public ElevenLabsSettingsView(ElevenLabsAudioPlayer player, System.Action onAuthSuccess, System.Action onLogoutCallback)
        {
            this.audioPlayer = player;
            this.onAuthenticated = onAuthSuccess;
            this.onLogout = onLogoutCallback;
            
            // Load existing key
            this.apiKey = ElevenLabsUtilities.GetAPIKey();
        }

        public void DrawSettingsUI()
        {
            ElevenLabsEditorStyles.Init();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Account Configuration", EditorStyles.boldLabel);
            GUILayout.Space(10);
            
            GUILayout.Label("Current API Key", EditorStyles.label);
            GUILayout.BeginHorizontal();
            apiKey = EditorGUILayout.PasswordField(apiKey, GUILayout.Width(300));
            if (GUILayout.Button("Update & Verify", GUILayout.Height(18)))
            {
                VerifyCredentials();
            }
            GUILayout.EndHorizontal();
            
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
            if (GUILayout.Button("Logout / Clear Key", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Logout", "Are you sure you want to remove your API Key?", "Yes", "Cancel"))
                {
                    ElevenLabsUtilities.Logout();
                    apiKey = "";
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
               Application.OpenURL("https://github.com/Yokesh-4040/ElevenLabs-Unity-Plugin/issues"); 
            }
            
            if (GUILayout.Button(new GUIContent(" Bug Report", EditorGUIUtility.IconContent("d_console.erroricon.sml").image), GUILayout.Height(30), GUILayout.Width(btnWidth)))
            {
                Application.OpenURL("https://github.com/Yokesh-4040/ElevenLabs-Unity-Plugin/issues/new");
            }
            
            if (GUILayout.Button("Check Updates", GUILayout.Height(30), GUILayout.Width(btnWidth)))
            {
                 Application.OpenURL("https://github.com/Yokesh-4040/ElevenLabs-Unity-Plugin/releases");
            }
            
            GUILayout.EndHorizontal();
            
            GUILayout.Space(20);
            
            // --- Thanks & Star ---
            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Space(5);
            
            // Use LabelField with word wrap instead of Label for better wrapping control
            EditorGUILayout.LabelField("Thanks for using ElevenLabs Voice Generator.\n If you like it, please give the project a star on GitHub.", EditorStyles.wordWrappedLabel);
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
            if (string.IsNullOrEmpty(apiKey))
            {
                verificationError = "API Key cannot be empty.";
                return;
            }

            isVerifying = true;
            verificationError = "";
            voicePermissionStatus = "Checking...";
            ttsPermissionStatus = "Checking...";
            historyPermissionStatus = "Checking...";
            
            ElevenLabsUtilities.SaveAPIKey(apiKey);

            // 1. Test Voice Access
            var voices = await ElevenLabsAPI.GetVoicesAsync();
            if (voices != null)
            {
                voicePermissionStatus = "Active";
            }
            else
            {
                voicePermissionStatus = "Failed or Access Denied";
            }

            // 2. Test TTS Access (only if voices ok, need a voice ID)
            if (voicePermissionStatus == "Active" && voices.Count > 0)
            {
                // Try generating a very small sample "a"
                // Using the first available voice
                var clip = await ElevenLabsAPI.GenerateVoiceAsync("a", voices[0].voice_id);
                if (clip != null)
                {
                    ttsPermissionStatus = "Active";
                }
                else
                {
                    ttsPermissionStatus = "Failed or Access Denied";
                }
            }
            else
            {
                if (voicePermissionStatus != "Active") ttsPermissionStatus = "Cannot Check (Voice API Failed)";
                else ttsPermissionStatus = "Failed (No voices found)";
            }

            // 3. Test History Access
            var history = await ElevenLabsAPI.GetHistoryAsync();
            if (history != null)
            {
                historyPermissionStatus = "Active";
            }
            else
            {
                historyPermissionStatus = "Failed or Access Denied";
            }

            isVerifying = false;

            if (voicePermissionStatus == "Active" && ttsPermissionStatus == "Active")
            {
                onAuthenticated?.Invoke();
            }
            else
            {
                verificationError = "Could not verify full access. Please check your API permissions.";
            }
        }
        
        public void DrawLoginUI()
        {
            ElevenLabsEditorStyles.Init();

            GUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("Setup Instructions", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("1. Create an Account on ElevenLabs.io", EditorStyles.wordWrappedLabel);
            
            if (GUILayout.Button("Open ElevenLabs Website", GUILayout.Width(200)))
            {
                ElevenLabsUtilities.OpenWebsite("https://elevenlabs.io/?from=partnerunity4928");
            }
            
            GUILayout.Space(10);
            EditorGUILayout.LabelField("2. Create a new API Key", EditorStyles.wordWrappedLabel);
            EditorGUILayout.LabelField("   - Go to Profile > API Keys", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("   - Click 'Create New API Key'", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("   - Ensure permissions are allowed", EditorStyles.miniLabel);
            
            if (GUILayout.Button("Open API Keys Page", GUILayout.Width(200)))
            {
                ElevenLabsUtilities.OpenWebsite("https://elevenlabs.io/app/settings/api-keys");
            }

            GUILayout.Space(20);
            GUILayout.Label("Authentication", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            GUILayout.Label("API Key:", GUILayout.Width(60));
            apiKey = EditorGUILayout.PasswordField(apiKey);
            if (GUILayout.Button("Paste", GUILayout.Width(60))) apiKey = GUIUtility.systemCopyBuffer;
            GUILayout.EndHorizontal();

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
                 DrawStatusLabel("History API:", historyPermissionStatus);
            }
            
            GUILayout.EndVertical();
        }
    }
}
