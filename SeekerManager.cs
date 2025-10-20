using System;
using System.Linq;
using UnityEngine;
using Photon.Pun;
using ExitGames.Client.Photon;
using Hashtable = ExitGames.Client.Photon.Hashtable;

namespace HideAndSeek
{
    /// Photon-callback-free seeker manager. Polls state lightly; invoked by RoundStartGate.
    internal class SeekerManager : MonoBehaviour
    {
        private static SeekerManager _instance;

        private const string KEY_SEEKER = "HNS_Seeker";     // int ActorNumber
        private const string KEY_LAST = "HNS_LastSeeker"; // int ActorNumber

        public static bool IsLocalSeeker { get; private set; }
        public static int CurrentSeekerActor { get; private set; } = -1;

        private float _nextPoll;          // throttle property polling
        private int _lastSeenProp = -2; // cache last seeker value we saw in room props

        public static void Ensure() {
            if (_instance != null) return;
            var go = new GameObject("HideAndSeek_SeekerManager") { hideFlags = HideFlags.DontSave };
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<SeekerManager>();
            HideAndSeekPlugin.Log?.LogInfo("[Seeker] Manager created (polling/no-callbacks).");
        }

        // Called by RoundStartGate when gameplay is actually ready.
        public static void BeginRound() {
            if (_instance == null) return;
            _instance.DoBeginRound();
        }

        public static void ForceReselect() {
            if (_instance == null) return;
            _instance.DoForceReselect();
        }

        private void DoBeginRound() {
            // Only act if Photon is fully ready and we're in a room
            if (!(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)) return;

            // Master: pick if not set; others: resolve whatever was set
            int seeker = GetRoomInt(KEY_SEEKER, -1);
            if (PhotonNetwork.IsMasterClient) {
                if (seeker <= 0) {
                    ChooseAndSetSeeker(avoidLastIfPossible: true);
                }
                else {
                    ApplySeeker(seeker);
                }
            }
            else {
                ApplySeeker(seeker); // will no-op if invalid
            }

            // Start watching for future changes (e.g., late re-roll)
            _nextPoll = 0f; // poll next frame
        }

        private void DoForceReselect() {
            if (!(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)) return;
            if (!PhotonNetwork.IsMasterClient) {
                HideAndSeekPlugin.Log?.LogInfo("[Seeker] ForceReselect ignored (not master).");
                return;
            }
            ChooseAndSetSeeker(avoidLastIfPossible: true);
            _nextPoll = 0f;
        }

        private void Update() {
            // Very light poll (2x/second) to notice room prop changes without Photon callbacks
            if (Time.unscaledTime < _nextPoll) return;
            _nextPoll = Time.unscaledTime + 0.5f;

            if (!(PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)) return;

            int seeker = GetRoomInt(KEY_SEEKER, -1);
            if (seeker != _lastSeenProp) {
                _lastSeenProp = seeker;
                HideAndSeekPlugin.Log?.LogInfo($"[Seeker] Poll detected change -> {seeker}");
                ApplySeeker(seeker);
            }
        }

        // ---------------- Core logic ----------------

        private void ChooseAndSetSeeker(bool avoidLastIfPossible) {
            try {
                var players = PhotonNetwork.PlayerList?.Where(p => p != null).ToArray();
                if (players == null || players.Length == 0) return;

                int last = GetRoomInt(KEY_LAST, -1);
                var pool = (avoidLastIfPossible && players.Length > 1 && last > 0)
                    ? players.Where(p => p.ActorNumber != last).ToArray()
                    : players;

                if (pool.Length == 0) pool = players;

                var chosen = pool[UnityEngine.Random.Range(0, pool.Length)];

                var props = new Hashtable {
                    [KEY_SEEKER] = chosen.ActorNumber,
                    [KEY_LAST] = chosen.ActorNumber
                };

                PhotonNetwork.CurrentRoom?.SetCustomProperties(props);
                HideAndSeekPlugin.Log?.LogInfo($"[Seeker] Master chose actor {chosen.ActorNumber} ({chosen.NickName})");

                // Locally apply immediately on master too
                ApplySeeker(chosen.ActorNumber);
            }
            catch (Exception ex) {
                HideAndSeekPlugin.Log?.LogError($"[Seeker] ChooseAndSetSeeker exception: {ex}");
            }
        }

        private void ApplySeeker(int actorNumber) {
            CurrentSeekerActor = actorNumber;

            var local = PhotonNetwork.LocalPlayer;
            bool wasLocalSeeker = IsLocalSeeker;
            IsLocalSeeker = (local != null && actorNumber > 0 && local.ActorNumber == actorNumber);

            HideAndSeekPlugin.Log?.LogInfo($"[Seeker] ApplySeeker actor={actorNumber} Local={IsLocalSeeker}");

            if (actorNumber <= 0) return; // nothing to do yet

            // Friendly name (best-effort; may be null early)
            string seekerName = PhotonNetwork.CurrentRoom?.GetPlayer(actorNumber)?.NickName ?? $"Player {actorNumber}";

            if (IsLocalSeeker && !wasLocalSeeker) {
                // small defer so camera/UI settle → same effect as hotkey
                StartCoroutine(CoDeferBlindness(0.25f));
            }
            else if (!IsLocalSeeker) {
                AnnouncementOverlay.Show($"{seekerName} is the seeker! Go hide!", 10f);
            }
        }

        private int GetRoomInt(string key, int def) {
            try {
                var room = PhotonNetwork.CurrentRoom;
                if (room == null) return def;
                var props = room.CustomProperties;
                if (props == null || !props.ContainsKey(key)) return def;

                var v = props[key];
                if (v is int i) return i;
                if (v is long l) return (int)l;
                if (v is byte b) return b;
            }
            catch (Exception ex) {
                HideAndSeekPlugin.Log?.LogError($"[Seeker] GetRoomInt exception: {ex}");
            }
            return def;
        }

        private System.Collections.IEnumerator CoDeferBlindness(float delay) {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);

            HideAndSeekPlugin.TriggerBlindnessOnce(); // same public API your hotkey calls
        }
    }
}
