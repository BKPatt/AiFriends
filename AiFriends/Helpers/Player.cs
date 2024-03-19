using GameNetcodeStuff;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

namespace AiFriends.Helpers
{
    public static partial class Helper
    {
        public static class Player
        {
            public static PlayerControllerB FindPlayerControllerBById(ulong netId)
            {
                foreach (var controller in Object.FindObjectsOfType<PlayerControllerB>())
                {
                    if (controller.GetComponent<NetworkObject>().NetworkObjectId == netId)
                    {
                        return controller;
                    }
                }
                return null;
            }

            public static void DropLootInShip(PlayerControllerB player)
            {
                player.DiscardHeldObject();
                SayVoiceLine("Loot transferred to ship");
            }

            public static void SayVoiceLine(string message)
            {
                // Implement logic to play voice lines based on the AI's actions
                Debug.Log($"AI Companion: {message}");
            }

            public static bool IsPlayerOutside(PlayerControllerB localPlayerController)
            {
                return localPlayerController != null && !localPlayerController.isInsideFactory;
            }
        }
    }
}
