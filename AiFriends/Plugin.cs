using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using AiFriends.Managers;
using AiFriends.Misc;
using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using UnityEngine;
using Unity.Netcode;
using System.Security.Cryptography;
using System.Text;
using LethalLib.Modules;

namespace AiFriends
{
    [BepInPlugin(Metadata.GUID, Metadata.NAME, Metadata.VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private readonly Harmony harmony = new Harmony(Metadata.GUID);
        public static Plugin instance;
        public static ManualLogSource mls;
        public static PluginConfig cfg { get; private set; }

        void Awake()
        {
            cfg = new(base.Config);
            cfg.InitBindings();
            mls = Logger;
            instance = this;
            PatchAllMethods();
            NetcodePatch();
            Logger.LogInfo("Helper functionality has been initialized.");
        }

        private void PatchAllMethods()
        {
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private static void NetcodePatch()
        {
            var types = Assembly.GetExecutingAssembly().GetTypes();
            foreach (var type in types)
            {
                var methods = type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
                foreach (var method in methods)
                {
                    var attributes = method.GetCustomAttributes(typeof(RuntimeInitializeOnLoadMethodAttribute), false);
                    if (attributes.Length > 0)
                    {
                        method.Invoke(null, null);
                    }
                }
            }
        }

        private string GetJsonContent()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "AiFriends.Misc.InfoStrings.json";
            using (StreamReader reader = new StreamReader(assembly.GetManifestResourceStream(resourceName)))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
