using HarmonyLib;

namespace Coop.Mod.Patch
{
    /// <summary>
    ///     Patches to recognize remote player characters in the local game instance.
    /// </summary>
    public class PlayerCharacters
    {
        [HarmonyPatch(typeof(TaleWorlds.CampaignSystem.CharacterObject))]
        [HarmonyPatch(MethodType.Getter)]
        [HarmonyPatch(nameof(TaleWorlds.CampaignSystem.CharacterObject.IsPlayerCharacter))]
        class Patch
        {
            static bool Prefix(TaleWorlds.CampaignSystem.CharacterObject __instance, ref bool __result)
            {
                if (!Coop.DoSync())
                {
                    return true;
                }
                
                __result = CoopClient.Instance.GameState.IsPlayerControlled(__instance);
                return false;
            }
        }
    }
}
