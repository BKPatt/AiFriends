using HarmonyLib;
using AiFriends.Managers;
using UnityEngine;
using GameNetcodeStuff;
using System;

namespace AiFriends.Patches
{
    [HarmonyPatch(typeof(Terminal))]
    internal class TerminalPatcher
    {
        private static UpgradeBus upgradeBus;

        [HarmonyPostfix]
        [HarmonyPatch("ParsePlayerSentence")]
        private static void CustomParser(ref Terminal __instance, ref TerminalNode __result)
        {
            string text = __instance.screenText.text.Substring(__instance.screenText.text.Length - __instance.textAdded);
            string[] commandParts = text.Split(' ');

            if (commandParts[0] == "helper" && commandParts.Length == 2)
            {
                try
                {
                    string aiLevel = commandParts[1];

                    Debug.Log($"Attempting to hire a helper");
                    if (__instance.groupCredits < 100)
                    {
                        __result = CreateTerminalNode($"Not enough credits {__instance.groupCredits}/100", true);
                        return;
                    }

                    if (upgradeBus == null)
                    {
                        GameObject upgradeBusObject = GameObject.Find("UpgradeBus");
                        if (upgradeBusObject != null)
                        {
                            upgradeBus = upgradeBusObject.GetComponent<UpgradeBus>();
                        }
                    }
                    __result = UpgradeBus.Instance.ConstructNode();
                    UpgradeBus.Instance.HandleHelperRequest(aiLevel);
                }
                catch (Exception error)
                {
                    Debug.Log(error);
                }
            }
        }

        public static PlayerControllerB GetPlayerByName(string playerName)
        {
            Debug.Log($"GetPlayerByName");
            PlayerControllerB[] allPlayers = UnityEngine.Object.FindObjectsOfType<PlayerControllerB>();
            foreach (PlayerControllerB player in allPlayers)
            {
                if (player.playerUsername == playerName)
                {
                    return player;
                }
            }
            return null;
        }

        private static TerminalNode CreateTerminalNode(string displayText, bool clearPreviousText)
        {
            Debug.Log($"CreateTerminalNode");
            TerminalNode node = ScriptableObject.CreateInstance<TerminalNode>();
            node.displayText = displayText;
            node.clearPreviousText = clearPreviousText;
            return node;
        }
    }
}
