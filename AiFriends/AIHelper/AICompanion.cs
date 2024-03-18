using System;
using UnityEngine;
using GameNetcodeStuff;
using Unity.Netcode;
using AiFriends.Helpers;
using UnityEngine.AI;
using System.Linq;
using AiFriends.Patches;

namespace AiFriends.AIHelper
{
    public class AICompanion : NetworkBehaviour
    {
        public static AICompanion Instance { get; private set; }

        private PlayerControllerB playerControllerB;
        public float followDistance = 5f;
        public float lootGatherRadius = 10f;
        public int inventoryCapacity = 10;
        public AILevel.AiLevel aiLevel = AILevel.AiLevel.medium;
        private const int HelperCost = 100;

        private bool isClimbingLadder = false;
        private bool isClimbingUp = true;
        private InteractTrigger currentLadder;
        private GrabbableObject[] nearbyLoot;

        private NavMeshAgent navMeshAgent;

        private void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
        }

        private void Awake()
        {
            playerControllerB = GetComponent<PlayerControllerB>();
            if (playerControllerB == null)
            {
                playerControllerB = gameObject.AddComponent<PlayerControllerB>();
            }
        }

        private void Update()
        {
            if (playerControllerB.isPlayerDead)
                return;

            if (isClimbingLadder)
            {
                ClimbLadder();
            }
            else
            {
                Explore();
            }

            CheckDoorInteraction();
        }

        private void ClimbLadder()
        {
            if (currentLadder == null)
            {
                isClimbingLadder = false;
                return;
            }

            Vector3 ladderTop = currentLadder.transform.position + Vector3.up * 5f;
            Vector3 ladderBottom = currentLadder.transform.position - Vector3.up * 5f;

            if (isClimbingUp)
            {
                SetDestinationToPosition(ladderTop);
                if (Vector3.Distance(transform.position, ladderTop) < 1f)
                {
                    isClimbingLadder = false;
                    currentLadder = null;
                }
            }
            else
            {
                SetDestinationToPosition(ladderBottom);
                if (Vector3.Distance(transform.position, ladderBottom) < 1f)
                {
                    isClimbingLadder = false;
                    currentLadder = null;
                }
            }
        }

        private void SetDestinationToPosition(Vector3 targetPosition)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(targetPosition);
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
            if (!IsServer)
            {
                Debug.Log("Spawning AI helper on client");

                GameObject helperObject = Instantiate(StartOfRound.Instance.playerPrefab, spawnPosition, Quaternion.identity);
                PlayerControllerB aiHelper = helperObject.GetComponent<PlayerControllerB>();

                AILevel.AiLevel aiLevel = (AILevel.AiLevel)Enum.Parse(typeof(AILevel.AiLevel), AILevel);
                PlayerControllerBPatch.SetAIHelperData(aiHelper, aiLevel);

                aiHelper.playerUsername = "AI Helper";
                aiHelper.isPlayerControlled = false;
                aiHelper.DisablePlayerModel(aiHelper.gameObject, enable: true);

                aiHelper.gameplayCamera.enabled = false;
                aiHelper.thisPlayerModelArms.enabled = false;
                aiHelper.visorCamera.enabled = false;
                aiHelper.playerScreen.enabled = false;
                aiHelper.activeAudioListener.enabled = false;
                aiHelper.gameObject.GetComponent<CharacterController>().enabled = false;
            }
        }

        [ServerRpc(RequireOwnership = false)]
        public void RequestSpawnHelperServerRpc(string AILevel)
        {
            Debug.Log("Spawning AI helper on server");

            if (!CanAffordHelper())
            {
                return;
            }
            DeductCredits();

            Vector3 spawnPosition = GetPlayerSpawnPosition(0);

            GameObject helperObject = Instantiate(StartOfRound.Instance.playerPrefab, spawnPosition, Quaternion.identity);
            PlayerControllerB aiHelper = helperObject.GetComponent<PlayerControllerB>();

            AILevel.AiLevel aiLevel = (AILevel.AiLevel)Enum.Parse(typeof(AILevel.AiLevel), AILevel);
            PlayerControllerBPatch.SetAIHelperData(aiHelper, aiLevel);

            aiHelper.playerUsername = "AI Helper";
            aiHelper.isPlayerControlled = false;
            aiHelper.DisablePlayerModel(aiHelper.gameObject, enable: true);

            aiHelper.gameplayCamera.enabled = false;
            aiHelper.thisPlayerModelArms.enabled = false;
            aiHelper.visorCamera.enabled = false;
            aiHelper.playerScreen.enabled = false;
            aiHelper.activeAudioListener.enabled = false;
            aiHelper.gameObject.GetComponent<CharacterController>().enabled = false;

            helperObject.GetComponent<NetworkObject>().Spawn();

            SpawnHelperClientRpc(spawnPosition, AILevel);
        }

        private void Explore()
        {
            bool isPlayerOutside = IsPlayerOutside(playerControllerB);
            Vector3 targetPosition = playerControllerB.serverPlayerPosition;

            if (!playerControllerB.isInsideFactory && FirstEmptyItemSlot() != -1)
            {
                targetPosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(true, isPlayerOutside), default, 5f, -1);

                if (!NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, new NavMeshPath()))
                {
                    InteractTrigger ladder = FindNearestLadder();
                    if (ladder != null)
                    {
                        targetPosition = ladder.transform.position;
                        currentLadder = ladder;
                    }
                }
            }
            else if (playerControllerB.isInsideFactory && FirstEmptyItemSlot() == -1)
            {
                targetPosition = RoundManager.Instance.GetNavMeshPosition(RoundManager.FindMainEntrancePosition(true, isPlayerOutside), default, 5f, -1);

                if (!NavMesh.CalculatePath(transform.position, targetPosition, NavMesh.AllAreas, new NavMeshPath()))
                {
                    InteractTrigger ladder = FindNearestLadder();
                    if (ladder != null)
                    {
                        targetPosition = ladder.transform.position;
                        currentLadder = ladder;
                    }
                }
            }
            else if (playerControllerB.isInsideFactory && FirstEmptyItemSlot() != -1)
            {
                ExploreAndGatherLoot();
            }
            else if (!playerControllerB.isInsideFactory && FirstEmptyItemSlot() == -1)
            {
                ReturnToShip();
            }
            else
            {
                navMeshAgent.ResetPath();
            }

            SetDestinationToPosition(targetPosition);

            if (currentLadder != null && Vector3.Distance(transform.position, currentLadder.transform.position) < 2f)
            {
                isClimbingLadder = true;
                isClimbingUp = !playerControllerB.isInsideFactory;
                currentLadder.Interact(playerControllerB.thisPlayerBody);
            }
        }

        private void CheckDoorInteraction()
        {
            Transform nearbyExitTransform = GetNearbyExitTransform();

            if (nearbyExitTransform)
            {
                if (base.IsOwner)
                {
                    navMeshAgent.enabled = false;
                    base.transform.position = nearbyExitTransform.position;
                    navMeshAgent.enabled = true;
                }
                else
                {
                    base.transform.position = nearbyExitTransform.position;
                }

                playerControllerB.isInsideFactory = !playerControllerB.isInsideFactory;
            }
        }

        private bool IsPlayerOutside(PlayerControllerB localPlayerController)
        {
            return localPlayerController != null && !localPlayerController.isInsideFactory;
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
                    case AILevel.AiLevel.easy:
                        // Simple exploration logic for easy AI
                        break;
                    case AILevel.AiLevel.medium:
                        // More advanced exploration logic for medium AI
                        break;
                    case AILevel.AiLevel.hard:
                        // Complex exploration logic for hard AI
                        break;
                }
            }

            // Implement logic to handle enemy encounters based on the AI level and situation
            // You can use the existing EnemyAI methods to detect and engage enemies
        }

        private void ReturnToShip()
        {
            Vector3 targetPosition = RoundManager.Instance.GetNavMeshPosition(GetPlayerSpawnPosition(0));
            SetDestinationToPosition(targetPosition);

            if (Vector3.Distance(transform.position, RoundManager.FindMainEntrancePosition()) < 2f)
            {
                DropLootInShip();
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
            int emptySlot = FirstEmptyItemSlot();
            if (emptySlot != -1)
            {
                playerControllerB.ItemSlots[emptySlot] = loot;
                loot.gameObject.SetActive(false);
                SayVoiceLine("Picked up " + loot.itemProperties.itemName);
            }
            else
            {
                SayVoiceLine("Inventory full, returning to ship");
            }
        }

        private InteractTrigger FindNearestLadder()
        {
            InteractTrigger[] ladders = FindObjectsOfType<InteractTrigger>();
            InteractTrigger nearestLadder = null;
            float minDistance = float.MaxValue;

            foreach (InteractTrigger ladder in ladders)
            {
                if (ladder.isLadder)
                {
                    float distance = Vector3.Distance(transform.position, ladder.transform.position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        nearestLadder = ladder;
                    }
                }
            }

            return nearestLadder;
        }

        public void DropLootInShip()
        {
            playerControllerB.DiscardHeldObject();
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
            if (playerControllerB.ItemSlots[playerControllerB.currentItemSlot] == null)
            {
                result = playerControllerB.currentItemSlot;
            }
            else
            {
                for (int i = 0; i < playerControllerB.ItemSlots.Length; i++)
                {
                    if (playerControllerB.ItemSlots[i] == null)
                    {
                        result = i;
                        break;
                    }
                }
            }
            return result;
        }

        private Transform GetNearbyExitTransform()
        {
            EntranceTeleport[] entrances = FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            foreach (EntranceTeleport entrance in entrances)
            {
                if (Vector3.Distance(base.transform.position, entrance.entrancePoint.position) < 1f)
                {
                    foreach (EntranceTeleport entrance2 in entrances)
                    {
                        if (entrance2.isEntranceToBuilding != entrance.isEntranceToBuilding && entrance2.entranceId == entrance.entranceId)
                            return entrance2.entrancePoint;
                    }
                }
            }
            return null;
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
            System.Random random = new(65);
            float y = StartOfRound.Instance.playerSpawnPositions[0].position.y;
            for (int j = 0; j < 15; j++)
            {
                Vector3 vector = new(random.Next((int)StartOfRound.Instance.shipInnerRoomBounds.bounds.min.x, (int)StartOfRound.Instance.shipInnerRoomBounds.bounds.max.x), y, random.Next((int)StartOfRound.Instance.shipInnerRoomBounds.bounds.min.z, (int)StartOfRound.Instance.shipInnerRoomBounds.bounds.max.z));
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