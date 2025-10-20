using UnityEngine;

namespace HideAndSeek
{
    /// IMGUI overlay during blindness: grey wash, animated vignette, scanlines, centered banner + countdown.
    internal class BlindEffectsOverlay : MonoBehaviour
    {
        private static BlindEffectsOverlay _instance;
        private Texture2D _tex1x1;

        public static void Ensure() {
            if (_instance != null) return;
            var go = new GameObject("HideAndSeek_BlindEffectsOverlay") { hideFlags = HideFlags.DontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<BlindEffectsOverlay>();
        }

        private void EnsureTex() {
            if (_tex1x1 != null) return;
            _tex1x1 = new Texture2D(1, 1, TextureFormat.RGBA32, false, true);
            _tex1x1.wrapMode = TextureWrapMode.Repeat;
            _tex1x1.filterMode = FilterMode.Point;
            _tex1x1.SetPixel(0, 0, Color.white);
            _tex1x1.Apply();
        }

        private void DrawRect(int x, int y, int w, int h, Color c) {
            if (w <= 0 || h <= 0) return;
            var old = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(new Rect(x, y, w, h), _tex1x1);
            GUI.color = old;
        }

        private void DrawLineH(int y, int x0, int x1, Color c, int thickness = 1) {
            DrawRect(x0, y, Mathf.Max(1, x1 - x0), Mathf.Max(1, thickness), c);
        }

        private void OnGUI() {
            if (!BlindController.IsActive) return;
            if (!HideAndSeekPlugin.EffectsEnabled.Value) return;

            EnsureTex();
            GUI.depth = int.MinValue + 10;

            int sw = Screen.width;
            int sh = Screen.height;
            if (sw <= 0 || sh <= 0) return;

            // 1) Semi-transparent grey wash
            float aWash = Mathf.Clamp01(HideAndSeekPlugin.OverlayAlpha.Value);
            if (aWash > 0.001f) {
                var wash = new Color(0.10f, 0.12f, 0.14f, aWash); // cool grey
                DrawRect(0, 0, sw, sh, wash);
            }

            // 2) Vignette (edge darkening) with optional pulse
            if (HideAndSeekPlugin.VignetteEnabled.Value) {
                float str = Mathf.Clamp01(HideAndSeekPlugin.VignetteStrength.Value);
                if (HideAndSeekPlugin.VignettePulse.Value) {
                    float t = Time.unscaledTime * Mathf.Max(0.05f, HideAndSeekPlugin.VignettePulseSpeed.Value) * Mathf.PI * 2f;
                    float pulse = (Mathf.Sin(t) * 0.5f + 0.5f) * 0.25f; // 0..0.25
                    str = Mathf.Clamp01(str * (0.85f + pulse));
                }

                int steps = 12;
                for (int i = 0; i < steps; i++) {
                    float frac = (i + 1) / (float)steps;
                    float thick = frac * Mathf.Min(sw, sh) * 0.08f * str;
                    float alpha = frac * frac * 0.6f * str;
                    var col = new Color(0f, 0f, 0f, alpha);
                    int tt = Mathf.CeilToInt(thick);

                    // top / bottom / left / right
                    DrawRect(0, 0, sw, tt, col);
                    DrawRect(0, sh - tt, sw, tt, col);
                    DrawRect(0, 0, tt, sh, col);
                    DrawRect(sw - tt, 0, tt, sh, col);
                }
            }

            // 3) Scanlines
            if (HideAndSeekPlugin.ScanlinesEnabled.Value) {
                int step = Mathf.Max(1, HideAndSeekPlugin.ScanlineSpacing.Value);
                float a = Mathf.Clamp01(HideAndSeekPlugin.ScanlineAlpha.Value);
                if (a > 0.001f) {
                    var c = new Color(0f, 0f, 0f, a);
                    for (int y = 0; y < sh; y += step)
                        DrawLineH(y, 0, sw, c, 1);
                }
            }

            // 4) Banner + countdown (CENTERED with strong multi-pass shadow)
            if (HideAndSeekPlugin.BannerEnabled.Value) {
                float mult = Mathf.Clamp(HideAndSeekPlugin.BannerSize.Value, 0.5f, 2f);
                int baseSize = Mathf.Clamp(Mathf.Min(sw, sh) / 14, 18, 72);
                int size = Mathf.RoundToInt(baseSize * mult);

                var style = new GUIStyle(GUI.skin.label) {
                    fontSize = size,
                    alignment = TextAnchor.MiddleCenter,
                    fontStyle = FontStyle.Bold,
                    normal = { textColor = new Color(1f, 0.2f, 0.2f, 0.95f) }
                };

                Color shadowColor = new Color(0f, 0f, 0f, 0.85f);
                Vector2[] shadowOffsets = new Vector2[]
                {
                    new Vector2(2, 2), new Vector2(-2, 2), new Vector2(2, -2),
                    new Vector2(-2, -2), new Vector2(0, 3), new Vector2(3, 0)
                };

                var bannerRect = new Rect(0, (sh - size) * 0.5f, sw, size * 1.2f);
                string text = HideAndSeekPlugin.BannerText.Value;

                foreach (var o in shadowOffsets) {
                    var r = new Rect(bannerRect.x + o.x, bannerRect.y + o.y, bannerRect.width, bannerRect.height);
                    var old = GUI.color; GUI.color = shadowColor; GUI.Label(r, text, style); GUI.color = old;
                }
                GUI.Label(bannerRect, text, style);

                if (HideAndSeekPlugin.CountdownEnabled.Value) {
                    var sub = new GUIStyle(style) {
                        fontSize = Mathf.Max(14, size / 3),
                        fontStyle = FontStyle.Normal,
                        normal = { textColor = new Color(1f, 1f, 1f, 0.95f) }
                    };

                    float remain = Mathf.Ceil(BlindController.SecondsRemaining);
                    var subRect = new Rect(0, bannerRect.yMax + 10, sw, size);

                    foreach (var o in shadowOffsets) {
                        var r = new Rect(subRect.x + o.x, subRect.y + o.y, subRect.width, subRect.height);
                        var old = GUI.color; GUI.color = shadowColor; GUI.Label(r, $"{remain:0}s", sub); GUI.color = old;
                    }
                    GUI.Label(subRect, $"{remain:0}s", sub);
                }
            }
        }
    }
}
