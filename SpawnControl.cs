using HarmonyLib;
using UnityEngine;
using CommonModNS;

namespace SpawnControlModNS
{
    public enum FrequencyStates { NEVER, SLOWER, NORMAL, QUICKER, ALWAYS }


    [HarmonyPatch]
    public partial class SpawnControlMod : Mod
    {
        public static SpawnControlMod instance;
        public static void Log(string msg) => instance?.Logger.Log(msg);
        public static void LogError(string msg) => instance?.Logger.LogError(msg);

        public static bool AllowAnimalsToRoam => instance?.configAnimalRoam.Value ?? true;
        public static bool AllowEnemyDrags => instance?.configDraggableMobs.Value ?? false;

        public ConfigToggledEnum<FrequencyStates> configPortals;
        public ConfigToggledEnum<FrequencyStates> configRarePortals;
        public ConfigToggledEnum<FrequencyStates> configPirates;
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

            configPortals = NewToggle("spawncontrolmod_freq_portal");
            configRarePortals = NewToggle("spawncontrolmod_freq_rare");
            configPirates = NewToggle("spawncontrolmod_freq_pirates");
            configCart = NewToggle("spawncontrolmod_freq_cart");
            configPortals.onChange = delegate (FrequencyStates value)
            {
                configRarePortals.Enable(value != FrequencyStates.NEVER);
                return true;
            };
            configRarePortals.onDisplayText = () =>
            {
                string s = I.Xlat(configRarePortals.Name);
                if (configPortals.Value == FrequencyStates.NEVER)
                    s = "<s>" + s + "</s>";
                return s;
            };
            configRarePortals.onDisplayEnumText = (FrequencyStates state) =>
            {
                string s = I.Xlat($"spawncontrolmod_freq_{state}");
                if (configPortals.Value == FrequencyStates.NEVER)
                    s = "<s>" + s + "</s>";
                return s;
            };
            configRarePortals.onLoad = delegate ()
            {
                configRarePortals.Enable(configPortals.Value != FrequencyStates.NEVER);
                configRarePortals.Update();
            };

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
                configPortals.SetDefaults();
                configRarePortals.SetDefaults();
                configPirates.SetDefaults();
                configCart.SetDefaults();
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
            ConfigToggledEnum<FrequencyStates> toggle = new ConfigToggledEnum<FrequencyStates>(name, Config, FrequencyStates.NORMAL, new ConfigUI()
            {
                NameTerm = name,
                TooltipTerm = name + "_tooltip"
            });
            toggle.currentValueColor = Color.blue;
            toggle.onDisplayEnumText = (FrequencyStates state) =>
            {
                string term = $"spawncontrolmod_freq_{state}";
                if (name.Contains("cart") && state == FrequencyStates.ALWAYS)
                    term += "_cart";
                return I.Xlat(term);
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
}