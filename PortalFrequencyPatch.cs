using CommonModNS;
using HarmonyLib;
using System.Reflection;
using System.Reflection.Emit;

namespace SpawnControlModNS
{
    public partial class SpawnControlMod : Mod
    {
        private void ApplyFrequencies()
        {
            SpecialEvents_Patch.SetPortalValues(SummonsFrequency);
            SpecialEvents_Patch.SetCartValues(CartFrequency);
        }

        public override object Call(params object[] args)
        {
            if (args.Length > 1 && args[0].ToString() == "SetSadnessDivisor")
            {
                if (args[1] is int && (int)args[1] > 0)
                    return SpecialEvents_Patch.SetSadnessValues((int)args[1]);
            }
            return null;
        }
    }

    [HarmonyPatch]
    public class SpecialEvents_Patch
    {
        public static void SetPortalValues(FrequencyStates state)
        {
            RarePortalDivisor = PortalDivisor = divisors[(int)state];
            PortalMinMonth = PortalDivisor * 2;
            PirateDivisor = pirate_divisors[(int)state];
            I.Log($"Portal Divisor '{PortalDivisor}', Rare Divisor '{RarePortalDivisor}', Pirate Divisor '{PirateDivisor}'");
        }

        public static void SetCartValues(FrequencyStates state)
        {
            FrequencyOfTravellingCart = cart_thresholds[(int)state];
            MoonIs19 = state == FrequencyStates.NEVER ? -1 : 19;
            I.Log($"Travelling Cart Frequency '{FrequencyOfTravellingCart}', MoonIs19 '{MoonIs19}'");
        }

        public static int SetSadnessValues(int value)
        {
            if (value <= 0) value = 4;
            SadEventMinMonth = SadEventDivisor = value;
            I.Log($"Sadness Divisor '{SadEventDivisor}'");
            return value;
        }

        private static readonly int[] divisors = [1000000, 6, 4, 3, 1];
        private static readonly int[] pirate_divisors = [1000000, 12, 7, 3, 1];
        private static readonly float[] cart_thresholds = [-1f, 0.04f, 0.1f, 0.25f, 1f];

        private static int PortalMinMonth = 8;
        private static int PortalDivisor = 4;
        private static int RarePortalDivisor = 4;
        private static int PirateDivisor = 4;
        private static float FrequencyOfTravellingCart = 0.1f;
        private static int MoonIs19 = 19;
        private static int SadEventMinMonth = 4; // for use by Curse Worlds Mod
        private static int SadEventDivisor = 4;

        public static MethodBase TargetMethod()
        {
            Type InnerClass4EOMSpecialEvents = AccessTools.FirstInner(typeof(EndOfMonthCutscenes), t => t.Name.Contains("SpecialEvents"));
            //SpawnControlMod.Log(InnerClass4EOMSpecialEvents?.ToString() ?? "null");
            MethodBase method = AccessTools.FirstMethod(InnerClass4EOMSpecialEvents, method => method.Name.Contains("MoveNext"));
            //SpawnControlMod.Log(method?.ToString() ?? "null");
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
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "PortalMinMonth"))
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
                    //        bool spawnSadEvent = WorldManager.instance.CurrentBoard.Id == "happiness" && CurrentMonth > SadEventMinMonth && CurrentMonth % SadEventDivisor == 0;
                    .MatchStartForward(
                        new CodeMatch(OpCodes.Ldc_I4_4)
                    )
                    .ThrowIfNotMatch("Can't find happiness min month")
                    .Set(OpCodes.Ldsfld, AccessTools.Field(myClass, "SadEventMinMonth"))
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
