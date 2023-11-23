using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Text;

namespace SpawnControlModNS
{
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

    [HarmonyPatch(typeof(PirateBoat), "CanBeDragged", MethodType.Getter)]
    public class PirateBoatCanBeDragged
    {
        static void Postfix(PirateBoat __instance, ref bool __result)
        {
            if (SpawnControlMod.AllowEnemyDrags)
            {
                __result = true;
            }
        }
    }

    [HarmonyPatch(typeof(StrangePortal), "CanBeDragged", MethodType.Getter)]
    public class StrangePortalCanBeDragged
    {
        static void Postfix(StrangePortal __instance, ref bool __result)
        {
            if (SpawnControlMod.AllowEnemyDrags && !__instance.IsTakingPortal)
            {
                __result = true;
            }
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
