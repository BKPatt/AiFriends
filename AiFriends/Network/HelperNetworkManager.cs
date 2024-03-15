using HarmonyLib;
using AiFriends.Managers;
using AiFriends.AIHelper;
using System;
using System.Collections.Generic;
using System.Text;
using Unity.Netcode;
using UnityEngine;

namespace AiFriends.Network
{
    [HarmonyPatch]
    class HelperNetworkManager
    {
        [HarmonyPostfix, HarmonyPatch(typeof(GameNetworkManager), "Start")]
        public static void Init()
        {
            Debug.Log("Init");
            if (networkPrefab != null)
                return;

            networkPrefab = LethalLib.Modules.NetworkPrefabs.CreateNetworkPrefab("ReviveStore");
            AICompanion reviveStore = networkPrefab.AddComponent<AICompanion>();
            UpgradeBus upgradeBus = networkPrefab.AddComponent<UpgradeBus>();
        }

        [HarmonyPostfix, HarmonyPatch(typeof(StartOfRound), "Awake")]
        static void SpawnNetworkHandler()
        {
            Debug.Log("SpawnNetworkHandler");
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer)
            {
                Debug.Log("IsHost or IsServer");
                var networkHandlerHost = UnityEngine.Object.Instantiate(networkPrefab, Vector3.zero, Quaternion.identity);
                networkHandlerHost.GetComponent<NetworkObject>().Spawn();
            }
        }

        static GameObject networkPrefab;
    }
}
