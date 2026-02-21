using UnityEngine;
using UnityEditor;
using System.IO;

namespace FF.ElevenLabs.Editor
{
    public static class ElevenLabsUtilities
    {
        private const string API_KEY_PREF = "ElevenLabs_APIKey_V1";
        private const string SARVAM_API_KEY_PREF = "SarvamAI_APIKey_V1";
        
        public static void SaveAPIKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return;
            EditorPrefs.SetString(API_KEY_PREF, System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey)));
        }

        public static string GetAPIKey()
        {
            string key = EditorPrefs.GetString(API_KEY_PREF, "");
            if (string.IsNullOrEmpty(key)) return "";
            try { return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(key)); }
            catch { return ""; }
        }

        public static bool HasAPIKey()
        {
            return !string.IsNullOrEmpty(GetAPIKey());
        }

        public static void SaveSarvamAPIKey(string apiKey)
        {
            if (string.IsNullOrEmpty(apiKey)) return;
            EditorPrefs.SetString(SARVAM_API_KEY_PREF, System.Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(apiKey)));
        }

        public static string GetSarvamAPIKey()
        {
            string key = EditorPrefs.GetString(SARVAM_API_KEY_PREF, "");
            if (string.IsNullOrEmpty(key)) return "";
            try { return System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(key)); }
            catch { return ""; }
        }

        public static bool HasSarvamAPIKey()
        {
            return !string.IsNullOrEmpty(GetSarvamAPIKey());
        }

        public static void Logout()
        {
            EditorPrefs.DeleteKey(API_KEY_PREF);
            EditorPrefs.DeleteKey(SARVAM_API_KEY_PREF);
        }


        public static void OpenWebsite(string url)
        {
            Application.OpenURL(url);
        }
    }
}
