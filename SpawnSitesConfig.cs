using CommonModNS;
using HarmonyLib;
using UnityEngine;
using Rand = UnityEngine.Random;
using static CommonModNS.I;

namespace SpawnControlModNS
{
    public enum SpawnSites
    {
        Anywhere, Center, UpperLeft, UpperRight, LowerLeft, LowerRight
    };

    public partial class SpawnControlMod : Mod
    {
        public ConfigSpawnSites configSpawnSites;

        public void ApplySpawnSites()
        {
            switch (configSpawnSites.Value)
            {
                case SpawnSites.Anywhere:   SpawnPosition_Patch.SetExtents(0.1f, 0.9f, 0.1f, 0.9f); break; // these are the values used in the original code
                case SpawnSites.Center:     SpawnPosition_Patch.SetExtents(0.4f, 0.6f, 0.4f, 0.6f); break;
                case SpawnSites.LowerLeft:  SpawnPosition_Patch.SetExtents(0.1f, 0.3f, 0.1f, 0.3f); break;
                case SpawnSites.UpperRight: SpawnPosition_Patch.SetExtents(0.7f, 0.9f, 0.7f, 0.9f); break;
                case SpawnSites.LowerRight: SpawnPosition_Patch.SetExtents(0.7f, 0.9f, 0.1f, 0.3f); break;
                case SpawnSites.UpperLeft:  SpawnPosition_Patch.SetExtents(0.1f, 0.3f, 0.7f, 0.9f); break;
            }
        }

        public string YesNo(bool b) { return b ? "Yes" : "No"; }
    }

    public class ConfigSpawnSites : ConfigEntryEnum<SpawnSites>
    {
        public ConfigSpawnSites(string name, ConfigFile configFile, SpawnSites defaultValue, ConfigUI ui = null)
            : base(name, configFile, defaultValue, ui)
        {
            currentValueColor = Color.blue;
            onDisplayAnchorText = delegate ()
            {
                return I.Xlat("spawncontrolmod_location_anchor") + " " + ColorText(currentValueColor, I.Xlat($"spawncontrolmod_location_{(SpawnSites)Value}"));
            };
            onDisplayAnchorTooltip = delegate ()
            {
                return I.Xlat("spawncontrolmod_location_anchor_tooltip");
            };
            onDisplayEnumText = delegate (SpawnSites s)
            {
                return I.Xlat($"spawncontrolmod_location_{s}");
            };
            onDisplayEnumTooltip = delegate (SpawnSites s)
            {
                SokTerm term = SokLoc.instance.CurrentLocSet.GetTerm($"spawncontrolmod_location_tooltip_{s}");
                if (term == null) return null;
                return I.Xlat($"spawncontrolmod_location_tooltip_{s}");
            };
            popupMenuTitleText = "spawncontrolmod_location_menu_text";
            popupMenuHelpText = "spawncontrolmod_location_menu_tooltip";

            CloseButtonTextTerm = "spawncontrolmod_closemenu";
        }
    }

    [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.GetRandomSpawnPosition))]
    internal class SpawnPosition_Patch
    {
        public static void SetExtents(float lx, float hx, float lz, float hz)
        {
            lowX = lx;
            highX = hx;
            lowZ = lz;
            highZ = hz;
            Log($"Spawn Location Ranges: X({lowX:F1} to {highX:F1}), Y({lowZ:F1} to {highZ:F1})");
        }

        private static float lowX = 0f, highX = 1f, lowZ = 0f, highZ = 1f;

        static bool Prefix(WorldManager __instance, ref Vector3 __result)
        {
            Bounds worldBounds = __instance.CurrentBoard.WorldBounds;
//            SpawnControlMod.Log($"GetRandomSpawnPosition() Min/Max X {worldBounds.min.x}/{worldBounds.max.x} Min/Max Z {worldBounds.min.z}/{worldBounds.max.z}");
            float x = Mathf.Lerp(worldBounds.min.x, worldBounds.max.x, Rand.Range(lowX, highX));
            float z = Mathf.Lerp(worldBounds.min.z, worldBounds.max.z, Rand.Range(lowZ, highZ));
            __result = new Vector3(x, 0f, z);
//            SpawnControlMod.Log($"GetRandomSpawnPosition() {__result}");
            return false;
        }
    }

}
