using UnityEngine;
using UnityEditor;

namespace FF.ElevenLabs.Editor.Styles
{
    public static class ElevenLabsEditorStyles
    {
        public static GUIStyle SidebarStyle { get; private set; }
        public static GUIStyle ContentStyle { get; private set; }
        public static GUIStyle CardStyle { get; private set; }
        public static GUIStyle HeaderStyle { get; private set; }
        public static GUIStyle TabBtnStyle { get; private set; }
        public static GUIStyle TabBtnActiveStyle { get; private set; }
        public static GUIStyle PrimaryBtnStyle { get; private set; }
        public static GUIStyle GoldenBtnStyle { get; private set; }

        private static bool isInitialized = false;

        public static void Init()
        {
            if (isInitialized && SidebarStyle != null) return;

            Color sidebarColor = new Color(0.18f, 0.18f, 0.18f);
            Color contentColor = new Color(0.22f, 0.22f, 0.22f);
            Color cardColor = new Color(0.28f, 0.28f, 0.28f);

            SidebarStyle = new GUIStyle();
            SidebarStyle.normal.background = MakeTex(sidebarColor);
            SidebarStyle.padding = new RectOffset(10, 10, 10, 10);

            ContentStyle = new GUIStyle();
            ContentStyle.normal.background = MakeTex(contentColor);
            ContentStyle.padding = new RectOffset(15, 15, 15, 15);

            CardStyle = new GUIStyle(EditorStyles.helpBox);
            CardStyle.normal.background = MakeTex(cardColor);
            CardStyle.padding = new RectOffset(10, 10, 10, 10);
            CardStyle.border = new RectOffset(0, 0, 0, 0);

            HeaderStyle = new GUIStyle();
            HeaderStyle.normal.background = MakeTex(new Color(0.12f, 0.12f, 0.12f));
            HeaderStyle.padding = new RectOffset(10, 10, 10, 10);

            TabBtnStyle = new GUIStyle(EditorStyles.label);
            TabBtnStyle.alignment = TextAnchor.MiddleCenter;
            TabBtnStyle.fontSize = 12;
            TabBtnStyle.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
            TabBtnStyle.hover.textColor = Color.white;

            TabBtnActiveStyle = new GUIStyle(TabBtnStyle);
            TabBtnActiveStyle.normal.textColor = Color.white;
            TabBtnActiveStyle.fontStyle = FontStyle.Bold;
            TabBtnActiveStyle.normal.background = MakeTex(new Color(0.3f, 0.3f, 0.3f));

            PrimaryBtnStyle = new GUIStyle(EditorStyles.miniButton);
            PrimaryBtnStyle.normal.textColor = Color.white;
            PrimaryBtnStyle.fontStyle = FontStyle.Bold;
            PrimaryBtnStyle.fixedHeight = 30;

            InitGoldenStyle();

            isInitialized = true;
        }

        private static void InitGoldenStyle()
        {
            GoldenBtnStyle = new GUIStyle(EditorStyles.miniButton);

            // Deep Gold for Normal
            var goldTex = MakeTex(new Color(1f, 0.84f, 0f));
            GoldenBtnStyle.normal.background = goldTex;
            GoldenBtnStyle.normal.scaledBackgrounds = new Texture2D[] { goldTex };

            // Brighter Gold for Hover
            var hoverTex = MakeTex(new Color(1f, 0.9f, 0.2f));
            GoldenBtnStyle.hover.background = hoverTex;
            GoldenBtnStyle.hover.scaledBackgrounds = new Texture2D[] { hoverTex };

            // Darker Gold for Active/Press
            var activeTex = MakeTex(new Color(0.85f, 0.65f, 0f));
            GoldenBtnStyle.active.background = activeTex;
            GoldenBtnStyle.active.scaledBackgrounds = new Texture2D[] { activeTex };

            // Text Styling
            GoldenBtnStyle.normal.textColor = new Color(0.1f, 0.1f, 0.1f); // Almost black
            GoldenBtnStyle.hover.textColor = Color.black;
            GoldenBtnStyle.active.textColor = Color.black;

            GoldenBtnStyle.fontStyle = FontStyle.Bold;
            GoldenBtnStyle.fontSize = 12;
            GoldenBtnStyle.alignment = TextAnchor.MiddleCenter;
            GoldenBtnStyle.fixedHeight = 24;

            // Remove default margins/padding that might interfere
            GoldenBtnStyle.margin = new RectOffset(4, 4, 4, 4);
            GoldenBtnStyle.padding = new RectOffset(6, 6, 4, 4);
        }

        public static Texture2D MakeTex(Color color)
        {
            var pix = new Color[1] { color };
            var t = new Texture2D(1, 1);
            t.SetPixels(pix);
            t.Apply();
            return t;
        }
    }
}
