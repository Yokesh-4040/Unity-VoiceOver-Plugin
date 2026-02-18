using UnityEngine;
using UnityEditor;
using FF.ElevenLabs.Editor.Styles;

namespace FF.ElevenLabs.Editor.Components
{
    public class ElevenLabsAudioPlayer
    {
        private EditorWindow hostWindow;
        private AudioClip currentPlayingClip;
        private string currentPlayingTitle = "No Audio Selected";
        private UnityEditor.Editor audioClipEditor;
        private bool autoPlayAudio = true;

        public bool AutoPlayAudio { get => autoPlayAudio; set => autoPlayAudio = value; }

        public ElevenLabsAudioPlayer(EditorWindow window)
        {
            this.hostWindow = window;
        }

        public void OnDisable()
        {
            StopAllClips();
            if (audioClipEditor != null)
            {
                Object.DestroyImmediate(audioClipEditor);
            }
        }

        public void Update()
        {
            if (currentPlayingClip != null && IsClipPlaying())
            {
                hostWindow.Repaint();
            }
        }

        public void DrawBottomPlayer(float width, float height)
        {
            // Ensure Styles are Init
            ElevenLabsEditorStyles.Init();

            float playerHeight = 160f;
            Rect playerRect = new Rect(0, height - playerHeight, width, playerHeight);
            
            // Separator
            EditorGUI.DrawRect(new Rect(0, playerRect.y - 1, width, 1), new Color(0.1f, 0.1f, 0.1f));
            
            GUILayout.BeginArea(playerRect, ElevenLabsEditorStyles.ContentStyle);
            
            GUILayout.BeginVertical();
            
            // Top Row: Title + Controls
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.Label("Audio Player", EditorStyles.boldLabel);
            
            GUIStyle wrappedMini = new GUIStyle(EditorStyles.miniLabel);
            wrappedMini.wordWrap = true;
            GUILayout.Label(currentPlayingTitle, wrappedMini, GUILayout.MaxWidth(width - 250));
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            
            if (currentPlayingClip != null)
            {
                 bool isPlaying = IsClipPlaying();
                 string playIcon = isPlaying ? "PauseButton" : "PlayButton";
                 
                 // Play/Pause
                 if (GUILayout.Button(EditorGUIUtility.IconContent(playIcon), GUILayout.Width(40), GUILayout.Height(30)))
                 {
                     if (isPlaying) 
                     {
                         StopAllClips();
                         hostWindow.Repaint(); 
                     }
                     else 
                     {
                         LoadAudio(currentPlayingClip, currentPlayingTitle, autoPlay: true);
                     }
                 }
                 
                 // Save
                 if (GUILayout.Button(EditorGUIUtility.IconContent("SaveAs"), GUILayout.Width(40), GUILayout.Height(30)))
                 {
                     SaveCurrentClip();
                 }
            }
            GUILayout.EndHorizontal();


            if (currentPlayingClip != null && audioClipEditor != null)
            {
                 Rect previewRect = GUILayoutUtility.GetRect(width - 20, 80);
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
            else
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("No audio selected. Generate voice or select from history.", EditorStyles.centeredGreyMiniLabel);
                GUILayout.FlexibleSpace();
            }
            GUILayout.EndVertical();
            
            GUILayout.EndArea();
        }

        public void LoadAudio(AudioClip clip, string title, bool autoPlay = false)
        {
            if (clip == null) 
            {
                Debug.LogWarning("[ElevenLabs] Cannot load null audio clip");
                return;
            }
            
            StopAllClips();
            
            currentPlayingClip = clip;
            currentPlayingTitle = title;
            
            if (audioClipEditor != null) Object.DestroyImmediate(audioClipEditor);
            audioClipEditor = UnityEditor.Editor.CreateEditor(currentPlayingClip);
            
            if (autoPlay || autoPlayAudio) // Use the parameter OR the preference? The original code logic was `if (autoPlay)`. I added the property `autoPlayAudio` but usually that controls the `autoPlay` arg passed in.
            {
                 // Re-reading original: `LoadAudioToPlayer` had `bool autoPlay = false`. 
                 // Usage: `LoadAudioToPlayer(..., autoPlay: true)` when generating or clicking history.
                 // Usage: `LoadAudioToPlayer(..., autoPlay: true)` in play button.
                 // So the `autoPlayAudio` bool (preference) was used at the CALL SITE to determine if `autoPlay` arg should be true.
                 // I will mimic that behavior. The method just respects the arg.
                if (autoPlay) PlayClip(clip);
            }
            
            hostWindow.Repaint();
        }

        private void StopAllClips()
        {
            try
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var type = assembly.GetType("UnityEditor.AudioUtil");
                var method = type.GetMethod("StopAllPreviewClips", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                if (method != null) method.Invoke(null, null);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ElevenLabs] Failed to stop audio clips: {e.Message}");
            }
        }

        private void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            try
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var type = assembly.GetType("UnityEditor.AudioUtil");
                var method = type.GetMethod("PlayPreviewClip", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(AudioClip), typeof(int), typeof(bool) }, null);
                if (method != null) 
                {
                    method.Invoke(null, new object[] { clip, startSample, loop });
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ElevenLabs] Failed to play audio clip: {e.Message}");
            }
        }

        private bool IsClipPlaying()
        {
            if (currentPlayingClip == null) return false;
            
            try
            {
                var assembly = typeof(UnityEditor.Editor).Assembly;
                var type = assembly.GetType("UnityEditor.AudioUtil");
                
                var method = type.GetMethod("IsPreviewClipPlaying", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(AudioClip) }, null);
                if (method != null) return (bool)method.Invoke(null, new object[] { currentPlayingClip });
                
                method = type.GetMethod("IsClipPlaying", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, new System.Type[] { typeof(AudioClip) }, null);
                if (method != null) return (bool)method.Invoke(null, new object[] { currentPlayingClip });
                
                method = type.GetMethod("IsPreviewClipPlaying", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public, null, System.Type.EmptyTypes, null);
                if (method != null) return (bool)method.Invoke(null, null);
                
                return false;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ElevenLabs] Failed to check if clip is playing: {e.Message}");
                return false;
            }
        }

        private void SaveCurrentClip()
        {
            if (currentPlayingClip == null) return;

            string folderName = "Assets/ElevenLabs/GeneratedVoices";
            if (!System.IO.Directory.Exists(folderName)) System.IO.Directory.CreateDirectory(folderName);
            
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
    }
}
