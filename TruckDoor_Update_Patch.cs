using HarmonyLib;
using HideAndSeek;

[HarmonyPatch(typeof(TruckDoor), "Update")]
static class TruckDoor_Update_Patch
{
    // Private field accessors (names must match ILSpy exactly)
    private static readonly AccessTools.FieldRef<TruckDoor, bool> f_fullyOpen =
        AccessTools.FieldRefAccess<TruckDoor, bool>("fullyOpen");

    // 1) Before Update runs, capture the previous value
    static void Prefix(TruckDoor __instance, out bool __state) {
        __state = f_fullyOpen(__instance);
    }

    // 2) After Update runs, compare the new value
    static void Postfix(TruckDoor __instance, bool __state) {
        // fired only on the frame fullyOpen changes from false -> true
        if (!__state && f_fullyOpen(__instance)) {
            HideAndSeekPlugin.Log?.LogInfo("[REPO] TruckDoor fully opened.");
            
        }
    }
}