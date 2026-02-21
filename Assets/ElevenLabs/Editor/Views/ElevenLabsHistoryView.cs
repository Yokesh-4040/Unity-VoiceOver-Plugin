using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using FF.ElevenLabs.Editor.Styles;
using FF.ElevenLabs.Editor.Components;
using FF.ElevenLabs;

namespace FF.ElevenLabs.Editor.Views
{
    public class ElevenLabsHistoryView
    {
        private ElevenLabsAudioPlayer audioPlayer;
        private List<HistoryItemApiModel> apiHistory = new List<HistoryItemApiModel>();
        private HistoryItemApiModel selectedHistoryItem;

        public ElevenLabsHistoryView(ElevenLabsAudioPlayer player)
        {
            this.audioPlayer = player;
        }

        public void OnEnable()
        {
            FetchHistory();
        }

        public async void FetchHistory()
        {
            var history = await ElevenLabsAPI.GetHistoryAsync();
            if (history != null)
            {
                apiHistory = history;
                // We might need to repaint here, but we don't have direct access to Window.Repaint() easily without passing it.
                // However, the main window loop or user interaction usually covers it. 
                // If async updates don't show, we might need a callback or event.
                // For now, let's assume the user hovering or clicking will trigger repaint, 
                // but for async fetch completion, we really should repaint.
                // I'll add a Repaint callback/event if needed, but for now `ElevenLabsAudioPlayer` has a reference to Window, 
                // maybe I should pass Window to View as well? Yes.
            }
        }

        public void DrawList()
        {
            ElevenLabsEditorStyles.Init();

            GUILayout.BeginHorizontal();
            GUILayout.Label("History Items", EditorStyles.boldLabel);
            GUILayout.FlexibleSpace();
            if(GUILayout.Button("Refresh", EditorStyles.miniButton)) FetchHistory();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (apiHistory == null || apiHistory.Count == 0)
            {
                GUILayout.Label("No history.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            foreach (var item in apiHistory)
            {
                // Compact List Item
                bool isSelected = selectedHistoryItem == item;
                GUIStyle rowStyle = isSelected ? ElevenLabsEditorStyles.TabBtnActiveStyle : EditorStyles.label;
                
                // Truncate text for sidebar
                string text = item.text.Length > 25 ? item.text.Substring(0, 25) + "..." : item.text;
                string label = $"{item.voice_name}\n<size=10>{text}</size>";
                
                if (GUILayout.Button(label, rowStyle, GUILayout.Height(40)))
                {
                    selectedHistoryItem = item;
                }
            }
        }

        public void DrawDetail()
        {
            ElevenLabsEditorStyles.Init();

            if (selectedHistoryItem != null && !string.IsNullOrEmpty(selectedHistoryItem.history_item_id))
            {
                GUILayout.Label("History Details", EditorStyles.boldLabel);
                GUILayout.Space(10);
                
                EditorGUILayout.HelpBox(selectedHistoryItem.text, MessageType.None);
                GUILayout.Space(10);
            
                GUILayout.Label($"Voice: {selectedHistoryItem.voice_name}");
                System.DateTime date = System.DateTimeOffset.FromUnixTimeSeconds(selectedHistoryItem.date_unix).LocalDateTime;
                GUILayout.Label($"Date: {date:yyyy-MM-dd HH:mm:ss}");
                GUILayout.Label($"ID: {selectedHistoryItem.history_item_id}");
                
                GUILayout.Space(20);
                
                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Play Audio", GUILayout.Height(30)))
                {
                    LoadHistoryItem(selectedHistoryItem);
                }
                if (GUILayout.Button("Download", GUILayout.Height(30)))
                {
                    DownloadHistoryAudio(selectedHistoryItem);
                }
                GUILayout.EndHorizontal();
            }
            else
            {
                EditorGUILayout.HelpBox("Select a history item to view details.", MessageType.Info);
            }
        }

        private async void LoadHistoryItem(HistoryItemApiModel item)
        {
             if (string.IsNullOrEmpty(item.history_item_id)) return;
             
             var clip = await ElevenLabsAPI.GetHistoryAudioAsync(item.history_item_id);
             if (clip != null)
             {
                 // Ensure clip is loaded before playing
                 while (clip.loadState == AudioDataLoadState.Loading)
                 {
                     await System.Threading.Tasks.Task.Delay(10);
                 }
                 
                 if (clip.loadState == AudioDataLoadState.Loaded)
                 {
                    audioPlayer.LoadAudio(clip, $"{item.voice_name} - {item.history_item_id}", autoPlay: true);
                 }
                 else
                 {
                     Debug.LogError($"[ElevenLabs] Failed to load audio clip for {item.history_item_id}");
                 }
             }
        }

        private async void DownloadHistoryAudio(HistoryItemApiModel item)
        {
            if (string.IsNullOrEmpty(item.history_item_id)) return;

            string safeName = System.Text.RegularExpressions.Regex.Replace(item.text, "[^a-zA-Z0-9]", "_");
            if (safeName.Length > 50) safeName = safeName.Substring(0, 50);
            if (string.IsNullOrEmpty(safeName)) safeName = item.history_item_id;

            string fileName = $"{safeName}.mp3"; 
            string savePath = EditorUtility.SaveFilePanel("Save Audio", "", fileName, "mp3");

            if (string.IsNullOrEmpty(savePath)) return;

            var audioData = await ElevenLabsAPI.GetHistoryAudioBytesAsync(item.history_item_id);
            if (audioData != null)
            {
                try
                {
                    System.IO.File.WriteAllBytes(savePath, audioData);
                    EditorUtility.DisplayDialog("Download Complete", $"Saved to:\n{savePath}", "OK");
                    EditorUtility.RevealInFinder(savePath);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[ElevenLabs] Save failed: {e.Message}");
                    EditorUtility.DisplayDialog("Error", $"Failed to save file:\n{e.Message}", "OK");
                }
            }
        }
    }
}
