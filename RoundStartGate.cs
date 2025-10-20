using System;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

namespace HideAndSeek
{
    /// RoundStartGate: waits for (1) Scene == "Main", (2) game log "Changed level to: Level - <name>",
    /// (3) Photon in-room with MinPlayers, then fires SeekerManager.BeginRound() once.
    internal class RoundStartGate : MonoBehaviour
    {
        private static RoundStartGate _instance;

        // live config accessors
        private int MinPlayers => Mathf.Max(1, HideAndSeekPlugin.GateMinPlayers?.Value ?? 2);
        private int NeedStable => Mathf.Max(1, HideAndSeekPlugin.GateStableFrames?.Value ?? 15);
        private float ExtraDelay => Mathf.Max(0f, HideAndSeekPlugin.GateExtraDelay?.Value ?? 0.35f);
        private string RequiredScene => HideAndSeekPlugin.GateRequireSceneEquals?.Value ?? "Main";
        private string ExcludeToken => HideAndSeekPlugin.GateLevelExclude?.Value ?? "Lobby";

        // state
        private string _lastScene = "";
        private bool _firedForThisMap = false;
        private int _stableFrames = 0;
        private float _readySince = -1f;

        private string _detectedLevelName = null;   // from log
        private float _detectedAt = -1f;

        public static void Ensure() {
            if (_instance != null) return;
            var go = new GameObject("HideAndSeek_RoundStartGate") { hideFlags = HideFlags.DontSave };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<RoundStartGate>();
            HideAndSeekPlugin.Log?.LogInfo("[Gate] RoundStartGate (log-sniff) created.");
        }

        private void OnEnable() {
            Application.logMessageReceived += OnLog;
        }

        private void OnDisable() {
            Application.logMessageReceived -= OnLog;
        }

        private void OnLog(string condition, string stackTrace, LogType type) {
            // Look for the game's line: "Changed level to: Level - <Name>"
            // Keep it light: simple startswith/contains checks
            if (string.IsNullOrEmpty(condition)) return;

            // Fast path
            if (condition.IndexOf("Changed level to:", StringComparison.OrdinalIgnoreCase) >= 0 &&
                condition.IndexOf("Level - ", StringComparison.OrdinalIgnoreCase) >= 0) {
                // extract after "Level - "
                int idx = condition.IndexOf("Level - ", StringComparison.OrdinalIgnoreCase);
                if (idx >= 0) {
                    string name = condition.Substring(idx + "Level - ".Length).Trim();
                    // sanitize trailing noise (rare)
                    int cut = name.IndexOfAny(new[] { '\r', '\n' });
                    if (cut >= 0) name = name.Substring(0, cut).Trim();

                    _detectedLevelName = name;
                    _detectedAt = Time.unscaledTime;
                    _firedForThisMap = false; // new map → allow firing again
                    _stableFrames = 0;
                    _readySince = -1f;

                    HideAndSeekPlugin.Log?.LogInfo($"[Gate] Detected level from log: {name}");
                }
            }
        }

        private void Update() {
            if (!HideAndSeekPlugin.GateEnabled.Value) return;

            // Scene tracking
            string scene = SceneManager.GetActiveScene().name ?? "";
            if (scene != _lastScene) {
                HideAndSeekPlugin.Log?.LogInfo($"[Gate] Scene: {scene}");
                _lastScene = scene;
                // entering a fresh scene → clear stability (keep detected level if it just loaded)
                _stableFrames = 0;
                _readySince = -1f;
                _firedForThisMap = false;
            }

            if (_firedForThisMap) return;

            // Readiness checks:
            // 1) Must be in required Unity scene (e.g., "Main")
            if (!string.Equals(scene, RequiredScene, StringComparison.OrdinalIgnoreCase)) {
                _stableFrames = 0; _readySince = -1f;
                return;
            }

            // 2) We must have seen a level from the game log, and it must not look like a lobby
            if (string.IsNullOrEmpty(_detectedLevelName)) {
                _stableFrames = 0; _readySince = -1f;
                return;
            }
            if (!string.IsNullOrEmpty(ExcludeToken) &&
                _detectedLevelName.IndexOf(ExcludeToken, StringComparison.OrdinalIgnoreCase) >= 0) {
                _stableFrames = 0; _readySince = -1f;
                return;
            }

            // 3) Photon in-room with enough players
            if (!(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)) {
                _stableFrames = 0; _readySince = -1f;
                return;
            }
            int players = PhotonNetwork.PlayerList?.Length ?? 0;
            if (players < MinPlayers) {
                _stableFrames = 0; _readySince = -1f;
                return;
            }

            // 2.5) Also wait a fixed cinematic hold after we detected the level from the log
            float needHold = Mathf.Max(0f, HideAndSeekPlugin.GateCinematicDelaySeconds?.Value ?? 2.5f);
            if (_detectedAt > 0f && (Time.unscaledTime - _detectedAt) < needHold) {
                _stableFrames = 0; _readySince = -1f;
                return;
            }

            // Passed all gates → require stability + small delay
            _stableFrames++;
            if (_readySince < 0f) _readySince = Time.unscaledTime;

            if (_stableFrames >= NeedStable && (Time.unscaledTime - _readySince) >= ExtraDelay) {
                _firedForThisMap = true;
                HideAndSeekPlugin.Log?.LogInfo($"[Gate] Ready (scene=Main, level={_detectedLevelName}, players={players}) → SeekerManager.BeginRound()");
                SeekerManager.BeginRound();
            }
        }
    }
}
