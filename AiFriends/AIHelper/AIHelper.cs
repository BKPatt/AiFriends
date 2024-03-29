﻿using AiFriends.Helpers;
using GameNetcodeStuff;
using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;


namespace AiFriends.AIHelper
{
    public class AIHelper : MonoBehaviour
    {
        public Helper.AiLevel aiLevel;
        public bool isAIPlayer = true;
        public float followDistance = 5f;
        public float lootGatherRadius = 10f;
        public int inventoryCapacity = 10;
        private bool isClimbingLadder = false;
        private bool isClimbingUp = true;

        private Animator animator;
        private CharacterController characterController;
        private float jumpCooldown;
        private bool isJumping;
        private bool isGrounded;
        private bool isCrouching;
        private bool isSideways;

        private NavMeshAgent navMeshAgent;
        private InteractTrigger currentLadder;
        private GrabbableObject[] nearbyLoot;
        public GrabbableObject[] ItemSlots;
        public int currentItemSlot;

        private void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            ItemSlots = new GrabbableObject[inventoryCapacity];
            currentItemSlot = 0;
            animator = GetComponentInChildren<Animator>();
            characterController = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (navMeshAgent == null)
            {
                Debug.LogError("NavMeshAgent is null in Update");
                return;
            }

            HandleJumping();
            HandleLadderClimbing();
            HandleGroundedState();
            CheckDoorInteraction();

            switch (aiLevel)
            {
                case Helper.AiLevel.easy:
                    EasyAILogic();
                    break;
                case Helper.AiLevel.medium:
                    MediumAILogic();
                    break;
                case Helper.AiLevel.hard:
                    HardAILogic();
                    break;
            }

            bool isWalking = navMeshAgent.velocity.magnitude > 0.1f && navMeshAgent.velocity.magnitude <= 3f;
            animator.SetBool("Walking", isWalking);

            bool isSprinting = navMeshAgent.velocity.magnitude > 3f;
            animator.SetBool("Sprinting", isSprinting);

            animator.SetBool("Jumping", isJumping);
            animator.SetBool("Crouching", isCrouching);
            animator.SetBool("Sideways", isSideways);

            float animationSpeed = navMeshAgent.velocity.magnitude / navMeshAgent.speed;
            animator.SetFloat("AnimationSpeed", animationSpeed);
        }

        private void EasyAILogic()
        {
            Explore();
        }

        private void MediumAILogic()
        {
            // AvoidObstacles();
            Explore();
        }

        private void HardAILogic()
        {
            // AvoidObstacles();
            Explore();
            // EngageEnemies();
        }

        

        private void HandleJumping()
        {
            if (isJumping)
            {
                animator.SetBool("Jumping", true);

                if (jumpCooldown > 0)
                {
                    jumpCooldown -= Time.deltaTime;
                }
                else
                {
                    isJumping = false;
                }
            }
            else
            {
                animator.SetBool("Jumping", false);

                if (isGrounded && navMeshAgent.remainingDistance < navMeshAgent.baseOffset)
                {
                    jumpCooldown = 1f;
                    isJumping = true;
                    characterController.Move(Vector3.up * 5f);
                }
            }
        }

        private void HandleLadderClimbing()
        {
            InteractTrigger ladder = FindNearestLadder();
            if (ladder != null && Vector3.Distance(transform.position, ladder.transform.position) < 2f)
            {
                isClimbingLadder = true;
                isClimbingUp = transform.position.y < ladder.topOfLadderPosition.position.y;
                currentLadder = ladder;
                currentLadder.Interact(transform);
                animator.SetTrigger("EnterLadder");
                animator.SetBool("ClimbingLadder", value: true);
            }
            else
            {
                isClimbingLadder = false;
                currentLadder = null;
                animator.SetBool("ClimbingLadder", value: false);
            }

            if (isClimbingLadder)
            {
                ClimbLadder();
            }
        }

        private void ClimbLadder()
        {
            if (currentLadder == null)
                return;

            Vector3 targetPosition = isClimbingUp ? currentLadder.topOfLadderPosition.position : currentLadder.bottomOfLadderPosition.position;

            transform.position = Vector3.MoveTowards(transform.position, targetPosition, 2f * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
            {
                isClimbingLadder = false;
                currentLadder = null;
            }
        }

        private void HandleGroundedState()
        {
            isGrounded = characterController.isGrounded;
            animator.SetBool("IsGrounded", isGrounded);
        }

        private int FirstEmptyItemSlot()
        {
            int result = -1;

            if (ItemSlots != null && currentItemSlot >= 0 && currentItemSlot < ItemSlots.Length)
            {
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
            }

            return result;
        }

        private void Explore()
        {
            bool isPlayerOutside = base.transform.position.y > -80f;
            Vector3 targetPosition = transform.position;

            navMeshAgent.speed = 3f;
            navMeshAgent.acceleration = 10f;

            if (isPlayerOutside && FirstEmptyItemSlot() != -1)
            {
                targetPosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isPlayerOutside);

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
            else if (!isPlayerOutside && FirstEmptyItemSlot() == -1)
            {
                targetPosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isPlayerOutside);

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
            else if (!isPlayerOutside && FirstEmptyItemSlot() != -1)
            {
                ExploreAndGatherLoot();
            }
            else if (isPlayerOutside && FirstEmptyItemSlot() == -1)
            {
                ReturnToShip();
            }
            else
            {
                navMeshAgent.ResetPath();
            }

            if (navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(targetPosition);
            }
            else
            {
                animator.SetFloat("MoveSpeed", 1f);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, 2f * Time.deltaTime);
            }

            if (isClimbingLadder)
            {
                ClimbLadder();
                animator.SetBool("IsClimbing", true);
            }
            else
            {
                animator.SetBool("IsClimbing", false);
            }
        }

        private void DropLootInShip()
        {
            for (int i = 0; i < ItemSlots.Length; i++)
            {
                if (ItemSlots[i] != null)
                {
                    ItemSlots[i].gameObject.SetActive(true);
                    ItemSlots[i].transform.position = RoundManager.FindMainEntrancePosition();
                    ItemSlots[i] = null;
                }
            }

            Helper.Player.SayVoiceLine("Loot transferred to ship");
        }

        private void PickupLoot(GrabbableObject loot)
        {
            int emptySlot = FirstEmptyItemSlot();

            if (emptySlot != -1)
            {
                ItemSlots[emptySlot] = loot;
                loot.gameObject.SetActive(false);
                Helper.Player.SayVoiceLine("Picked up " + loot.itemProperties.itemName);
            }
            else
            {
                Helper.Player.SayVoiceLine("Inventory full, returning to ship");
            }
        }

        private void CheckDoorInteraction()
        {
            Transform nearbyExitTransform = GetNearbyExitTransform();

            if (nearbyExitTransform)
            {
                navMeshAgent.enabled = false;
                base.transform.position = nearbyExitTransform.position;
                navMeshAgent.enabled = true;
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
                    case Helper.AiLevel.easy:
                        // Simple exploration logic for easy AI
                        break;
                    case Helper.AiLevel.medium:
                        // More advanced exploration logic for medium AI
                        break;
                    case Helper.AiLevel.hard:
                        // Complex exploration logic for hard AI
                        break;
                }
            }

            // Implement logic to handle enemy encounters based on the AI level and situation
            // You can use the existing EnemyAI methods to detect and engage enemies
        }

        private void ReturnToShip()
        {
            Vector3 targetPosition = RoundManager.Instance.GetNavMeshPosition(Helper.PlayerSpawn.GetPlayerSpawnPosition(0));
            SetDestinationToPosition(targetPosition);

            if (Vector3.Distance(transform.position, RoundManager.FindMainEntrancePosition()) < 2f)
            {
                DropLootInShip();
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

        private GrabbableObject[] FindNearbyLoot()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, lootGatherRadius);
            return Array.FindAll(colliders, collider => collider.GetComponent<GrabbableObject>() != null)
                .Select(collider => collider.GetComponent<GrabbableObject>())
                .ToArray();
        }

        private Transform GetNearbyExitTransform()
        {
            EntranceTeleport[] entrances = FindObjectsOfType<EntranceTeleport>(includeInactive: false);
            foreach (EntranceTeleport entrance in entrances)
            {
                if (Vector3.Distance(transform.position, entrance.entrancePoint.position) < 1f)
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

        private void SetDestinationToPosition(Vector3 targetPosition)
        {
            if (navMeshAgent != null && navMeshAgent.isOnNavMesh)
            {
                navMeshAgent.SetDestination(targetPosition);
            }
        }
    }
}