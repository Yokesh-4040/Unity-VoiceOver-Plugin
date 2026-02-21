using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FF.ElevenLabs.Editor.Styles;
using FF.ElevenLabs.Editor.Components;
using FF.ElevenLabs.Editor.Views;
using FF.ElevenLabs;

namespace FF.ElevenLabs.Editor
{
    public class ElevenLabsEditorWindow : EditorWindow
    {
        // Core State
        private bool isAuthenticated = false;
        private List<Voice> availableVoices = new List<Voice>();

        // Components
        private ElevenLabsAudioPlayer audioPlayer;
        private ElevenLabsModulesView modulesView;
        private ElevenLabsHistoryView historyView;
        private ElevenLabsSettingsView settingsView;

        // UI State
        public int selectedTab = 0;
        private Vector2 sidebarScrollPosition;
        private Vector2 contentScrollPosition;
        private Texture2D elevenLabsLogo;
        private Texture2D sarvamLogo;
        private Texture2D currentHeaderImage;

        [MenuItem("Window/Voice Over %&v")]
        public static void ShowWindow()
        {
            var window = GetWindow<ElevenLabsEditorWindow>(true, "Voice Generator", true);
            window.minSize = new Vector2(1000, 700);
            window.maxSize = new Vector2(1000, 700);
        }

        private void OnEnable()
        {
            this.minSize = new Vector2(1000, 700);
            this.maxSize = new Vector2(1000, 700);

            elevenLabsLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ElevenLabs/Sprites/elevenlabs_unity.png");
            sarvamLogo = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/ElevenLabs/Sprites/sarvam_unity.png");
            UpdateBranding();


            // Initialize Components
            audioPlayer = new ElevenLabsAudioPlayer(this);
            modulesView = new ElevenLabsModulesView(this, audioPlayer);
            historyView = new ElevenLabsHistoryView(audioPlayer);
            settingsView = new ElevenLabsSettingsView(audioPlayer, OnAuthSuccess, OnLogout);

            CheckAuth();
        }

        private void OnDisable()
        {
            if (audioPlayer != null) audioPlayer.OnDisable();
        }

        private void Update()
        {
            if (audioPlayer != null) audioPlayer.Update();
        }

        private void CheckAuth()
        {
             var config = ElevenLabsConfig.FindOrCreate();
             bool hasKey = false;
             
             if (config.activeProvider == ElevenLabsConfig.VoiceProvider.ElevenLabs)
                 hasKey = ElevenLabsUtilities.HasAPIKey();
             else
                 hasKey = ElevenLabsUtilities.HasSarvamAPIKey();

             if (hasKey)
             {
                 settingsView.VerifyCredentials();
             }
        }

        private void OnAuthSuccess()
        {
            isAuthenticated = true;
            FetchVoices();
            historyView.FetchHistory();
            Repaint();
        }

        private void OnLogout()
        {
            isAuthenticated = false;
            availableVoices.Clear();
            modulesView.SetAvailableVoices(null);
            selectedTab = 0;
            Repaint();
        }

        private async void FetchVoices()
        {
            var config = ElevenLabsConfig.FindOrCreate();
            List<Voice> voices = null;

            if (config.activeProvider == ElevenLabsConfig.VoiceProvider.ElevenLabs)
            {
                voices = await ElevenLabsAPI.GetVoicesAsync();
                if (voices != null && config.customVoices != null)
                {
                    voices.AddRange(config.customVoices);
                }
            }
            else
            {
                voices = await SarvamAIAPI.GetVoicesAsync();
            }

            if (voices != null)
            {
                availableVoices = voices;
                modulesView.SetAvailableVoices(availableVoices);
                UpdateBranding();
                Repaint();
            }
        }

        private void UpdateBranding()
        {
            var config = ElevenLabsConfig.FindOrCreate();
            if (config.activeProvider == ElevenLabsConfig.VoiceProvider.ElevenLabs)
                currentHeaderImage = elevenLabsLogo;
            else
                currentHeaderImage = sarvamLogo;
        }


        private void OnGUI()

        {
            ElevenLabsEditorStyles.Init();

            // Main Horizontal Split: [Sidebar | Content]
            GUILayout.BeginHorizontal();

            // 1. Left Sidebar (Fixed Width)
            GUILayout.BeginVertical(ElevenLabsEditorStyles.SidebarStyle, GUILayout.Width(280), GUILayout.ExpandHeight(true));
            DrawSidebar();
            GUILayout.EndVertical();

            // 2. Right Content Area (Flexible)
            GUILayout.BeginVertical(ElevenLabsEditorStyles.ContentStyle, GUILayout.ExpandHeight(true));
            contentScrollPosition = EditorGUILayout.BeginScrollView(contentScrollPosition);
            DrawMainContent();
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // 3. Persistent Player Area (Overlay at bottom)
            if (audioPlayer != null)
            {
                audioPlayer.DrawBottomPlayer(position.width, position.height);
            }
        }

        private void DrawSidebar()
        {
            // --- 1. Header Area (Branding + Navigation) ---
            GUILayout.BeginHorizontal();

            // Left: Branding (Logo)
            GUILayout.BeginVertical();
            if (currentHeaderImage != null)
            {
                float aspect = (float)currentHeaderImage.width / currentHeaderImage.height;
                float height = 40f; 
                float width = height * aspect;
                
                if (GUILayout.Button(currentHeaderImage, GUIStyle.none, GUILayout.Width(width), GUILayout.Height(height)))
                {
                    selectedTab = 0;
                }
            }
            else
            {
                if (GUILayout.Button("ElevenLabs", EditorStyles.boldLabel)) selectedTab = 0;
            }
            GUILayout.EndVertical();

            GUILayout.FlexibleSpace();

            // Right: Vertical Stack [Created By | Star Button | Nav Row]
            GUILayout.BeginVertical();
            
            // Row 1: Created By
            GUILayout.Label("Created by Yokesh", EditorStyles.miniLabel);

            // Row 2: Star Button (Added by User)
            if (GUILayout.Button("★ Star on GitHub", ElevenLabsEditorStyles.GoldenBtnStyle, GUILayout.Width(120)))
            {
                Application.OpenURL("https://github.com/Yokesh-4040/ElevenLabs-Unity-Plugin");
            }

            GUILayout.Space(4);

            // Row 3: Navigation Icons
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace(); // Push icons to right
            
            Color originalColor = GUI.backgroundColor;
            
            // Modules
            bool isModules = selectedTab == 0;
            GUI.backgroundColor = isModules ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("UnityEditor.SceneHierarchyWindow").image, "Modules"), GUILayout.Width(28), GUILayout.Height(28)))
            {
                selectedTab = 0;
            }

            // History
            bool isHistory = selectedTab == 1;
            GUI.backgroundColor = isHistory ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("UnityEditor.ConsoleWindow").image, "History"), GUILayout.Width(28), GUILayout.Height(28)))
            {
                selectedTab = 1;
            }
            
            // Settings
            bool isSettings = selectedTab == 2;
            GUI.backgroundColor = isSettings ? new Color(0.6f, 0.6f, 0.6f) : Color.white;
            if (GUILayout.Button(new GUIContent(EditorGUIUtility.IconContent("SettingsIcon").image, "Settings"), GUILayout.Width(28), GUILayout.Height(28)))
            {
                selectedTab = 2;
            }
            GUI.backgroundColor = originalColor;

            GUILayout.EndHorizontal(); // End Nav Row

            GUILayout.EndVertical(); // End Right Stack

            GUILayout.EndHorizontal(); // End Header Split

            GUILayout.Space(10);
            
            if (isAuthenticated)
            {
                 // --- 2. Contextual List ---
                 sidebarScrollPosition = EditorGUILayout.BeginScrollView(sidebarScrollPosition);
                 
                 if (selectedTab == 0 || selectedTab == 2) // Modules
                 {
                     modulesView.DrawModuleList();
                 }
                 else if (selectedTab == 1) // History
                 {
                     historyView.DrawList();
                 }
                 
                 EditorGUILayout.EndScrollView();
                 
                 // Bottom of Sidebar: Global Actions
                 GUILayout.FlexibleSpace();
                 if (GUILayout.Button("Logout", EditorStyles.miniButton))
                 {
                     OnLogout();
                 }
            }
            else
            {
                GUILayout.Label("Please login to continue.", EditorStyles.centeredGreyMiniLabel);
            }
        }

        private void DrawMainContent()
        {
            if (!isAuthenticated)
            {
                settingsView.DrawLoginUI();
                return;
            }

            if (selectedTab == 0) // Modules Detail
            {
                modulesView.DrawDetail();
            }
            else if (selectedTab == 1) // History Detail
            {
                historyView.DrawDetail();
            }
            else if (selectedTab == 2) // Settings Detail
            {
                settingsView.DrawSettingsUI();
            }
        }
    }
}