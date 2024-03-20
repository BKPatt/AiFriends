using System;
using UnityEngine;
using GameNetcodeStuff;
using Unity.Netcode;
using AiFriends.Helpers;
using AiFriends.Patches;
using UnityEngine.AI;

namespace AiFriends.AIHelper
{
    public class AICompanion : NetworkBehaviour
    {
        public static AICompanion Instance { get; private set; }

        public Helper.AiLevel aiLevel = Helper.AiLevel.medium;
        private const int HelperCost = 100;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private bool CanAffordHelper()
        {
            Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            return terminal.groupCredits >= HelperCost;
        }

        private void DeductCredits()
        {
            Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            terminal.groupCredits -= HelperCost;
            SyncCreditsServerRpc(terminal.groupCredits);
        }

        [ServerRpc(RequireOwnership = false)]
        private void SyncCreditsServerRpc(int newCredits)
        {
            SyncCreditsClientRpc(newCredits);
        }

        [ClientRpc]
        private void SyncCreditsClientRpc(int newCredits)
        {
            Terminal terminal = GameObject.Find("TerminalScript").GetComponent<Terminal>();
            terminal.groupCredits = newCredits;
        }

        [ClientRpc]
        public void SpawnHelperClientRpc(Vector3 spawnPosition, string AILevel)
        {
            if (IsServer)
                return;

            Debug.Log("Spawning AI helper on client");

            GameObject helperObject = Instantiate(StartOfRound.Instance.playerPrefab, spawnPosition, Quaternion.identity);
            PlayerControllerB player = helperObject.GetComponent<PlayerControllerB>();

            Helper.AiLevel aiLevel = (Helper.AiLevel)Enum.Parse(typeof(Helper.AiLevel), AILevel);
            PlayerControllerBPatch.SetAIHelperData(player, aiLevel);

            player.playerUsername = "AI Helper";
            player.DisablePlayerModel(player.gameObject, enable: true);
            player.gameplayCamera.enabled = false;
            player.visorCamera.enabled = false;
            player.thisPlayerModelArms.enabled = false;
            player.playerScreen.enabled = false;
            player.activeAudioListener.enabled = false;
            player.gameObject.GetComponent<CharacterController>().enabled = false;
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestSpawnHelperServerRpc(string AILevel)
        {
            Debug.Log("Spawning AI helper on server");

            if (!CanAffordHelper())
                return;

            DeductCredits();

            Vector3 spawnPosition = Helper.PlayerSpawn.GetPlayerSpawnPosition(0);
            GameObject helperObject = Instantiate(StartOfRound.Instance.playerPrefab, spawnPosition, Quaternion.identity);
            helperObject.AddComponent<AIHelper>();
            helperObject.AddComponent<NavMeshAgent>();
            
            NavMeshAgent navMeshAgent = helperObject.GetComponent<NavMeshAgent>();
            if (navMeshAgent == null)
            {
                Debug.LogError("NavMeshAgent component is not found on the spawned AI Helper object.");
                return;
            }

            Helper.AiLevel aiLevel = (Helper.AiLevel)Enum.Parse(typeof(Helper.AiLevel), AILevel);
            AIHelper aiHelper = helperObject.GetComponent<AIHelper>();
            aiHelper.aiLevel = aiLevel;

            helperObject.transform.position = spawnPosition;
            helperObject.GetComponent<NetworkObject>().Spawn();
            SpawnHelperClientRpc(spawnPosition, AILevel);
        }
    }
}
