﻿using HarmonyLib;
using System;
using System.Collections;
using UnityEngine;

namespace ExampleModNS
{
    public class ExampleMod : Mod
    {
        public static ExampleMod instance;
        public static void Log(string msg) => instance.Logger.Log(msg);
        public static void LogError(string msg) => instance.Logger.LogError(msg);
        private void Awake()
        {
            instance = this;
            //Harmony.PatchAll();
        }

        public override void Ready()
        {
            Logger.Log("Ready!");
        }
    }
}