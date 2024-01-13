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
        public static bool AllowRarePortals => instance?.configRarePortals.Value ?? false;
        public static bool AllowAnimalsToRoam => instance?.configAnimalRoam.Value ?? true;

        private ConfigToggledEnum<FrequencyStates> configDanger;
        private ConfigToggledEnum<FrequencyStates> configCart;
        private ConfigEntryBool configRarePortals;
        private ConfigEntryBool configAnimalRoam;

        private RunoptsEnum<FrequencyStates> runoptDanger;

        private SaveHelper saveHelper;
        private SaveSettingsMode SaveMode;

        private ConfigEntryBool configNotifications;

        private void Awake()
        {
            instance = this;
            SavePatches();
            SetupConfig();
#if false
            SetupRunopts();
            GameOverScreen_Patch.AddListener( () =>
            {
                if (SaveMode == SaveSettingsMode.Tournament)
                {
                    return I.Xlat("");
                }
                return "";
            });
#endif
            Harmony.PatchAll(); // patches are in Patches.cs
        }

        private void SetupRunopts()
        {
            runoptDanger = new RunoptsEnum<FrequencyStates>("spawncontrolmod_freq_danger", FrequencyStates.NORMAL)
            {
                NameTerm = "spawncontrolmod_freq_danger",
                TooltipTerm = "spawncontrolmod_freq_danger_tooltip",
                EnumTermPrefix = "spawncontrolmod_freq_",
                FontColor = Color.blue,
                FontSize = 20,
                Value = configDanger.Value
            };
            HookRunOptions.ApplyPatch(Harmony);
        }

        private void SetupConfig()
        {
            configSpawnSites = new ConfigSpawnSites("spawncontrolmod_spawning", Config, SpawnSites.Anywhere);

            configDanger = NewToggle("spawncontrolmod_freq_danger");
            configCart = NewToggle("spawncontrolmod_freq_cart");

            configRarePortals = new ConfigEntryBool("spawncontrolmod_rareportals", Config, true, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_rare",
                TooltipTerm = "spawncontrolmod_rare_tooltip"
            } ) {
                currentValueColor = Color.blue,
                FontSize = 25
            };

            configAnimalRoam = new ConfigEntryBool("spawncontrolmod_roaming", Config, true, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_roaming",
                TooltipTerm = "spawncontrolmod_roaming_tooltip"
            })
            {
                currentValueColor = Color.blue,
                FontSize = 25
            };

            configNotifications = new ConfigEntryBool("spawncontrolmod_notifications", Config, true, new ConfigUI()
            {
                NameTerm = "spawncontrolmod_notifications",
                TooltipTerm = "spawncontrolmod_notifications_tooltip"
            })
            {
                currentValueColor = Color.blue,
                FontSize = 25
            };


            _ = new ConfigResetDefaults(Config, () =>
            {
                configSpawnSites.SetDefaults();
                configDanger.SetDefaults();
                configCart.SetDefaults();
                configRarePortals.SetDefaults();
                configAnimalRoam.SetDefaults();
            } ) {
                FontSize = 25
            };

            Config.OnSave = () =>
            {
                SummonsFrequency = /* runoptDanger.Value =*/ configDanger.Value;
                CartFrequency = configCart.Value;
            };
        }

        private void ApplyConfig()
        {
            Config.OnSave.Invoke();
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
            Log($"Config {name} {toggle.Value}");
            return toggle;
        }

        public override void Ready()
        {
            ApplyConfig();
            Log("Ready!");
        }

        private void SavePatches()
        {
            saveHelper = new SaveHelper("SpawnControlMod")
            {
                onGetSettings = delegate ()
                {
                    return SummonsFrequency.ToString();
                }
            };
            //WorldManagerPatches.LoadSaveRound += WM_OnLoad;
            //WorldManagerPatches.GetSaveRound += WM_OnSave;
            //WorldManagerPatches.StartNewRound += WM_OnNewRound;
            WorldManagerPatches.Play += WM_OnPlay;
            WorldManagerPatches.ApplyPatches(Harmony);
            //saveHelper.Ready(Path);
        }

        private void WM_OnNewRound(WorldManager _)
        {
            if (SaveMode == SaveSettingsMode.Disabled)
            {
                SummonsFrequency = FrequencyStates.NORMAL;
            }
            else
            {
                SummonsFrequency = runoptDanger.Value;
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
                if (Enum.TryParse<FrequencyStates>(payload, out FrequencyStates summon))
                {
                    SummonsFrequency = summon;
                }
                else
                {
                    SaveMode = SaveSettingsMode.Tampered;
                    SummonsFrequency = configDanger.Value;
                }
            }
            else if (SaveMode == SaveSettingsMode.Disabled)
            {
                SummonsFrequency = FrequencyStates.NORMAL;
            }
            else
            {
                SummonsFrequency = configDanger.Value;
            }
        }

        private void WM_OnPlay(WorldManager _)
        {
            ApplyConfig();
            Notification();
        }

        private void Notification()
        {
            if (configNotifications.Value)// && SaveMode != SaveSettingsMode.Disabled)
            {
                string text = I.Xlat("spawncontrolmod_location_anchor") + "\n" + ConfigEntryHelper.ColorText(Color.blue, I.Xlat($"spawncontrolmod_location_{instance.configSpawnSites.Value}")) + ".";
                if (configDanger.Value != FrequencyStates.NORMAL) 
                { 
                    text += "\n" + I.Xlat("spawncontrolmod_notify_danger") + "\n" + ConfigEntryHelper.ColorText(Color.blue, I.Xlat($"spawncontrolmod_freq_{instance.configDanger.Value}"));
                }
                I.GS.AddNotification(I.Xlat("spawncontrolmod_notify"), text);
            }
        }
    }
}