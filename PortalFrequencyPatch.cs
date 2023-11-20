using CommonModNS;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace SpawnControlModNS
{
    public partial class SpawnControlMod : Mod
    {
        void ApplyFrequencies()
        {
            int[] divisors = new int[] { 1000000, 6, 4, 3, 1 };
            int[] pirate_divisors = new int[] { 1000000, 12, 7, 3, 1 };
            float[] cart_thresholds = new float[] { -1f, 0.04f, 0.1f, 0.25f, 1f };

            SpecialEvents_Patch.PortalDivisor = divisors[(int)configDanger.Value];
            SpecialEvents_Patch.RarePortalDivisor = divisors[(int)configDanger.Value];
            SpecialEvents_Patch.PirateDivisor = pirate_divisors[(int)configDanger.Value];
            SpecialEvents_Patch.FrequencyOfTravellingCart = cart_thresholds[(int)configCart.Value];
            SpecialEvents_Patch.MoonIs19 = configCart.Value == FrequencyStates.NEVER ? -1 : 19;

            Log($"Portal Divisor '{SpecialEvents_Patch.PortalDivisor}', Rare Divisor '{SpecialEvents_Patch.RarePortalDivisor}'," 
              + $"Pirate Divisor '{SpecialEvents_Patch.PirateDivisor}', Travelling Cart '{SpecialEvents_Patch.FrequencyOfTravellingCart:0.00}', MoonIs19 '{SpecialEvents_Patch.MoonIs19}'");
        }

        public void SetSadnessDivisor(int value)
        {
            SpecialEvents_Patch.SadEventDivisor = Math.Clamp(value, 1, 8);
        }
    }

    [HarmonyPatch]
    public class SpecialEvents_Patch
    {
        public static int PortalMinMonth = 8;
        public static int PortalDivisor = 4;
        public static int RarePortalDivisor = 4;
        public static float FrequencyOfTravellingCart = 0.1f;
        public static int PirateDivisor = 4;
        public static int SadEventMinMonth = 4;
        public static int SadEventDivisor = 4;
        public static int MoonIs19 = 19;

        private static Type innerClass;
        public static MethodBase TargetMethod()
        {
            innerClass = AccessTools.FirstInner(typeof(EndOfMonthCutscenes), t => t.Name.Contains("SpecialEvents"));
            SpawnControlMod.Log(innerClass?.ToString() ?? "null");
            MethodBase method = AccessTools.FirstMethod(innerClass, method => method.Name.Contains("MoveNext"));
            SpawnControlMod.Log(method?.ToString() ?? "null");
            return method;
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            try
            {
                Type myClass = typeof(SpecialEvents_Patch);
                List<CodeInstruction> result = new CodeMatcher(instructions)
                    //        bool flag = CurrentMonth > 8 && CurrentMonth % 4 == 0;
                    //        bool flag = CurrentMonth > SpecialEvents_Patch.PortalMinMonth && CurrentMonth % SpecialEvents_Patch.PortalDivisor == 0;
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_8)
                    )
                    .ThrowIfNotMatch("Can't find portal min month")
//                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "PortalMinMonth"))
                    .Advance(1)
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_4)
                    )
                    .ThrowIfNotMatch("Can't find portal min month")
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "PortalDivisor"))
                    //        bool spawnTravellingCart = (Random.value <= 0.1f && CurrentMonth >= 8 && CurrentMonth % 2 == 1) || CurrentMonth == 19;
                    //        bool spawnTravellingCart = (Random.value <= SpecialEvents_Patch.FrequencyOfTravellingCart && CurrentMonth >= 8 && CurrentMonth % 2 == 1) || CurrentMonth == SpecialEvents_Patch.MoonIs19;
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_R4, 0.1f)
                    )
                    .ThrowIfNotMatch("Can't find portal divisor")
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "FrequencyOfTravellingCart"))
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_S, (sbyte)19) // don't ask me why. But, it needs (sbyte)
                    )
                    .ThrowIfNotMatch("Can't find travelling cart month = 19")
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "MoonIs19"))
                    //         bool spawnPirateBoat = WorldManager.instance.BoardMonths.IslandMonth % 7 == 0 && WorldManager.instance.CurrentBoard.BoardOptions.CanSpawnPirateBoat;
                    //         bool spawnPirateBoat = WorldManager.instance.BoardMonths.IslandMonth % SpecialEvents_Patch.PirateDivisor == 0 && WorldManager.instance.CurrentBoard.BoardOptions.CanSpawnPirateBoat;
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_7)
                    )
                    .ThrowIfNotMatch("Can't find travelling cart frequency")
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "PirateDivisor"))
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldstr, "happiness")
                    )
                    .ThrowIfNotMatch("Can't find happiness")
                    //        bool spawnSadEvent = WorldManager.instance.CurrentBoard.Id == "happiness" && CurrentMonth > 4 && CurrentMonth % 4 == 0;
                    //        bool spawnSadEvent = WorldManager.instance.CurrentBoard.Id == "happiness" && CurrentMonth > 4 && CurrentMonth % SadEventDivisor == 0;
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_4)
                    )
                    .ThrowIfNotMatch("Can't find happiness min month")
//                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "SadEventMinMonth"))
                    .Advance(1)
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_4)
                    )
                    .ThrowIfNotMatch("Can't find happiness min month")
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "SadEventDivisor"))
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_4)
                    )
                    .ThrowIfNotMatch("Can't find happiness min month")
                    //            if (WorldManager.instance.CurrentRunVariables.StrangePortalSpawns % 4 == 0)
                    //            if (WorldManager.instance.CurrentRunVariables.StrangePortalSpawns % SpecialEvents_Patch.RarePortalDivisor == 0)
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "RarePortalDivisor"))
                    .InstructionEnumeration()
                    .ToList();
//                result.ForEach(instruction => SpawnControlMod.Log($"{instruction}"));
                SpawnControlMod.Log($"Exiting Instructions in {instructions.Count()}, instructions out {result.Count()}");
                return result;
            }
            catch (Exception e)
            {
                SpawnControlMod.LogError("Failed to Transpile EndOfMonthCutscenes.SpecialEvents" + e.ToString());
                return instructions;
            }
        }
    }
}
