using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HideAndSeek
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public class HideAndSeekPlugin : BaseUnityPlugin
    {
        public const string PluginGuid = "derrickorama.hideandseek";
        public const string PluginName = "Hide & Seek - Blindness";
        public const string PluginVersion = "1.0.0";

        internal static ManualLogSource Log;

        // Blindness core
        internal static ConfigEntry<int> BlindSeconds;
        internal static ConfigEntry<bool> KeepUIVisible;
        internal static ConfigEntry<float> BlindTintR;
        internal static ConfigEntry<float> BlindTintG;
        internal static ConfigEntry<float> BlindTintB;

        // Inputs / debug
        internal static ConfigEntry<KeyboardShortcut> TestHotkey;
        internal static ConfigEntry<KeyCode> RawKey;
        internal static ConfigEntry<bool> EnableOnGUIFallback;
        internal static ConfigEntry<KeyCode> OnGUIKey;
        internal static ConfigEntry<int> MouseButtonTest;
        internal static ConfigEntry<int> AutoTriggerAfterSeconds;
        internal static ConfigEntry<bool> ShowOverlay;

        // Visual effects overlay
        internal static ConfigEntry<bool> EffectsEnabled;
        internal static ConfigEntry<float> OverlayAlpha;
        internal static ConfigEntry<bool> VignetteEnabled;
        internal static ConfigEntry<float> VignetteStrength;
        internal static ConfigEntry<bool> VignettePulse;
        internal static ConfigEntry<float> VignettePulseSpeed;
        internal static ConfigEntry<bool> ScanlinesEnabled;
        internal static ConfigEntry<int> ScanlineSpacing;
        internal static ConfigEntry<float> ScanlineAlpha;
        internal static ConfigEntry<bool> BannerEnabled;
        internal static ConfigEntry<string> BannerText;
        internal static ConfigEntry<float> BannerSize;
        internal static ConfigEntry<bool> CountdownEnabled;

        // Seeker system master switches
        internal static ConfigEntry<bool> SeekerEnabled;
        internal static ConfigEntry<bool> GateEnabled;

        // Seeker system
        internal static ConfigEntry<bool> SeekerVerboseLogs;
        internal static ConfigEntry<bool> SeekerAutoSelectOnJoin;   // master auto-picks on join / round start
        internal static ConfigEntry<float> SeekerAutoDelaySeconds;   // small delay before auto-pick
        internal static ConfigEntry<KeyboardShortcut> SeekerForceHotkey; // debug hotkey to re-roll

        // Gate / round-start detection
        internal static ConfigEntry<float> GateCinematicDelaySeconds; // extra hold after level-log
        internal static ConfigEntry<int> GateMinPlayers;
        internal static ConfigEntry<string> GateSceneMustContain;   // e.g. "Level - "
        internal static ConfigEntry<string> GateSceneExclude;       // e.g. "Lobby,Menu"
        internal static ConfigEntry<string> GateRequireComponent;   // optional type name to exist (e.g. "PlayerController")
        internal static ConfigEntry<int> GateStableFrames;       // frames the condition must hold
        internal static ConfigEntry<float> GateExtraDelay;         // small delay after ready
        internal static ConfigEntry<string> GateRequireSceneEquals;  // usually "Main"
        internal static ConfigEntry<string> GateLevelPrefix;         // usually "Level - "
        internal static ConfigEntry<string> GateLevelExclude;        // e.g., "Lobby"

        private void Awake() {
            Log = Logger;

            // Core
            BlindSeconds = Config.Bind("Blindness", "DurationSeconds", 30, "How long the blackout lasts (seconds).");
            KeepUIVisible = Config.Bind("Blindness", "KeepUIVisible", true, "Keep UI visible during blackout (set UI layer index in code if needed).");
            BlindTintR = Config.Bind("Blindness", "BlindTintR", 0.18f, "Background grey R (0..1).");
            BlindTintG = Config.Bind("Blindness", "BlindTintG", 0.20f, "Background grey G (0..1).");
            BlindTintB = Config.Bind("Blindness", "BlindTintB", 0.22f, "Background grey B (0..1).");
            TestHotkey = Config.Bind("Input", "TestHotkey", new KeyboardShortcut(KeyCode.P, KeyCode.LeftControl), "Primary test hotkey.");
            RawKey = Config.Bind("Input", "RawKey", KeyCode.F9, "Raw Input fallback key.");
            EnableOnGUIFallback = Config.Bind("Input", "EnableOnGUIFallback", true, "Also listen via OnGUI.");
            OnGUIKey = Config.Bind("Input", "OnGUIKey", KeyCode.BackQuote, "OnGUI key (e.g., back-quote `).");
            MouseButtonTest = Config.Bind("Input", "MouseButtonTest", 2, "Mouse button to trigger (-1 disable, 0 L, 1 R, 2 M).");
            AutoTriggerAfterSeconds = Config.Bind("Debug", "AutoTriggerAfterSeconds", 0, "Auto trigger after N seconds (0 disables).");
            ShowOverlay = Config.Bind("Debug", "ShowOverlay", false, "Top-left IMGUI debug box.");

            // Effects
            EffectsEnabled = Config.Bind("Effects", "Enabled", true, "Enable overlay effects during blindness.");
            OverlayAlpha = Config.Bind("Effects", "OverlayAlpha", 0.55f, "Extra grey wash on screen (0..1).");
            VignetteEnabled = Config.Bind("Effects", "VignetteEnabled", true, "Darken screen edges.");
            VignetteStrength = Config.Bind("Effects", "VignetteStrength", 0.85f, "Edge darkening strength (0..1).");
            VignettePulse = Config.Bind("Effects", "VignettePulse", true, "Gently pulse the vignette.");
            VignettePulseSpeed = Config.Bind("Effects", "VignettePulseSpeed", 0.7f, "Pulse speed (Hz).");
            ScanlinesEnabled = Config.Bind("Effects", "ScanlinesEnabled", true, "Add horizontal scanlines.");
            ScanlineSpacing = Config.Bind("Effects", "ScanlineSpacing", 2, "Scanline spacing in pixels (>=1).");
            ScanlineAlpha = Config.Bind("Effects", "ScanlineAlpha", 0.10f, "Scanline opacity (0..1).");
            BannerEnabled = Config.Bind("Effects", "BannerEnabled", true, "Show a big banner text.");
            BannerText = Config.Bind("Effects", "BannerText", "YOU ARE A SEEKER", "Large text displayed when seeker is blinded.");
            BannerSize = Config.Bind("Effects", "BannerSize", 1.0f, "Banner size multiplier (0.5..2.0).");
            CountdownEnabled = Config.Bind("Effects", "CountdownEnabled", true, "Show seconds remaining below banner.");

            SeekerEnabled = Config.Bind("Seeker", "Enabled", true, "Create SeekerManager (random IT) system.");
            GateEnabled = Config.Bind("RoundGate", "Enabled", true, "Create RoundStartGate (delays seeker until gameplay).");

            SeekerAutoSelectOnJoin = Config.Bind("Seeker", "AutoSelectOnJoin", true, "Master automatically selects a seeker when the room/round begins.");
            SeekerAutoDelaySeconds = Config.Bind("Seeker", "AutoDelaySeconds", 1.0f, "Delay (seconds) before auto-select to let Photon settle.");
            SeekerForceHotkey = Config.Bind("Seeker", "ForceReselectHotkey", new KeyboardShortcut(KeyCode.R, KeyCode.LeftAlt), "Force re-roll the seeker (master only) for testing.");
            SeekerVerboseLogs = Config.Bind("Seeker", "VerboseLogs", false, "Extra logging for seeker bootstrap.");

            GateCinematicDelaySeconds = Config.Bind("RoundGate", "CinematicDelaySeconds", 7f, "Extra seconds to wait after the game logs 'Changed level to: Level - <name>' before choosing the seeker.");
            GateMinPlayers = Config.Bind("RoundGate", "MinPlayers", 2, "Minimum players before selecting a seeker.");
            GateSceneMustContain = Config.Bind("RoundGate", "SceneMustContain", "Level - ", "Substring that must be in gameplay scene name.");
            GateSceneExclude = Config.Bind("RoundGate", "SceneExclude", "Lobby,Menu", "Comma list of substrings that disqualify scenes.");
            GateRequireComponent = Config.Bind("RoundGate", "RequireComponent", "", "Optional: a MonoBehaviour type that must be present before picking (leave empty to ignore).");
            GateStableFrames = Config.Bind("RoundGate", "StableFrames", 15, "How many consecutive frames the condition must be true.");
            GateExtraDelay = Config.Bind("RoundGate", "ExtraDelay", 0.35f, "Extra seconds to wait after ready before picking.");

            GateRequireSceneEquals = Config.Bind("RoundGate", "RequireSceneEquals", "Main", "Unity scene name that must be active before gameplay (e.g., Main).");
            GateLevelPrefix = Config.Bind("RoundGate", "LevelNamePrefix", "Level - ", "Root GameObject name prefix that indicates a loaded map.");
            GateLevelExclude = Config.Bind("RoundGate", "LevelExcludeToken", "Lobby", "If the level name contains this token, treat it as non-gameplay (e.g., Lobby).");

            Log.LogInfo($"{PluginName} {PluginVersion} loaded (GUID={PluginGuid})");

            RuntimeDriver.Ensure();
            BlindController.Ensure();
            BlindEffectsOverlay.Ensure();
            AnnouncementOverlay.Ensure();

            // Patches
            new Harmony(PluginGuid).PatchAll();

            if (SeekerEnabled.Value)
                SeekerManager.Ensure();

            if (GateEnabled.Value)
                RoundStartGate.Ensure();
        }

        /// Call from gameplay when someone becomes IT.
        public static void TriggerBlindnessOnce() => RuntimeDriver.TriggerOnce();
    }
}
