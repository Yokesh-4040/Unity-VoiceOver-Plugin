using UnityEngine;
using UnityEditor;
using System.IO;

namespace FF.ElevenLabs.Editor
{
    public static class ElevenLabsUtilities
    {
        private const string API_KEY_PREF = "ElevenLabs_APIKey_V1";
        
        public static void SaveAPIKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return;
            // Simple obfuscation to prevent casual reading in registry/prefs
            // For production, use a more secure local storage solution
            EditorPrefs.SetString(API_KEY_PREF, System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey)));
        }

        public static string GetAPIKey()
        {
            string key = EditorPrefs.GetString(API_KEY_PREF, "");
            if (string.IsNullOrEmpty(key)) return "";
            try
            {
                return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(key));
            }
            catch
            {
                return "";
            }
        }

        public static bool HasAPIKey()
        {
            return !string.IsNullOrEmpty(GetAPIKey());
        }

        public static void Logout()
        {
            EditorPrefs.DeleteKey(API_KEY_PREF);
        }

        public static void OpenWebsite(string url)
        {
            Application.OpenURL(url);
        }
    }
}
