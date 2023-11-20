using HarmonyLib;
using UnityEngine;
using CommonModNS;

namespace SpawnControlModNS
{
    public enum FrequencyStates { NEVER, SLOWER, NORMAL, QUICKER, ALWAYS }
    public enum RarePortals { NEVER, NORMAL, ALWAYS }

    [HarmonyPatch]
    public partial class SpawnControlMod : Mod
    {
        public static SpawnControlMod instance;
        public static void Log(string msg) => instance?.Logger.Log(msg);
        public static void LogError(string msg) => instance?.Logger.LogError(msg);

        public static bool AllowAnimalsToRoam => instance?.configAnimalRoam.Value ?? true;
        public static bool AllowEnemyDrags => instance?.configDraggableMobs.Value ?? false;

        public ConfigToggledEnum<FrequencyStates> configDanger;
        public ConfigToggledEnum<RarePortals> configRare;
        public ConfigToggledEnum<FrequencyStates> configCart;
        public ConfigEntryBool configAnimalRoam;
        public ConfigEntryBool configDraggableMobs;

        private void Awake()
        {
            instance = this;
            WorldManagerPatches.Play += WM_Play;
            WorldManagerPatches.ApplyPatches(Harmony);
            SetupConfig();
            Harmony.PatchAll();
        }

        private void SetupConfig()
        {
            configSpawnSites = new ConfigSpawnSites("spawncontrolmod_spawning", Config, SpawnSites.Anywhere);

            configDanger = NewToggle("spawncontrolmod_freq_danger");
            configRare = NewToggle("")
            configCart = NewToggle("spawncontrolmod_freq_cart");

            configAnimalRoam = new ConfigEntryBool("spawncontrolmod_roaming", Config, true, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_roaming"
            })
            {
                currentValueColor = Color.blue
            };

            configDraggableMobs = new ConfigEntryBool("spawncontrolmod_dragmobs", Config, false, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_dragmobs"
            }){
                currentValueColor = Color.blue
            };
            ConfigFreeText configResetDefaults = new("none", Config, "spawncontrolmod_reset_defaults", "spawncontrolmod_reset_defaults_tooltip");
            configResetDefaults.Clicked += delegate (ConfigEntryBase _, CustomButton _)
            {
                configSpawnSites.SetDefaults();
                configRare.SetDefaults();
                configCart.SetDefaults();
                configAnimalRoam.SetDefaults();
                configDraggableMobs.SetDefaults();
            };
            Config.OnSave = () =>
            {
                ApplyConfig();
            };
        }

        private void ApplyConfig()
        {
            ApplySpawnSites();
            ApplyFrequencies();
        }

        private ConfigToggledEnum<FrequencyStates> NewToggle(string name)
        {
            ConfigToggledEnum<FrequencyStates> toggle = new(name, Config, FrequencyStates.NORMAL, new ConfigUI()
            {
                NameTerm = name,
                TooltipTerm = name + "_tooltip"
            }){
                currentValueColor = Color.blue,
                onDisplayEnumText = (FrequencyStates state) =>
                {
                    string term = $"spawncontrolmod_freq_{state}";
                    if (name.Contains("cart") && state == FrequencyStates.ALWAYS)
                        term += "_cart";
                    return I.Xlat(term);
                }
            };
            return toggle;
        }

        public override void Ready()
        {
            ApplyConfig();
            Log("Ready!");
        }

        static void WM_Play(WorldManager _)
        {
            instance.ApplyConfig();
            I.GS.AddNotification(I.Xlat("spawncontrolmod_notify"),
                                 I.Xlat("spawncontrolmod_location_anchor") + ConfigEntryHelper.ColorText(Color.blue, I.Xlat($"spawncontrolmod_location_{instance.configSpawnSites.Value}")));
        }

    }

    [HarmonyPatch(typeof(Animal), "Move")]
    public class RangeFreeAnimals
    {
        static bool Prefix(Animal __instance)
        {
            //I.Log($"Animal Roam {SpawnControlMod.AllowAnimalsToRoam}");
            return SpawnControlMod.AllowAnimalsToRoam || __instance.Id == Cards.eel;
        }
    }

    [HarmonyPatch(typeof(Animal), "CanHaveCard")]
    public class AnimalCanHaveEnemy
    {
        static bool Prefix(Animal __instance, ref bool __result, CardData otherCard)
        {
            if (otherCard is Enemy)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(Mob), "CanBeDragged", MethodType.Getter)]
    public class MobsCanBeDragged
    {
        static void Postfix(Mob __instance, ref bool __result)
        {
            if (SpawnControlMod.AllowEnemyDrags && __instance is Enemy)
            {
                if (__instance.MyGameCard.BeingDragged && __instance.MyGameCard.InventoryVisible)
                    __instance.MyGameCard.OpenInventory(false);
                __result = true;
            }
        }
    }

#if false
    [HarmonyPatch(typeof(Crab),nameof(Crab.Die))]
    internal class MommaCrab_Patch
    {
        public static int MommaCrabFrequency = 3;
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            List<CodeInstruction> result = new CodeMatcher(instructions)
                .MatchStartForward(
                    new CodeMatch(OpCodes.Ldc_I4_3)
                )
                .Set(OpCodes.Ldsfld, AccessTools.Field(typeof(MommaCrab_Patch), "MommaCrabFrequency"))
                .InstructionEnumeration()
                .ToList();
            return result;
        }
    }
#endif
}