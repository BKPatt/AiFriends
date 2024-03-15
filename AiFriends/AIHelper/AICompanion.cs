using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using GameNetcodeStuff;
using Unity.Netcode;
using AiFriends.Helpers;
using UnityEngine.AI;
using System.Linq;

namespace AiFriends.AIHelper
{
    public class AICompanion : PlayerControllerB
    {
        public static AICompanion Instance { get; private set; }

        public PlayerControllerB ownerPlayer;
        public float followDistance = 5f;
        public float lootGatherRadius = 10f;
        public int inventoryCapacity = 10;
        public AILevel.AiLevel aiLevel = AILevel.AiLevel.Medium;
        private const int HelperCost = 100;

        private bool isExploring = true;
        private bool isReturningToShip = false;
        private GrabbableObject[] nearbyLoot;
        private Vector3 shipPosition;

        private NavMeshAgent navMeshAgent;
        private bool isMoving;

        private void Start()
        {
            shipPosition = RoundManager.FindMainEntrancePosition();
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Update()
        {
            if (isPlayerDead)
                return;

            if (isExploring)
            {
                Explore();
            }
            else if (isReturningToShip)
            {
                ReturnToShip();
            }
            else
            {
                ExploreAndGatherLoot();
            }
        }

        private void SetDestinationToPosition(Vector3 targetPosition)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(targetPosition);
                isMoving = true;
            }
        }

        private void StopMoving()
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.ResetPath();
                isMoving = false;
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
        private void SpawnHelperClientRpc(PlayerControllerB player, String AILevel)
        {
            if (!CanAffordHelper())
            {
                Debug.Log("Not enough credits to spawn AI companion.");
                return;
            }

            DeductCredits();

            Vector3 spawnPosition = player.transform.position + player.transform.forward * 2f;
            Quaternion spawnRotation = player.transform.rotation;

            GameObject aiCompanionInstance = Instantiate(gameObject, spawnPosition, spawnRotation);
            AICompanion aiCompanion = aiCompanionInstance.GetComponent<AICompanion>();

            aiCompanion.ownerPlayer = player;
            aiCompanion.aiLevel = aiLevel;

            aiCompanionInstance.GetComponent<NetworkObject>().Spawn();
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestSpawnHelperServerRpc(String AILevel)
        {
            PlayerControllerB player = new();
            DeductCredits();
            SpawnHelperClientRpc(player, AILevel);
        }

        private void Explore()
        {
            if (ownerPlayer == null)
                return;

            Vector3 targetPosition = ownerPlayer.transform.position;
            float distance = Vector3.Distance(transform.position, targetPosition);

            if (distance > followDistance)
            {
                SetDestinationToPosition(targetPosition);
            }
            else
            {
                StopMoving();
            }
        }

        private void ExploreAndGatherLoot()
        {
            nearbyLoot = FindNearbyLoot();

            if (nearbyLoot.Length > 0)
            {
                GrabbableObject loot = nearbyLoot[0];
                SetDestinationToPosition(loot.transform.position);

                if (Vector3.Distance(transform.position, loot.transform.position) < 1f)
                {
                    PickupLoot(loot);
                }
            }
            else
            {
                // Implement exploration logic based on AI level
                switch (aiLevel)
                {
                    case AILevel.AiLevel.Easy:
                        // Simple exploration logic for easy AI
                        break;
                    case AILevel.AiLevel.Medium:
                        // More advanced exploration logic for medium AI
                        break;
                    case AILevel.AiLevel.Hard:
                        // Complex exploration logic for hard AI
                        break;
                }
            }

            // Implement logic to handle enemy encounters based on the AI level and situation
            // You can use the existing EnemyAI methods to detect and engage enemies
        }

        private void ReturnToShip()
        {
            // Implement logic to navigate back to the ship or main entrance to drop off the loot
            // You can use the existing navmesh or waypoint system to find the path

            if (Vector3.Distance(transform.position, RoundManager.FindMainEntrancePosition()) < 2f)
            {
                TransferLootToPlayer();
                isReturningToShip = false;
                isExploring = true;
            }
        }

        private GrabbableObject[] FindNearbyLoot()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, lootGatherRadius);
            return System.Array.FindAll(colliders, collider => collider.GetComponent<GrabbableObject>() != null)
                .Select(collider => collider.GetComponent<GrabbableObject>())
                .ToArray();
        }

        private void PickupLoot(GrabbableObject loot)
        {
            // Implement logic to add the loot to the AI companion's inventory

            if (FirstEmptyItemSlot() == -1)
            {
                SayVoiceLine("Inventory full, returning to ship");
                isReturningToShip = true;
            }
        }

        public void TransferLootToPlayer()
        {
            DiscardHeldObject();
            SayVoiceLine("Loot transferred to ship");
        }

        private void SayVoiceLine(string message)
        {
            // Implement logic to play voice lines based on the AI's actions
            Debug.Log($"AI Companion: {message}");
        }

        private int FirstEmptyItemSlot()
        {
            int result = -1;
            if (ItemSlots[currentItemSlot] == null)
            {
                result = currentItemSlot;
            }
            else
            {
                for (int i = 0; i < ItemSlots.Length; i++)
                {
                    if (ItemSlots[i] == null)
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        private Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport = false)
        {
            Debug.Log("Get Player Spawn Position");
            if (simpleTeleport)
            {
                return StartOfRound.Instance.playerSpawnPositions[0].position;
            }
            Debug.DrawRay(StartOfRound.Instance.playerSpawnPositions[playerNum].position, Vector3.up, Color.red, 15f);
            if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[playerNum].position, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
            {
                return StartOfRound.Instance.playerSpawnPositions[playerNum].position;
            }
            if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[playerNum].position + Vector3.up, 0.2f, 67108864, QueryTriggerInteraction.Ignore))
            {
                return StartOfRound.Instance.playerSpawnPositions[playerNum].position + Vector3.up * 0.5f;
            }
            for (int i = 0; i < StartOfRound.Instance.playerSpawnPositions.Length; i++)
            {
                if (i != playerNum)
                {
                    Debug.DrawRay(StartOfRound.Instance.playerSpawnPositions[i].position, Vector3.up, Color.green, 15f);
                    if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[i].position, 0.12f, -67108865, QueryTriggerInteraction.Ignore))
                    {
                        return StartOfRound.Instance.playerSpawnPositions[i].position;
                    }
                    if (!Physics.CheckSphere(StartOfRound.Instance.playerSpawnPositions[i].position + Vector3.up, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
                    {
                        return StartOfRound.Instance.playerSpawnPositions[i].position + Vector3.up * 0.5f;
                    }
                }
            }
            System.Random random = new System.Random(65);
            float y = StartOfRound.Instance.playerSpawnPositions[0].position.y;
            for (int j = 0; j < 15; j++)
            {
                Vector3 vector = new Vector3(random.Next((int)StartOfRound.Instance.shipInnerRoomBounds.bounds.min.x, (int)StartOfRound.Instance.shipInnerRoomBounds.bounds.max.x), y, random.Next((int)StartOfRound.Instance.shipInnerRoomBounds.bounds.min.z, (int)StartOfRound.Instance.shipInnerRoomBounds.bounds.max.z));
                vector = StartOfRound.Instance.shipInnerRoomBounds.transform.InverseTransformPoint(vector);
                Debug.DrawRay(vector, Vector3.up, Color.yellow, 15f);
                if (!Physics.CheckSphere(vector, 0.12f, 67108864, QueryTriggerInteraction.Ignore))
                {
                    return StartOfRound.Instance.playerSpawnPositions[j].position;
                }
            }
            return StartOfRound.Instance.playerSpawnPositions[0].position + Vector3.up * 0.5f;
        }
    }
}