using System;
using UnityEngine;

namespace HideAndSeek
{
    internal class RuntimeDriver : MonoBehaviour
    {
        private static RuntimeDriver _instance;
        private float _nextHeartbeat;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap() => Ensure();

        public static void Ensure() {
            if (_instance != null) return;
            var go = new GameObject("HideAndSeek_RuntimeDriver") { hideFlags = HideFlags.DontSave };
            UnityEngine.Object.DontDestroyOnLoad(go);
            _instance = go.AddComponent<RuntimeDriver>();
        }

        private void Start() {
            int auto = Mathf.Max(0, HideAndSeekPlugin.AutoTriggerAfterSeconds.Value);
            if (auto > 0) StartCoroutine(AutoTrigger(auto));
        }

        private System.Collections.IEnumerator AutoTrigger(int seconds) {
            float end = Time.unscaledTime + seconds;
            while (Time.unscaledTime < end) yield return null;
            TriggerOnce();
        }

        private void Update() {
            if (Time.unscaledTime >= _nextHeartbeat) {
                _nextHeartbeat = Time.unscaledTime + 10f;
                HideAndSeekPlugin.Log?.LogDebug("RuntimeDriver heartbeat…");
            }

            if (HideAndSeekPlugin.TestHotkey.Value.IsDown()) TriggerOnce();
            if (Input.GetKeyDown(HideAndSeekPlugin.RawKey.Value)) TriggerOnce();

            int mb = HideAndSeekPlugin.MouseButtonTest.Value;
            if (mb >= 0 && mb <= 2 && Input.GetMouseButtonDown(mb)) TriggerOnce();

            if (HideAndSeekPlugin.SeekerForceHotkey.Value.IsDown()) {
                SeekerManager.ForceReselect();
            }

        }

        private void OnGUI() {
            if (HideAndSeekPlugin.EnableOnGUIFallback.Value &&
                Event.current != null && Event.current.type == EventType.KeyDown &&
                Event.current.keyCode == HideAndSeekPlugin.OnGUIKey.Value) {
                TriggerOnce();
                Event.current.Use();
            }

            if (HideAndSeekPlugin.ShowOverlay.Value) {
                GUI.depth = int.MinValue;
                GUI.Box(new Rect(10, 10, 520, 54),
                    $"[H&S Blindness] Driver Alive  |  {DateTime.Now:T}\n" +
                    $"Hotkey={HideAndSeekPlugin.TestHotkey.Value} Raw={HideAndSeekPlugin.RawKey.Value} OnGUI={HideAndSeekPlugin.OnGUIKey.Value} Mouse={HideAndSeekPlugin.MouseButtonTest.Value}");
            }
        }

        public static void TriggerOnce() {
            int seconds = Mathf.Max(1, HideAndSeekPlugin.BlindSeconds.Value);
            bool keepUI = HideAndSeekPlugin.KeepUIVisible.Value;
            BlindController.EnableBlackoutFor(seconds, keepUI);
        }
    }
}
