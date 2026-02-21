using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System.IO.Compression;
using FF.Voiceover.Editor.Styles;
using FF.Voiceover.Editor.Components;
using FF.Voiceover.Editor.Popups;
using FF.Voiceover;

namespace FF.Voiceover.Editor.Views
{
    public class VoiceoverModulesView
    {
        private EditorWindow hostWindow;
        private VoiceoverAudioPlayer audioPlayer;
        private List<Voice> availableVoices;
        private string[] voiceNames;

        private List<VoiceModule> foundModules = new List<VoiceModule>();
        private HashSet<VoiceModule> expandedModules = new HashSet<VoiceModule>();
        
        private VoiceModule selectedModule;
        private VoiceStep selectedStep;

        private bool isGenerating = false;
        private float currentProgress = 0f;
        private string progressInfo = "";

        // Public Accessors
        public VoiceModule SelectedModule { get => selectedModule; private set => selectedModule = value; }
        public VoiceStep SelectedStep { get => selectedStep; private set => selectedStep = value; }

        public VoiceoverModulesView(EditorWindow window, VoiceoverAudioPlayer player)
        {
            this.hostWindow = window;
            this.audioPlayer = player;
            RefreshModuleList();
        }

        public void SetAvailableVoices(List<Voice> voices)
        {
            this.availableVoices = voices;
            if (availableVoices != null)
            {
                this.voiceNames = availableVoices.Select(v => $"{v.name} ({v.category})").ToArray();
            }
        }

        public void OnGUI()
        {
             // This method delegates to specific draw methods based on selection state, 
             // but logic here implies the Main Window decides what to draw based on tab selection.
             // The Main Window calls specific methods like DrawModuleList or DrawDetail.
        }

        public void DrawModuleList()
        {
            VoiceoverEditorStyles.Init();

            GUILayout.Label("Modules", EditorStyles.boldLabel);
            
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("+ New Module", EditorStyles.miniButton)) CreateNewModule();
            if (GUILayout.Button("Refresh", EditorStyles.miniButton)) RefreshModuleList();
            GUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            
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
                Rect moduleRect = GUILayoutUtility.GetRect(new GUIContent(mod.name), EditorStyles.label, GUILayout.Height(20));
                
                if (selectedModule == mod && selectedStep == null)
                {
                    EditorGUI.DrawRect(moduleRect, new Color(0.24f, 0.49f, 0.9f, 0.5f)); // Selection Blue
                }
                
                if (GUI.Button(moduleRect, mod.name, EditorStyles.label))
                {
                    selectedModule = mod;
                    selectedStep = null; // Deselect step when module is selected
                    GUI.FocusControl(null);
                    if (hostWindow is VoiceoverEditorWindow w) w.selectedTab = 0;
                }
                
                // Add Step Shortcut
                if (GUILayout.Button("+", EditorStyles.miniButton, GUILayout.Width(20)))
                {
                    mod.steps.Add(new VoiceStep { title = $"Step {mod.steps.Count + 1}" });
                    EditorUtility.SetDirty(mod);
                    expandedModules.Add(mod); // Auto expand
                    if (hostWindow is VoiceoverEditorWindow w) w.selectedTab = 0;
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
                        
                        // Double-click to highlight in Project
                        if (Event.current.type == EventType.MouseDown && r.Contains(Event.current.mousePosition) && Event.current.clickCount == 2)
                        {
                            if (step.generatedAudio != null)
                            {
                                EditorGUIUtility.PingObject(step.generatedAudio);
                                Selection.activeObject = step.generatedAudio;
                                Event.current.Use();
                            }
                        }
                        
                        if (GUI.Button(r, displayTitle, EditorStyles.label))
                        {
                            selectedModule = mod;
                            selectedStep = step;
                            GUI.FocusControl(null);
                            if (hostWindow is VoiceoverEditorWindow w) w.selectedTab = 0;
                            
                            // Auto-play on select
                            if (selectedStep.generatedAudio != null)
                            {
                                audioPlayer.LoadAudio(selectedStep.generatedAudio, $"{selectedModule.name} - {selectedStep.title}", autoPlay: true);
                            }
                        }
                        
                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        public void DrawDetail()
        {
             if (selectedStep != null) DrawSelectedStep();
             else if (selectedModule != null) DrawSelectedModule();
             else EditorGUILayout.HelpBox("Select a Module or Step from the sidebar.", MessageType.Info);
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
            string path = "Assets/Voiceover/VoiceModules";
            if (!System.IO.Directory.Exists(path)) System.IO.Directory.CreateDirectory(path);
            
            VoiceModule newMod = ScriptableObject.CreateInstance<VoiceModule>();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath($"{path}/NewVoiceModule.asset");
            
            AssetDatabase.CreateAsset(newMod, uniquePath);
            AssetDatabase.SaveAssets();
            
            RefreshModuleList();
            selectedModule = newMod;
            selectedStep = null;
            if (hostWindow is VoiceoverEditorWindow w) w.selectedTab = 0;
        }

        private void DrawSelectedModule()
        {
             VoiceoverEditorStyles.Init();

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
            
            VoiceoverEditorStyles.Init();

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
            hostWindow.Repaint();
            
            try 
            {
                var config = VoiceoverConfig.FindOrCreate();
                AudioClip rawClip = null;

                if (config.activeProvider == VoiceoverConfig.VoiceProvider.Voiceover)
                {
                    rawClip = await VoiceoverAPI.GenerateVoiceAsync(step.voText, vId, module.defaultVoiceSettings);
                }
                else
                {
                    rawClip = await SarvamAIAPI.GenerateVoiceAsync(step.voText, vId);
                }

                if (rawClip != null)
                {
                    string modFolder = $"Assets/Voiceover/Generated/{module.name}";
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
                        if (audioPlayer != null && audioPlayer.AutoPlayAudio) // Check preference
                        {
                            audioPlayer.LoadAudio(step.generatedAudio, $"{module.name} - {step.title}", autoPlay: true);
                        }
                    }
                }
            }
            finally
            {
                step.isProcessing = false;
                hostWindow.Repaint();
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
                     hostWindow.Repaint();
                     
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
                 hostWindow.Repaint();
             }
        }

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
                 if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath);

                 using (var archive = ZipFile.Open(savePath, ZipArchiveMode.Create))
                 {
                     int successCount = 0;
                     foreach(var step in validSteps)
                     {
                         string assetPath = AssetDatabase.GetAssetPath(step.generatedAudio);
                         if (string.IsNullOrEmpty(assetPath)) continue;
                         
                         // Sanitize filename from text
                         string safeName = System.Text.RegularExpressions.Regex.Replace(step.title, "[^a-zA-Z0-9]", "_");
                         if (safeName.Length > 50) safeName = safeName.Substring(0, 50);
                         if (string.IsNullOrEmpty(safeName)) safeName = $"Audio_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
                         
                         string fileName = $"{safeName}.mp3"; // Or .wav depending on source, but assuming .mp3 for simplicity or consistent export
                         // Actually, let's keep original extension to be safe
                         string ext = System.IO.Path.GetExtension(assetPath);
                         fileName = $"{safeName}{ext}";

                         // Ensure unique names in zip
                         // (Simple handling: if duplicate, zip lib might throw or overwrite. 
                         //  For now, assuming titles are somewhat unique or just letting it happen)
                         
                         archive.CreateEntryFromFile(assetPath, fileName);
                         successCount++;
                     }
                     
                     EditorUtility.DisplayDialog("Export Complete", $"Successfully zipped {successCount} files to:\n{savePath}", "OK");
                     EditorUtility.RevealInFinder(savePath);
                 }
             }
             catch (System.Exception e)
             {
                 Debug.LogError($"[Voiceover] Export failed: {e.Message}");
                 EditorUtility.DisplayDialog("Export Error", $"Failed to create ZIP file:\n{e.Message}", "OK");
             }
        }

        private void ImportStepsFromText(VoiceModule module, string text, ImportVoiceStepsPopup.ImportMode mode)
        {
             string[] lines = text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            int addedCount = 0;
            
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
    }
}
