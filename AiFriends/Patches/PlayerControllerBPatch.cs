using HarmonyLib;
using System.Collections.Generic;
using GameNetcodeStuff;
using AiFriends.Helpers;
using UnityEngine.AI;
using UnityEngine;

namespace AiFriends.Patches
{
    [HarmonyPatch(typeof(PlayerControllerB), "Awake")]
    class PlayerControllerBPatch
    {
        public static Dictionary<PlayerControllerB, AIHelper.AIHelper> aiHelperData = new Dictionary<PlayerControllerB, AIHelper.AIHelper>();

        static void Postfix(PlayerControllerB __instance)
        {
            if (aiHelperData.ContainsKey(__instance) && aiHelperData[__instance].isAIPlayer)
            {
                __instance.gameObject.AddComponent<AIHelper.AIHelper>();
                __instance.gameObject.AddComponent<NavMeshAgent>();
                AIHelper.AIHelper aiHelper = __instance.gameObject.GetComponent<AIHelper.AIHelper>();
                aiHelper.aiLevel = aiHelperData[__instance].aiLevel;
                aiHelperData.Remove(__instance);

                // Set additional properties for the AI-controlled player
                __instance.isPlayerControlled = false;
                __instance.isTestingPlayer = false;
                __instance.isHostPlayerObject = false;
                __instance.gameplayCamera.enabled = false;
                __instance.visorCamera.enabled = false;
                __instance.thisPlayerModelArms.enabled = false;
                __instance.playerScreen.enabled = false;
                __instance.activeAudioListener.enabled = false;
                __instance.gameObject.GetComponent<CharacterController>().enabled = false;
            }
        }

        public static void SetAIHelperData(PlayerControllerB player, Helper.AiLevel aiLevel)
        {
            aiHelperData[player] = new AIHelper.AIHelper
            {
                aiLevel = aiLevel,
                isAIPlayer = true
            };
        }

        public static bool IsAIPlayer(PlayerControllerB player)
        {
            return aiHelperData.ContainsKey(player) && aiHelperData[player].isAIPlayer;
        }
    }
}
