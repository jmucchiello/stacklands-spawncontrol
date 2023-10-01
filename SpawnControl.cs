using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace SpawnControlModNS
{
    public enum FrequencyStates { NEVER, SLOWER, NORMAL, QUICKER, ALWAYS }

    public partial class SpawnControlMod : Mod
    {
        public static SpawnControlMod instance;
        public static void Log(string msg) => instance?.Logger.Log(msg);
        public static void LogError(string msg) => instance?.Logger.LogError(msg);

        public ConfigToggledEnum<FrequencyStates> configPortals;
        public ConfigToggledEnum<FrequencyStates> configRarePortals;
        public ConfigToggledEnum<FrequencyStates> configPirates;
        public ConfigToggledEnum<FrequencyStates> configCart;

        private void Awake()
        {
            instance = this;
            configSpawnSites = new ConfigSpawnSites("spawncontrolmod_spawning", Config, SpawnSites.Anywhere);

            configPortals = NewToggle("spawncontrolmod_freq_portal");
            configRarePortals = NewToggle("spawncontrolmod_freq_rare");
            configPirates = NewToggle("spawncontrolmod_freq_pirates");
            configCart = NewToggle("spawncontrolmod_freq_cart");

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
                ApplySpawnSites();
                ApplyFrequencies();
            };
            Harmony.PatchAll();
        }

        private ConfigToggledEnum<FrequencyStates> NewToggle(string name)
        {
            ConfigToggledEnum<FrequencyStates> toggle = new ConfigToggledEnum<FrequencyStates>(name, Config, FrequencyStates.NORMAL);
            toggle.currentValueColor = Color.blue;
            toggle.onDisplayText = () =>
            {
                return I.Xlat(name);
            };
            if (name.Contains("cart"))
            {
                toggle.onDisplayEnumText = (FrequencyStates state) =>
                {
                    string term = $"spawncontrolmod_freq_{state}";
                    if (state == FrequencyStates.ALWAYS)
                        term += "_cart";
                    return I.Xlat(term);
                };
            }
            else
            {
                toggle.onDisplayEnumText = (FrequencyStates state) =>
                {
                    return I.Xlat($"spawncontrolmod_freq_{state}");
                };
            }
            toggle.onDisplayTooltip = () =>
            {
                return I.Xlat($"{name}_tooltip");
            };
            return toggle;
        }

        public override void Ready()
        {
            instance = this;
            ApplySpawnSites();
            ApplyFrequencies();
            Log("Ready!");
        }

        [HarmonyPatch(typeof(WorldManager), nameof(WorldManager.LoadSaveRound))]
        [HarmonyPostfix]
        static void WorldManager_LoadSaveRound(WorldManager __instance, SaveRound saveRound)
        {
            instance.ApplySpawnSites();
            instance.ApplyFrequencies();
        }
    }
}