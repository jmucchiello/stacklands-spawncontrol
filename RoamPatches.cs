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
}