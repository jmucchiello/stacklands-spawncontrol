using HarmonyLib;
using UnityEngine;
using CommonModNS;
using UnityEngine.Playables;

namespace SpawnControlModNS
{
    public enum FrequencyStates { NEVER, SLOWER, NORMAL, QUICKER, ALWAYS }

    [HarmonyPatch]
    public partial class SpawnControlMod : Mod
    {
        public static SpawnControlMod instance;
        public static void Log(string msg) => instance?.Logger.Log(msg);
        public static void LogError(string msg) => instance?.Logger.LogError(msg);

        // these can be overridden by the save file
        // so they are updated in Config.OnSave, WM_OnLoad, and in WM_OnNewRound
        public static FrequencyStates SummonsFrequency { get; private set; }
        public static FrequencyStates CartFrequency { get; private set; }

        // these can't be overriden by the save file
        public static bool AllowAnimalsToRoam => instance?.configAnimalRoam.Value ?? true;
        public static bool AllowEnemyDrags => instance?.configDraggableMobs.Value ?? false;

        private ConfigToggledEnum<FrequencyStates> configDanger;
        private ConfigToggledEnum<FrequencyStates> configCart;
        private ConfigEntryBool configAnimalRoam;
        private ConfigEntryBool configDraggableMobs;
        private ConfigTournament configTournament;

        private void Awake()
        {
            instance = this;
            SavePatches();
            SetupConfig();
            Harmony.PatchAll(); // patches are in Patches.cs
        }

        private void SetupConfig()
        {
            configSpawnSites = new ConfigSpawnSites("spawncontrolmod_spawning", Config, SpawnSites.Anywhere);

            configDanger = NewToggle("spawncontrolmod_freq_danger");
//            configRare = NewToggle("");
            configCart = NewToggle("spawncontrolmod_freq_cart");

            configAnimalRoam = new ConfigEntryBool("spawncontrolmod_roaming", Config, true, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_roaming"
            } ) {
                currentValueColor = Color.blue,
                TextSize = 25
            };

            configDraggableMobs = new ConfigEntryBool("spawncontrolmod_dragmobs", Config, false, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_dragmobs",
                TooltipTerm = "spawncontrolmod_dragmobs_tooltip"
            } ) {
                currentValueColor = Color.blue,
                TextSize = 25
            };

            configTournament = new ConfigTournament("spawncontrolmod_tournament", Config);

            ConfigResetDefaults crd = new ConfigResetDefaults(Config, () =>
            {
                configSpawnSites.SetDefaults();
                configDanger.SetDefaults();
                configCart.SetDefaults();
                configAnimalRoam.SetDefaults();
                configDraggableMobs.SetDefaults();
            });

            Config.OnSave = () =>
            {
                SummonsFrequency = configDanger.Value;
                CartFrequency = configCart.Value;
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
                onDisplayText = () => {
                    return ConfigEntryHelper.SizeText(25, I.Xlat(name));
                },
                onDisplayEnumText = (FrequencyStates state) => {
                    string term = $"spawncontrolmod_freq_{state}";
                    if (name.Contains("cart") && state == FrequencyStates.ALWAYS)
                        term += "_cart";
                    return ConfigEntryHelper.SizeText(25, I.Xlat(term));
                }
            };
            return toggle;
        }

        public override void Ready()
        {
            ApplyConfig();
            Log("Ready!");
        }

        private SaveHelper saveHelper;
        private SaveSettingsMode SaveMode;

        private void SavePatches()
        {
            saveHelper = new SaveHelper("SpawnControlMod")
            {
                onGetSettings = delegate ()
                {
                    return SummonsFrequency.ToString() + " " + CartFrequency.ToString();
                }
            };
            WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            WorldManagerPatches.GetSaveRound += WM_OnSave;
            WorldManagerPatches.StartNewRound += WM_OnNewRound;
            WorldManagerPatches.Play += WM_OnPlay;
            WorldManagerPatches.ApplyPatches(Harmony);
        }

        private void WM_OnNewRound(WorldManager _)
        {
            SaveMode = configTournament.Value;
            if (SaveMode == SaveSettingsMode.Disabled)
            {
                SummonsFrequency = FrequencyStates.NORMAL;
                CartFrequency = FrequencyStates.NORMAL;
            }
            else
            {
                SummonsFrequency = configDanger.Value;
                CartFrequency = configCart.Value;
            }
        }

        private void WM_OnSave(WorldManager _, SaveRound saveRound)
        {
            saveHelper.SaveData(saveRound, SaveMode);
        }

        private void WM_OnLoad(WorldManager _, SaveRound saveRound)
        {
            (SaveMode, string payload) = saveHelper.LoadData(saveRound);
            if (SaveMode == SaveSettingsMode.Tournament)
            {
                string[] values = payload?.Split(' ') ?? new string[0];
                if (values.Length == 2 &&
                    Enum.TryParse<FrequencyStates>(values[0], out FrequencyStates summon) &&
                    Enum.TryParse<FrequencyStates>(values[1], out FrequencyStates cart))
                {
                    SummonsFrequency = summon;
                    CartFrequency = cart;
                }
                else
                {
                    SaveMode = SaveSettingsMode.Tampered;
                    SummonsFrequency = configDanger.Value;
                    CartFrequency = configCart.Value;
                }
            }
            else if (SaveMode == SaveSettingsMode.Disabled)
            {
                SummonsFrequency = FrequencyStates.NORMAL;
                CartFrequency = FrequencyStates.NORMAL;
            }
            else
            {
                SummonsFrequency = configDanger.Value;
                CartFrequency = configCart.Value;
            }
        }

        private void WM_OnPlay(WorldManager _)
        {
            ApplyConfig();
            Notification();
        }

        private void Notification()
        {
            if (SaveMode != SaveSettingsMode.Disabled)
            {
                I.GS.AddNotification(I.Xlat("spawncontrolmod_notify"),
                                     I.Xlat("spawncontrolmod_location_anchor") + ConfigEntryHelper.ColorText(Color.blue, I.Xlat($"spawncontrolmod_location_{instance.configSpawnSites.Value}")));
            }
        }
    }
}