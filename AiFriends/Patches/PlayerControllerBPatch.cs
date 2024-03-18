using HarmonyLib;
using AiFriends.AIHelper;
using UnityEngine;
using System.Collections.Generic;
using GameNetcodeStuff;
using AiFriends.Helpers;

namespace AiFriends.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB))]
    class PlayerControllerBPatch
    {
        public static Dictionary<PlayerControllerB, AIHelper> aiHelperData = new Dictionary<PlayerControllerB, AIHelper>();

        public static void SetAIHelperData(PlayerControllerB player, AILevel.AiLevel aiLevel)
        {
            aiHelperData[player] = new AIHelper
            {
                aiLevel = aiLevel,
                isAIPlayer = true
            };
        }

        public static bool IsAIPlayer(PlayerControllerB player)
        {
            return aiHelperData.ContainsKey(player) && aiHelperData[player].isAIPlayer;
        }

        /* [HarmonyPostfix]
        [HarmonyPatch("Awake")]
        static void AwakePostfix(PlayerControllerB __instance)
        {
            if (IsAIPlayer(__instance))
            {
                __instance.playerUsername = "AI Helper";
                __instance.gameplayCamera.enabled = false;
                __instance.thisPlayerModelArms.enabled = false;
                __instance.visorCamera.enabled = false;
                __instance.playerScreen.enabled = false;
                __instance.activeAudioListener.enabled = false;
                __instance.gameObject.GetComponent<CharacterController>().enabled = false;

            }
        } */
    }

    public class AIHelper
    {
        public AILevel.AiLevel aiLevel;
        public bool isAIPlayer;
    }
}