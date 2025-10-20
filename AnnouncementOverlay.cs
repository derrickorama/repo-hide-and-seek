using UnityEngine;

namespace HideAndSeek
{
    /// Simple IMGUI toast/announcement overlay.
    internal class AnnouncementOverlay : MonoBehaviour
    {
        private static AnnouncementOverlay _instance;

        private static string _text;
        private static float _until;
        private static float _fadeOutStart; // time when fading begins
        private static float _fadeOutDuration = 0.75f; // seconds of fade

        private Texture2D _tex1x1;

        public static void Ensure() {
            if (_instance != null) return;
            var go = new GameObject("HideAndSeek_AnnouncementOverlay") { hideFlags = HideFlags.DontSave };
            Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<AnnouncementOverlay>();
        }

        public static void Show(string text, float seconds = 10f) {
            Ensure();
            _text = text;
            float dur = Mathf.Max(0.5f, seconds);
            _until = Time.unscaledTime + dur;
            _fadeOutStart = _until - _fadeOutDuration;
        }

        private void EnsureTex() {
            if (_tex1x1 != null) return;
            _tex1x1 = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            _tex1x1.SetPixel(0, 0, Color.white);
            _tex1x1.Apply();
        }

        private void OnGUI() {
            if (string.IsNullOrEmpty(_text)) return;
            float now = Time.unscaledTime;
            if (now > _until) { _text = null; return; }

            EnsureTex();
            GUI.depth = int.MinValue + 5;

            int sw = Screen.width;
            int sh = Screen.height;
            if (sw <= 0 || sh <= 0) return;

            // Alpha with soft fade-out
            float alpha = 1f;
            if (now > _fadeOutStart) {
                float t = Mathf.InverseLerp(_fadeOutStart, _until, now);
                alpha = Mathf.Clamp01(1f - t);
            }

            // Style
            int baseSize = Mathf.Clamp(Mathf.Min(sw, sh) / 18, 18, 56);
            var style = new GUIStyle(GUI.skin.label) {
                alignment = TextAnchor.UpperCenter,
                fontSize = baseSize,
                fontStyle = FontStyle.Bold
            };

            // Colors
            Color baseColor = new Color(1f, 1f, 1f, 0.97f * alpha);
            Color shadowColor = new Color(0f, 0f, 0f, 0.85f * alpha);
            Color panelColor = new Color(0f, 0f, 0f, 0.35f * alpha);

            // Layout near the top center (keep eyes free to look ahead)
            float topY = sh * 0.08f;
            var rect = new Rect(sw * 0.1f, topY, sw * 0.8f, baseSize * 1.6f);

            // Subtle panel behind text (rounded feel via padding)
            DrawRect(rect.x - 10, rect.y - 6, rect.width + 20, rect.height + 12, panelColor);

            // Strong multi-pass shadow
            var shadowStyle = new GUIStyle(style);
            shadowStyle.normal.textColor = shadowColor;

            Vector2[] offsets = new Vector2[]
            {
                new(2,2), new(-2,2), new(2,-2), new(-2,-2), new(0,3), new(3,0)
            };

            foreach (var o in offsets) {
                var r = new Rect(rect.x + o.x, rect.y + o.y, rect.width, rect.height);
                GUI.Label(r, _text, shadowStyle);
            }

            // Main text
            style.normal.textColor = baseColor;
            GUI.Label(rect, _text, style);
        }

        private void DrawRect(float x, float y, float w, float h, Color c) {
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), _tex1x1);
            GUI.color = old;
        }
    }
}
