using System.Collections.Generic;
using UnityEngine;

namespace HideAndSeek
{
    internal class BlindController : MonoBehaviour
    {
        private static BlindController _instance;

        private float _until;
        private bool _active;
        private bool _keepUI;

        private readonly Dictionary<Camera, (int mask, CameraClearFlags flags, Color bg)> _backup
            = new Dictionary<Camera, (int, CameraClearFlags, Color)>();

        public static bool IsActive => _instance != null && _instance._active;
        public static float SecondsRemaining => IsActive ? Mathf.Max(0f, _instance._until - Time.unscaledTime) : 0f;

        public static void Ensure() {
            if (_instance != null) return;
            var go = new GameObject("HideAndSeek_BlindController") { hideFlags = HideFlags.DontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<BlindController>();
        }

        public static void EnableBlackoutFor(int seconds, bool keepUI) {
            Ensure();
            _instance.DoEnable(seconds, keepUI);
        }

        private void Update() {
            if (_active && Time.unscaledTime >= _until)
                Disable();
        }

        private void DoEnable(int seconds, bool keepUI) {
            _keepUI = keepUI;
            _until = Time.unscaledTime + Mathf.Max(1, seconds);
            _active = true;

            HideAndSeekPlugin.Log?.LogInfo($"Blindness enabled for {seconds}s (keepUI={_keepUI})");

            _backup.Clear();

            // GREY clear color from config
            var clearCol = new Color(
                Mathf.Clamp01(HideAndSeekPlugin.BlindTintR.Value),
                Mathf.Clamp01(HideAndSeekPlugin.BlindTintG.Value),
                Mathf.Clamp01(HideAndSeekPlugin.BlindTintB.Value),
                1f);

            foreach (var cam in Camera.allCameras) {
                if (!cam || !cam.enabled) continue;

                _backup[cam] = (cam.cullingMask, cam.clearFlags, cam.backgroundColor);

                int newMask = 0;
                // If the game's UI is on a known layer (commonly 5), keep it:
                if (_keepUI) newMask = (1 << 5); // change 5 if your UI uses a different layer

                cam.cullingMask = newMask;
                cam.clearFlags = CameraClearFlags.SolidColor;
                cam.backgroundColor = clearCol;
            }
        }

        private void Disable() {
            foreach (var kv in _backup) {
                var cam = kv.Key;
                if (!cam) continue;

                var (mask, flags, bg) = kv.Value;
                cam.cullingMask = mask;
                cam.clearFlags = flags;
                cam.backgroundColor = bg;
            }

            _backup.Clear();
            _active = false;

            HideAndSeekPlugin.Log?.LogInfo("Blindness disabled");
        }
    }
}
