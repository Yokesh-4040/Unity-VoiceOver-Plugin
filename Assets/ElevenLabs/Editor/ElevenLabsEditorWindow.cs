using System.IO.Compression;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using FF.ElevenLabs.Editor;

namespace FF.ElevenLabs.Editor
{
    public class ElevenLabsEditorWindow : EditorWindow
    {
        private string apiKey = "";
        private bool isAuthenticated = false;
        
        // Voice Data
        private List<Voice> availableVoices = new List<Voice>();
        private string[] voiceNames;
        
        // History Data
        private List<HistoryItemApiModel> apiHistory = new List<HistoryItemApiModel>();
        
        // Player Data
        private AudioClip currentPlayingClip;
        private string currentPlayingTitle = "No Audio Selected";
        private UnityEditor.Editor audioClipEditor;
        
        // UI State
        private Vector2 mainScrollPosition;
        private int selectedTab = 0;
        private string[] tabs = new string[] { "Voice Modules", "History", "Settings" };
        
        // Module UI State
        private Vector2 modulesScrollPosition;
        private Vector2 inspectorScrollPosition;
        private List<VoiceModule> foundModules = new List<VoiceModule>();
        private VoiceModule selectedModule;
        private VoiceStep selectedStep;
        private HashSet<VoiceModule> expandedModules = new HashSet<VoiceModule>();
        
        // Progress Limiting
        private bool isGenerating = false;
        private float currentProgress = 0f;
        private string progressInfo = "";
        
        // Verification State
        private bool isVerifying = false;
        private string voicePermissionStatus = "Not Checked";
        private string ttsPermissionStatus = "Not Checked";
        private string historyPermissionStatus = "Not Checked";
        private string verificationError = "";

        [MenuItem("Window/Voice Over %&v")]
        public static void ShowWindow()
        {
            GetWindow<ElevenLabsEditorWindow>("Voice Generator");
        }

        private void OnEnable()
        {
            // Load API Key
            apiKey = ElevenLabsUtilities.GetAPIKey();
            isAuthenticated = ElevenLabsUtilities.HasAPIKey();
            
            if (isAuthenticated)
            {
                FetchVoices();
                FetchHistory();
            }
            
            // Clear Player State on Reload
            currentPlayingClip = null;
            currentPlayingTitle = "No Audio Selected";
            StopAllClips();
        }

        private void OnDisable()
        {
            StopAllClips();
            if (audioClipEditor != null)
            {
                DestroyImmediate(audioClipEditor);
            }
        }

        private void OnGUI()
        {
            // Layout Configuration
            float headerHeight = 60f;
            float playerHeight = 180f; // Increased from 140f to prevent clipping
            float contentHeight = position.height - headerHeight - playerHeight;
            
            // 1. Header Area
            GUILayout.BeginArea(new Rect(0, 0, position.width, headerHeight));
            DrawHeader();
            GUILayout.EndArea();

            if (!isAuthenticated)
            {
                GUILayout.BeginArea(new Rect(0, headerHeight, position.width, position.height - headerHeight));
                DrawLoginUI();
                GUILayout.EndArea();
                return;
            }

            // 2. Main Content Area
            Rect contentRect = new Rect(0, headerHeight, position.width, contentHeight);
            GUILayout.BeginArea(contentRect);
            
            // Tabs
            GUILayout.BeginHorizontal();
            GUILayout.Space(10);
            selectedTab = GUILayout.Toolbar(selectedTab, tabs, GUILayout.Height(25));
            GUILayout.Space(10);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            // Tab Content
            if (selectedTab == 0)
            {
                DrawModulesUI();
            }
            else if (selectedTab == 1)
            {
                // History needs its own scroll view since Modules manages its own split view
                mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
                DrawHistoryUI();
                EditorGUILayout.EndScrollView();
            }
            else if (selectedTab == 2)
            {
                mainScrollPosition = EditorGUILayout.BeginScrollView(mainScrollPosition);
                DrawSettingsUI();
                EditorGUILayout.EndScrollView();
            }
            
            GUILayout.EndArea();

            // 3. Persistent Player Area
            Rect playerRect = new Rect(0, position.height - playerHeight, position.width, playerHeight);
            GUILayout.BeginArea(playerRect);
            DrawBottomPlayer();
            GUILayout.EndArea();
        }

        private void DrawHeader()
        {
            GUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("ElevenLabs Voice Generator", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if (isAuthenticated)
            {
                if (GUILayout.Button("Logout", EditorStyles.toolbarButton))
                {
                    ElevenLabsUtilities.Logout();
                    isAuthenticated = false;
                    apiKey = "";
                    availableVoices.Clear();
                }
            }
            GUILayout.EndHorizontal();
            
            if (isGenerating)
            {
                EditorGUI.ProgressBar(new Rect(0, 20, position.width, 5), currentProgress, "");
                GUILayout.Label(progressInfo, EditorStyles.centeredGreyMiniLabel);
            }
        }

        #region Player
        private void DrawBottomPlayer()
        {
            GUILayout.BeginVertical(EditorStyles.helpBox);
            
            // Top Row: Title + Controls
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Audio Player", EditorStyles.boldLabel);
            
            GUIStyle wrappedMini = new GUIStyle(EditorStyles.miniLabel);
            wrappedMini.wordWrap = true;
            GUILayout.Label(currentPlayingTitle, wrappedMini, GUILayout.MaxWidth(position.width - 250));
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            if (currentPlayingClip != null)
            {
                 if (GUILayout.Button("Stop / Pause", GUILayout.Height(30)))
                 {
                     StopAllClips();
                 }
                 if (GUILayout.Button("Save to Project", GUILayout.Height(30)))
                 {
                     SaveCurrentClip();
                 }
            }
            GUILayout.EndHorizontal();

            if (currentPlayingClip != null)
            {
                 // Check/Create editor
                 if (audioClipEditor == null || audioClipEditor.target != currentPlayingClip)
                 {
                     if(audioClipEditor != null) DestroyImmediate(audioClipEditor);
                     audioClipEditor = UnityEditor.Editor.CreateEditor(currentPlayingClip);
                 }
                 
                 // Draw Waveform
                 if (audioClipEditor != null)
                 {
                     Rect previewRect = GUILayoutUtility.GetRect(position.width - 20, 80);
                     if (Event.current.type == EventType.Repaint)
                     {
                        EditorStyles.helpBox.Draw(previewRect, false, false, false, false);
                     }
                     audioClipEditor.OnInteractivePreviewGUI(previewRect, EditorStyles.whiteLabel);
                     
                     // Toolbar
                     GUILayout.BeginHorizontal(EditorStyles.toolbar);
                     GUILayout.Label(audioClipEditor.GetInfoString(), EditorStyles.miniLabel);
                     GUILayout.FlexibleSpace();
                     audioClipEditor.OnPreviewSettings();
                     GUILayout.EndHorizontal();
                 }
            }
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("No audio selected. Generate voice or select from history.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
        }

        private void StopAllClips()
        {
            var assembly = typeof(UnityEditor.Editor).Assembly;
            var type = assembly.GetType("UnityEditor.AudioUtil");
            var method = type.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            if (method != null) method.Invoke(null, null);
        }

        private void PlayAudio(AudioClip clip, string title)
        {
            if (clip == null) return;
            
            currentPlayingClip = clip;
            currentPlayingTitle = title;
            
            if (audioClipEditor != null) DestroyImmediate(audioClipEditor);
            audioClipEditor = UnityEditor.Editor.CreateEditor(currentPlayingClip);
            
            // Auto play?
            // EditorUtility.Audio.StopAllPreviewClips();
            // EditorUtility.Audio.PlayPreviewClip(clip);
            // InteractivePreviewGUI handles playback when user clicks play. 
            // If we want auto-play, we can use reflection.
            // For now, let's just load it. User can press play.
            
            Repaint();
        }
        
        private void SaveCurrentClip()
        {
            if (currentPlayingClip == null) return;

            string folderName = "Assets/ElevenLabs/GeneratedVoices";
            if (!System.IO.Directory.Exists(folderName)) System.IO.Directory.CreateDirectory(folderName);
            
            // Clean title for filename
            string safeTitle = System.Text.RegularExpressions.Regex.Replace(currentPlayingTitle, "[^a-zA-Z0-9 _-]", "");
            string fileName = $"{folderName}/{safeTitle}_{System.DateTime.Now:yyyyMMdd_HHmmss}.wav";
            
            if (SavWav.Save(fileName, currentPlayingClip))
            {
                AssetDatabase.Refresh();
                var clipRef = AssetDatabase.LoadAssetAtPath<AudioClip>(fileName);
                EditorGUIUtility.PingObject(clipRef);
                EditorUtility.DisplayDialog("Success", $"Saved to {fileName}", "OK");
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Failed to save audio file.", "OK");
            }
        }
        #endregion

        #region History
        private async void FetchHistory()
        {
            var history = await ElevenLabsAPI.GetHistoryAsync();
            if (history != null)
            {
                apiHistory = history;
                Repaint();
            }
        }

        private void DrawHistoryUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Cloud History", EditorStyles.boldLabel);
            if(GUILayout.Button("Refresh", EditorStyles.miniButton, GUILayout.Width(60))) FetchHistory();
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (apiHistory == null || apiHistory.Count == 0)
            {
                GUILayout.Label("No history items found.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            foreach (var item in apiHistory)
            {
                // Click to play logic
                Rect itemRect = EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                GUILayout.BeginHorizontal();
                
                GUILayout.BeginVertical();
                GUILayout.Label($"{item.voice_name}", EditorStyles.boldLabel);
                
                // Date & Char Count
                System.DateTime date = System.DateTimeOffset.FromUnixTimeSeconds(item.date_unix).LocalDateTime;
                GUILayout.Label($"{date:yyyy-MM-dd HH:mm:ss} • {item.text.Length} chars", EditorStyles.miniLabel);
                
                GUILayout.Label(item.text.Length > 60 ? item.text.Substring(0, 60) + "..." : item.text, EditorStyles.wordWrappedLabel);
                GUILayout.EndVertical();

                // if (GUILayout.Button("Load", GUILayout.Width(60), GUILayout.Height(40)))
                // {
                //    LoadHistoryItem(item);
                // }
                
                GUILayout.EndHorizontal();
                GUILayout.EndVertical();
                
                 if (Event.current.type == EventType.MouseDown && itemRect.Contains(Event.current.mousePosition))
                 {
                     LoadHistoryItem(item);
                     Event.current.Use();
                     Repaint();
                 }
            }
        }

        private async void LoadHistoryItem(HistoryItemApiModel item)
        {
             if (string.IsNullOrEmpty(item.history_item_id)) return;
             
             var clip = await ElevenLabsAPI.GetHistoryAudioAsync(item.history_item_id);
             if (clip != null)
             {
                 PlayAudio(clip, $"{item.voice_name} - {item.history_item_id}");
             }
        }
        #endregion

        #region Modules
        private void DrawModulesUI()
        {
            GUILayout.BeginHorizontal();
            
            // Left Sidebar: Tree View
            GUILayout.BeginVertical(GUILayout.Width(250)); // Slightly wider for hierarchy
            DrawModuleList();
            GUILayout.EndVertical();
            
            GUILayout.Box("", GUILayout.Width(1), GUILayout.ExpandHeight(true)); // Vertical Separator
            
            // Right Content: Inspector
            GUILayout.BeginVertical();
            inspectorScrollPosition = EditorGUILayout.BeginScrollView(inspectorScrollPosition);
            
            if (selectedStep != null)
            {
                DrawSelectedStep();
            }
            else if (selectedModule != null)
            {
                DrawSelectedModule();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a Voice Module or Step.", MessageType.Info);
            }
            
            EditorGUILayout.EndScrollView();
            GUILayout.EndVertical();
            
            GUILayout.EndHorizontal();
        }

        private void DrawModuleList()
        {
            GUILayout.Label("Modules", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ New Module", EditorStyles.miniButton)) CreateNewModule();
            if (GUILayout.Button("Refresh", EditorStyles.miniButton)) RefreshModuleList();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            modulesScrollPosition = EditorGUILayout.BeginScrollView(modulesScrollPosition);
            
            if (foundModules.Count == 0) RefreshModuleList();

            foreach (var mod in foundModules)
            {
                if (mod == null) continue;
                
                // Module Row
                GUILayout.BeginHorizontal();
                
                // Expand Toggle
                bool isExpanded = expandedModules.Contains(mod);
                string arrow = isExpanded ? "▼" : "▶";
                if (GUILayout.Button(arrow, EditorStyles.label, GUILayout.Width(15)))
                {
                    if (isExpanded) expandedModules.Remove(mod);
                    else expandedModules.Add(mod);
                }
                
                // Selection Button
                GUI.backgroundColor = (selectedModule == mod && selectedStep == null) ? Color.cyan : Color.white;
                if (GUILayout.Button(mod.name, EditorStyles.label, GUILayout.Height(20)))
                {
                    selectedModule = mod;
                    selectedStep = null; // Deselect step when module is selected
                    GUI.FocusControl(null);
                }
                GUI.backgroundColor = Color.white;
                
                // Add Step Shortcut
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    mod.steps.Add(new VoiceStep { title = $"Step {mod.steps.Count + 1}" });
                    EditorUtility.SetDirty(mod);
                    expandedModules.Add(mod); // Auto expand
                }
                
                GUILayout.EndHorizontal();
                
                // Steps (Children)
                if (isExpanded)
                {
                    for (int i = 0; i < mod.steps.Count; i++)
                    {
                        var step = mod.steps[i];
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(20); // Indent
                        
                        // Icon based on status
                        string icon = "•"; 
                        if (step.generatedAudio != null) icon = "♫";
                        if (step.isDirty) icon = "*";
                        
                        string displayTitle = $"{icon} {step.title}";
                        
                        // Custom Highlight Logic
                        // Truncate to fit sidebar
                        string truncatedTitle = displayTitle.Length > 28 ? displayTitle.Substring(0, 25) + "..." : displayTitle;
                        Rect r = GUILayoutUtility.GetRect(new GUIContent(truncatedTitle), EditorStyles.label, GUILayout.Height(18), GUILayout.MaxWidth(220));
                        
                        if (selectedStep == step)
                        {
                            EditorGUI.DrawRect(r, new Color(0.24f, 0.49f, 0.9f, 0.5f)); // Selection Blue
                        }
                        
                        if (GUI.Button(r, displayTitle, EditorStyles.label))
                        {
                            selectedModule = mod;
                            selectedStep = step;
                            GUI.FocusControl(null);
                            
                            // Auto-play on select
                            if (selectedStep.generatedAudio != null)
                            {
                                PlayAudio(selectedStep.generatedAudio, $"{selectedModule.name} - {selectedStep.title}");
                            }
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                }
            }
            
            EditorGUILayout.EndScrollView();
        }

        private void RefreshModuleList()
        {
            foundModules.Clear();
            string[] guids = AssetDatabase.FindAssets("t:VoiceModule");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                VoiceModule mod = AssetDatabase.LoadAssetAtPath<VoiceModule>(path);
                if (mod != null) foundModules.Add(mod);
            }
        }

        private void CreateNewModule()
        {
            string path = "Assets/ElevenLabs/VoiceModules";
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            
            VoiceModule newMod = ScriptableObject.CreateInstance<VoiceModule>();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewVoiceModule.asset");
            
            AssetDatabase.CreateAsset(newMod, uniquePath);
            AssetDatabase.SaveAssets();
            
            RefreshModuleList();
            selectedModule = newMod;
            selectedStep = null;
        }

        private void DrawSelectedModule()
        {
             GUILayout.Label($"Module: {selectedModule.name}", EditorStyles.boldLabel);
             GUILayout.Space(10);

             EditorGUI.BeginChangeCheck();
             string newName = EditorGUILayout.DelayedTextField("Module Name", selectedModule.name);
             if (EditorGUI.EndChangeCheck())
             {
                 AssetDatabase.RenameAsset(AssetDatabase.GetAssetPath(selectedModule), newName);
                 AssetDatabase.SaveAssets();
             }
            
             GUILayout.Space(10);
            
             // Module Settings
             GUILayout.Label("Default Settings", EditorStyles.boldLabel);
             
             // Voice Selector
             int defaultIndex = 0;
             if (availableVoices != null)
             {
                 var voiceList = availableVoices.Select(v => v.name).ToList();
                 voiceList.Insert(0, "Select Voice...");
                 string currentName = availableVoices.FirstOrDefault(v => v.voice_id == selectedModule.defaultVoiceId)?.name;
                 defaultIndex = voiceList.IndexOf(currentName);
                 if (defaultIndex == -1) defaultIndex = 0;
                 
                 int newIndex = EditorGUILayout.Popup("Default Voice", defaultIndex, voiceList.ToArray());
                 if (newIndex > 0)
                 {
                     selectedModule.defaultVoiceId = availableVoices[newIndex - 1].voice_id;
                     EditorUtility.SetDirty(selectedModule);
                 }
             }
             
             GUILayout.Space(10);
            
             // Voice Settings
             GUILayout.Label("Voice Settings", EditorStyles.boldLabel);
             EditorGUI.BeginChangeCheck();
             selectedModule.defaultVoiceSettings.stability = EditorGUILayout.Slider("Stability", selectedModule.defaultVoiceSettings.stability, 0f, 1f);
             selectedModule.defaultVoiceSettings.similarity_boost = EditorGUILayout.Slider("Clarity + Similarity", selectedModule.defaultVoiceSettings.similarity_boost, 0f, 1f);
             selectedModule.defaultVoiceSettings.style = EditorGUILayout.Slider("Style Exaggeration", selectedModule.defaultVoiceSettings.style, 0f, 1f);
             selectedModule.defaultVoiceSettings.use_speaker_boost = EditorGUILayout.Toggle("Speaker Boost", selectedModule.defaultVoiceSettings.use_speaker_boost);
             if (EditorGUI.EndChangeCheck())
             {
                 EditorUtility.SetDirty(selectedModule);
             }
             
             GUILayout.Space(20);
             
             // Actions
             GUILayout.Label("Bulk Actions", EditorStyles.boldLabel);
             GUILayout.BeginHorizontal();
             if (GUILayout.Button("Generate All Pending", GUILayout.Width(150)))
             {
                 GenerateModifiedSteps(selectedModule);
             }
             if (GUILayout.Button("Export All Audio...", GUILayout.Width(150)))
             {
                  ExportAllAudio(selectedModule);
             }
             GUILayout.EndHorizontal();
             
             GUILayout.Space(10);
             
             if (GUILayout.Button("Import Steps from Text/CSV...", GUILayout.Height(30)))
             {
                 ImportVoiceStepsPopup.Init((text, mode) => {
                     ImportStepsFromText(selectedModule, text, mode);
                 });
             }
             
             GUILayout.Space(20);
             EditorGUILayout.HelpBox($"This module contains {selectedModule.steps.Count} steps.\nSelect a step in the sidebar to edit it.", MessageType.Info);
             
             GUILayout.Space(20);
             GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Red
             if (GUILayout.Button("Delete Module", GUILayout.Height(30)))
             {
                 if (EditorUtility.DisplayDialog("Delete Module", 
                     $"Are you sure you want to delete '{selectedModule.name}' and all its steps?\nThis cannot be undone.", 
                     "Delete Forever", "Cancel"))
                 {
                     string path = AssetDatabase.GetAssetPath(selectedModule);
                     AssetDatabase.DeleteAsset(path);
                     foundModules.Remove(selectedModule);
                     if (expandedModules.Contains(selectedModule)) expandedModules.Remove(selectedModule);
                     selectedModule = null;
                     selectedStep = null;
                     AssetDatabase.Refresh();
                     RefreshModuleList(); // Ensure list is up to date
                     GUIUtility.ExitGUI();
                 }
             }
             GUI.backgroundColor = Color.white;
        }
        
        private void DrawSelectedStep()
        {
            if (selectedStep == null || selectedModule == null) return;
            
            GUILayout.Label($"Step: {selectedStep.title}", EditorStyles.boldLabel);
            
            EditorGUI.BeginChangeCheck();
            string newTitle = EditorGUILayout.TextField("Step Title", selectedStep.title);
            if (EditorGUI.EndChangeCheck())
            {
                selectedStep.title = newTitle;
                EditorUtility.SetDirty(selectedModule);
            }
            
            GUILayout.Space(10);
            
            // Voice Selector Row
            GUILayout.BeginHorizontal();
            GUILayout.Label("Voice:", GUILayout.Width(50));
            var voiceOptions = new List<string> { "Use Module Default" };
            if (voiceNames != null) voiceOptions.AddRange(voiceNames);
            
            int currentVoiceIndex = 0;
            if (!string.IsNullOrEmpty(selectedStep.assignedVoiceId) && availableVoices != null)
            {
                int vIndex = availableVoices.FindIndex(v => v.voice_id == selectedStep.assignedVoiceId);
                if (vIndex != -1) currentVoiceIndex = vIndex + 1;
            }

            EditorGUI.BeginChangeCheck();
            int newVoiceIndex = EditorGUILayout.Popup(currentVoiceIndex, voiceOptions.ToArray());
            if (EditorGUI.EndChangeCheck())
            {
                if (newVoiceIndex == 0) selectedStep.assignedVoiceId = "";
                else if (availableVoices != null && newVoiceIndex - 1 < availableVoices.Count) 
                    selectedStep.assignedVoiceId = availableVoices[newVoiceIndex - 1].voice_id;
                
                selectedStep.isDirty = true;
                EditorUtility.SetDirty(selectedModule);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.Label("Voice Content:", EditorStyles.label);
            
            // Text Content
            EditorGUI.BeginChangeCheck();
            GUIStyle areaStyle = new GUIStyle(EditorStyles.textArea);
            areaStyle.wordWrap = true;
            // Use remaining height for text area, but leave space for bottom buttons
            float minHeight = 200;
            selectedStep.voText = EditorGUILayout.TextArea(selectedStep.voText, areaStyle, GUILayout.MinHeight(minHeight));
            if (EditorGUI.EndChangeCheck())
            {
                selectedStep.isDirty = true;
                EditorUtility.SetDirty(selectedModule);
            }
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            // Status
             if (selectedStep.isDirty)
            {
                GUILayout.Label("Status: Modified (Needs Generation)", EditorStyles.miniLabel);
            }
            else if (selectedStep.generatedAudio != null)
            {
                GUILayout.Label("Status: Generated", EditorStyles.miniLabel);
            }
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            GUILayout.BeginHorizontal();
            
             // Generate
            if (selectedStep.isProcessing)
            {
                GUILayout.Box("Generating...", GUILayout.Width(100));
            }
            else
            {
                string btnLabel = "Generate Audio";
                if (!selectedStep.isDirty && selectedStep.generatedAudio != null) btnLabel = "Regenerate Audio";
                
                if (GUILayout.Button(btnLabel, GUILayout.Height(30)))
                {
                    GenerateSingleStep(selectedModule, selectedStep);
                }
            }
            
            // Delete
            GUI.backgroundColor = new Color(1f, 0.5f, 0.5f); // Soft Red
            if (GUILayout.Button(EditorGUIUtility.IconContent("TreeEditor.Trash"), GUILayout.Width(30), GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog("Delete Step", $"Are you sure you want to delete '{selectedStep.title}'?", "Delete", "Cancel"))
                {
                    selectedModule.steps.Remove(selectedStep);
                    selectedStep = null;
                    EditorUtility.SetDirty(selectedModule);
                    GUIUtility.ExitGUI(); 
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
            // Play Button 
            if (selectedStep.generatedAudio != null)
            {
                if (GUILayout.Button("▶ Play Current Audio", GUILayout.Height(40)))
                {
                   PlayAudio(selectedStep.generatedAudio, $"{selectedModule.name} - {selectedStep.title}");
                }
            }
        }


        private async System.Threading.Tasks.Task GenerateSingleStep(VoiceModule module, VoiceStep step)
        {
            string vId = !string.IsNullOrEmpty(step.assignedVoiceId) ? step.assignedVoiceId : module.defaultVoiceId;
            
            if (string.IsNullOrEmpty(vId) || vId.Contains(" ") || vId.Length > 50)
            {
                EditorUtility.DisplayDialog("Invalid Voice ID", "Please check the Step settings or Module Default Voice.", "OK");
                return;
            }
            
            step.isProcessing = true;
            Repaint();
            
            try 
            {
                var rawClip = await ElevenLabsAPI.GenerateVoiceAsync(step.voText, vId, module.defaultVoiceSettings);
                if (rawClip != null)
                {
                    string modFolder = $"Assets/ElevenLabs/Generated/{module.name}";
                    if (!System.IO.Directory.Exists(modFolder)) System.IO.Directory.CreateDirectory(modFolder);
                    
                    int stepIndex = module.steps.IndexOf(step) + 1;
                    string cleanTitle = System.Text.RegularExpressions.Regex.Replace(step.title, "[^a-zA-Z0-9 _-]", "");
                    cleanTitle = cleanTitle.Length > 30 ? cleanTitle.Substring(0, 30) : cleanTitle;
                    
                    string fileName = $"{modFolder}/step{stepIndex}_{cleanTitle.Trim()}.wav";
                     if (SavWav.Save(fileName, rawClip))
                    {
                        AssetDatabase.Refresh();
                        step.generatedAudio = AssetDatabase.LoadAssetAtPath<AudioClip>(fileName);
                        step.isDirty = false;
                        step.lastGeneratedTime = System.DateTime.Now.ToString();
                        EditorUtility.SetDirty(module);
                        AssetDatabase.SaveAssets();
                        
                        // Auto-load to player
                        PlayAudio(step.generatedAudio, $"{module.name} - {step.title}");
                    }
                }
            }
            finally
            {
                step.isProcessing = false;
                Repaint();
            }
        }

        private async void GenerateModifiedSteps(VoiceModule module)
        {
             if (isGenerating) return;
             isGenerating = true;
             
             try 
             {
                 var dirtySteps = module.steps.Where(s => s.isDirty).ToList();
                 int total = dirtySteps.Count;
                 
                 if (total == 0)
                 {
                     if (EditorUtility.DisplayDialog("Generate All", "No pending steps. Regenerate ALL?", "Yes", "Cancel"))
                     {
                         dirtySteps = module.steps.ToList();
                         total = dirtySteps.Count;
                     }
                     else return;
                 }
                 
                 for(int i = 0; i < total; i++)
                 {
                     var step = dirtySteps[i];
                     currentProgress = (float)i / total;
                     progressInfo = $"Processing step {i+1} of {total}: {step.title}";
                     Repaint();
                     
                     await GenerateSingleStep(module, step);
                     await System.Threading.Tasks.Task.Delay(250);
                 }
             }
             finally
             {
                 isGenerating = false;
                 currentProgress = 0f;
                 progressInfo = "";
                 AssetDatabase.Refresh();
                 Repaint();
             }
        }
        
        private void ImportStepsFromText(VoiceModule module, string text, ImportVoiceStepsPopup.ImportMode mode)
        {
            // Same import logic as before, essentially
             string[] lines = text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;
            
            // Simple Parsing for brevity (complex CSV logic preserved from original if needed, but simplified here for robustness)
            // Assuming the helper class logic handles the mode enum, we just need the parser.
            // ... copying main parser logic ...
            
             int nameCol = 0;
            int textCol = 1;
            int voiceCol = -1;
            bool skipFirstLine = false;

            if (mode == ImportVoiceStepsPopup.ImportMode.CSV && lines.Length > 0)
            {
                string csvPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";
                string[] headers = System.Text.RegularExpressions.Regex.Split(lines[0], csvPattern);
                for (int i = 0; i < headers.Length; i++)
                {
                    string h = headers[i].Trim().Trim('"').ToLower(); 
                    if (h.Contains("name") || h.Contains("title")) nameCol = i;
                    if (h.Contains("text") || h.Contains("content")) textCol = i;
                    else if (h.Contains("voice") || h.Contains("id")) voiceCol = i;
                }
                skipFirstLine = true; 
            }

            for (int i = 0; i < lines.Length; i++)
            {
                if (skipFirstLine && i == 0) continue;
                string line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                string stepName = $"Step {module.steps.Count + 1}";
                string content = line.Trim();
                string voiceId = "";

                if (mode == ImportVoiceStepsPopup.ImportMode.ScriptFormat)
                {
                    int colonIndex = line.IndexOf(':');
                    if (colonIndex > 0)
                    {
                        stepName = line.Substring(0, colonIndex).Trim();
                        content = line.Substring(colonIndex + 1).Trim();
                    }
                }
                else if (mode == ImportVoiceStepsPopup.ImportMode.CSV)
                {
                    string csvPattern = ",(?=(?:[^\"]*\"[^\"]*\")*[^\"]*$)";
                    var cols = System.Text.RegularExpressions.Regex.Split(line, csvPattern);
                    if (cols.Length > nameCol) stepName = cols[nameCol].Trim().Trim('"');
                    if (cols.Length > textCol) content = cols[textCol].Trim().Trim('"').Replace("\"\"", "\"");
                    if (voiceCol != -1 && cols.Length > voiceCol) voiceId = cols[voiceCol].Trim().Trim('"');
                }

                module.steps.Add(new VoiceStep { 
                    title = stepName, 
                    voText = content,
                    assignedVoiceId = voiceId,
                    isDirty = true 
                });
                addedCount++;
            }

            if (addedCount > 0)
            {
                EditorUtility.SetDirty(module);
                EditorUtility.DisplayDialog("Import Complete", $"Imported {addedCount} steps.", "OK");
            }
        }
        #endregion

        #region Auth
        private async void FetchVoices()
        {
            var voices = await ElevenLabsAPI.GetVoicesAsync();
            if (voices != null)
            {
                var config = ElevenLabsConfig.FindOrCreate();
                if (config != null && config.customVoices != null)
                {
                    voices.AddRange(config.customVoices);
                }
                availableVoices = voices;
                voiceNames = availableVoices.Select(v => $"{v.name} ({v.category})").ToArray();
                Repaint();
            }
        }
        
        private void DrawLoginUI()
        {
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

        private async void VerifyCredentials()
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
                    // Cleanup the clip if needed, though GenerateVoiceAsync returns a temp one
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
                isAuthenticated = true;
                FetchVoices();
                FetchHistory();
                
                // If we are in settings tab (tab 2), remain there
            }
            else
            {
                verificationError = "Could not verify full access. Please check your API permissions.";
            }

            Repaint();
        }

        private void DrawSettingsUI()
        {
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
                ElevenLabsUtilities.Logout();
                isAuthenticated = false;
                apiKey = "";
                availableVoices.Clear();
                selectedTab = 0; // Reset tab
            }
            GUI.backgroundColor = Color.white;
            
            GUILayout.EndVertical();
        }
        #endregion
        
        private void ExportAllAudio(VoiceModule module)
        {
             // 1. Get Save Path
             string defaultName = $"{module.name}_Voices.zip";
             string savePath = EditorUtility.SaveFilePanel("Export Voices to ZIP", "", defaultName, "zip");
             
             if (string.IsNullOrEmpty(savePath)) return;
             
             // 2. Collect Files
             var validSteps = module.steps.Where(s => s.generatedAudio != null).ToList();
             if (validSteps.Count == 0)
             {
                 EditorUtility.DisplayDialog("Export Failed", "No generated audio files found in this module.", "OK");
                 return;
             }

             // 3. Create ZIP
             try 
             {
                 // Delete if exists (SaveFilePanel usually handles overwrite confirmation, but good to be safe)
                 if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);

                 using (var archive = ZipFile.Open(savePath, ZipArchiveMode.Create))
                 {
                     int successCount = 0;
                     foreach(var step in validSteps)
                     {
                         string assetPath = AssetDatabase.GetAssetPath(step.generatedAudio);
                         if (string.IsNullOrEmpty(assetPath)) continue;
                         
                         // We pull the raw disk path to bypass Unity's internal asset handling if needed, 
                         // but standard File.Copy works fine with relative asset paths in Editor.
                         // However, for ZipFile.CreateEntryFromFile, we generally want full system paths 
                         // or valid relative paths.
                         
                         // Get filename
                         string fileName = System.IO.Path.GetFileName(assetPath);
                         
                         // Add Key-Value entry: [SourceFile, EntryName]
                         archive.CreateEntryFromFile(assetPath, fileName);
                         successCount++;
                     }
                     
                     EditorUtility.DisplayDialog("Export Complete", $"Successfully zipped {successCount} files to:\n{savePath}", "OK");
                     EditorUtility.RevealInFinder(savePath);
                 }
             }
             catch (System.Exception e)
             {
                 Debug.LogError($"[ElevenLabs] Export failed: {e.Message}");
                 EditorUtility.DisplayDialog("Export Error", $"Failed to create ZIP file:\n{e.Message}", "OK");
             }
        }
    }

    public class CustomVoicePopup : EditorWindow
    {
        private string voiceId = "";
        private string voiceName = "";
        private System.Action<string, string> onAdd;

        public static void Init(System.Action<string, string> onAddCallback)
        {
            CustomVoicePopup window = ScriptableObject.CreateInstance<CustomVoicePopup>();
            window.titleContent = new GUIContent("Add Custom Voice");
            window.onAdd = onAddCallback;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 300, 150);
            window.ShowUtility();
        }

        void OnGUI()
        {
            GUILayout.Label("Add Custom Voice", EditorStyles.boldLabel);
            voiceName = EditorGUILayout.TextField("Voice Name", voiceName);
            voiceId = EditorGUILayout.TextField("Voice ID", voiceId);
            
            GUILayout.Space(20);
            if (GUILayout.Button("Add"))
            {
                onAdd?.Invoke(voiceId, voiceName);
                Close();
            }
        }
    }

    public class ImportVoiceStepsPopup : EditorWindow
    {
        public enum ImportMode { SimpleLines, ScriptFormat, CSV }
        private string textToImport = "";
        private ImportMode mode = ImportMode.SimpleLines;
        private System.Action<string, ImportMode> onImport;

        public static void Init(System.Action<string, ImportMode> onCallback)
        {
            ImportVoiceStepsPopup window = ScriptableObject.CreateInstance<ImportVoiceStepsPopup>();
            window.titleContent = new GUIContent("Import Steps");
            window.onImport = onCallback;
            window.position = new Rect(Screen.width / 2, Screen.height / 2, 400, 350);
            window.ShowUtility();
        }

        void OnGUI()
        {
            GUILayout.Label("Import Steps", EditorStyles.boldLabel);
            if (GUILayout.Button("Load from File"))
            {
                string path = EditorUtility.OpenFilePanel("Open Script", "", "csv,txt");
                if (!string.IsNullOrEmpty(path))
                {
                    textToImport = System.IO.File.ReadAllText(path);
                    if (path.EndsWith(".csv")) mode = ImportMode.CSV;
                }
            }
            
            textToImport = EditorGUILayout.TextArea(textToImport, GUILayout.Height(150));
            mode = (ImportMode)EditorGUILayout.EnumPopup("Mode", mode);
            
            if (GUILayout.Button("Import"))
            {
                onImport?.Invoke(textToImport, mode);
                Close();
            }
        }
    }
}