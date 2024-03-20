using AiFriends.Helpers;
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

        private NavMeshAgent navMeshAgent;
        private InteractTrigger currentLadder;
        private GrabbableObject[] nearbyLoot;
        public GrabbableObject[] ItemSlots;
        public int currentItemSlot;
        private readonly float smoothingFactor = 5f;
        private bool exploreRan = false;
        private bool allowDoorInteraction = false;

        private void Start()
        {
            navMeshAgent = GetComponent<NavMeshAgent>();
            navMeshAgent.speed = 5f;
            navMeshAgent.updatePosition = false;
            navMeshAgent.updateRotation = true;

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

            bool isWalking = navMeshAgent.velocity.magnitude > 0.01f;
            animator.SetBool("Walking", isWalking);

            bool isSprinting = navMeshAgent.velocity.magnitude > 7f;
            animator.SetBool("Sprinting", isSprinting);

            animator.SetBool("Jumping", isJumping);

            float animationSpeed = Mathf.Lerp(animator.GetFloat("AnimationSpeed"), navMeshAgent.velocity.magnitude / navMeshAgent.speed, Time.deltaTime * smoothingFactor);
            animator.SetFloat("AnimationSpeed", animationSpeed);

            if (navMeshAgent.isOnNavMesh && !isClimbingLadder)
            {
                transform.position = navMeshAgent.nextPosition;
            }
        }

        private void EasyAILogic()
        {
            if (!exploreRan)
            {
                Explore();
            }
        }

        private void MediumAILogic()
        {
            // AvoidObstacles();
            if (!exploreRan)
            {
                Explore();
            }
        }

        private void HardAILogic()
        {
            // AvoidObstacles();
            if (!exploreRan)
            {
                Explore();
            }
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
                    navMeshAgent.enabled = false;
                    characterController.Move(Vector3.up * 5f);
                    navMeshAgent.enabled = true;
                }
            }
        }

        private void HandleLadderClimbing()
        {
            InteractTrigger ladder = FindNearestLadder();
            if (ladder != null && Vector3.Distance(transform.position, ladder.transform.position) < 2f)
            {
                if (!isClimbingLadder)
                {
                    StartCoroutine(DelayLadderClimbing(ladder));
                }
                else
                {
                    ClimbLadder();
                }
            }
            else
            {
                isClimbingLadder = false;
                currentLadder = null;
                animator.SetBool("ClimbingLadder", value: false);
            }
        }

        private IEnumerator DelayLadderClimbing(InteractTrigger ladder)
        {
            yield return new WaitForSeconds(0.5f);

            isClimbingLadder = true;
            isClimbingUp = transform.position.y < ladder.topOfLadderPosition.position.y;
            currentLadder = ladder;
            currentLadder.Interact(transform);
            animator.SetTrigger("EnterLadder");
            animator.SetBool("ClimbingLadder", value: true);
        }

        private void ClimbLadder()
        {
            if (currentLadder == null)
                return;

            Vector3 targetPosition = isClimbingUp ? currentLadder.topOfLadderPosition.position : currentLadder.bottomOfLadderPosition.position;

            if (Vector3.Distance(transform.position, currentLadder.transform.position) < 0.5f)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, 2f * Time.deltaTime);

                if (Vector3.Distance(transform.position, targetPosition) < 0.1f)
                {
                    if (isClimbingUp)
                    {
                        isClimbingLadder = false;
                        currentLadder = null;
                        animator.SetBool("ClimbingLadder", false);
                    }
                    else
                    {
                        isClimbingUp = true;
                    }
                }
            }
            else
            {
                transform.position = Vector3.MoveTowards(transform.position, currentLadder.transform.position, 2f * Time.deltaTime);
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

            if (isPlayerOutside)
            {
                HandleLadderClimbing();
                if (!exploreRan || navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
                {
                    if (FirstEmptyItemSlot() != -1)
                    {
                        Debug.Log("Player is outside and going to the main entrance");

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
                        allowDoorInteraction = true;
                    }
                    else if (FirstEmptyItemSlot() == -1)
                    {
                        Debug.Log("Player is outside and returning to the ship");

                        ReturnToShip();
                    }
                    else
                    {
                        Debug.Log("Resetting path");

                        navMeshAgent.ResetPath();
                    }
                }
            }
            else if (!isPlayerOutside)
            {
                if (!exploreRan || navMeshAgent.remainingDistance < navMeshAgent.stoppingDistance)
                {
                    if (FirstEmptyItemSlot() == -1)
                    {
                        Debug.Log("Player is inside and returning to the main entrance");

                        targetPosition = RoundManager.FindMainEntrancePosition(getTeleportPosition: true, isPlayerOutside);
                        allowDoorInteraction = true;
                    }
                    else if (FirstEmptyItemSlot() != -1)
                    {
                        Debug.Log("Player is inside and trying to gather loot");

                        ExploreAndGatherLoot();
                        return;
                    }
                    else
                    {
                        Debug.Log("Resetting path");

                        navMeshAgent.ResetPath();
                    }
                }
            }
                
                
                


            if (navMeshAgent.isOnNavMesh && isPlayerOutside)
            {
                Debug.Log("Setting Destination");

                navMeshAgent.SetDestination(targetPosition);
                exploreRan = true;
            }
            else
            {
                animator.SetFloat("MoveSpeed", 1f);
                transform.position = Vector3.MoveTowards(transform.position, targetPosition, 1.5f * Time.deltaTime);
            }

            if (isClimbingLadder)
            {
                ClimbLadder();
            }
        }

        private void CalculateMultiWaypointPath(Vector3 targetPosition)
        {
            NavMeshPath path = new();
            navMeshAgent.CalculatePath(targetPosition, path);

            if (path.status == NavMeshPathStatus.PathComplete)
            {
                navMeshAgent.SetPath(path);
            }
            else
            {
                if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 5f, NavMesh.AllAreas))
                {
                    navMeshAgent.SetDestination(hit.position);
                }
            }

            if (IsObstacleAhead() && isGrounded)
            {
                Jump();
            }
            exploreRan = true;
        }

        private bool IsObstacleAhead()
        {
            Debug.Log($"IsObstacleAhead()");

            Vector3 origin = transform.position + Vector3.up * 0.5f;
            Vector3 direction = transform.forward;
            float distance = 1.5f;
            float jumpableHeight = 1.5f;

            if (Physics.Raycast(origin, direction, out RaycastHit hit, distance))
            {
                if (hit.collider.gameObject.GetComponent<NavMeshObstacle>() != null)
                {
                    if (hit.collider.bounds.size.y <= jumpableHeight)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void Jump()
        {
            Debug.Log($"Jump");
            animator.SetTrigger("Jump");
            characterController.Move(Vector3.up * 5f);
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
            exploreRan = false;
        }

        private void PickupLoot(GrabbableObject loot)
        {
            Debug.Log($"Pick up loot: {loot}");

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
            Transform nearbyExitTransform = null;
            if (allowDoorInteraction)
            {
                nearbyExitTransform = GetNearbyExitTransform();
            }

            if (nearbyExitTransform)
            {
                navMeshAgent.enabled = false;
                base.transform.position = nearbyExitTransform.position;
                navMeshAgent.enabled = true;

                if (nearbyExitTransform.GetComponent<EntranceTeleport>().isEntranceToBuilding)
                {
                    exploreRan = false;
                }
                allowDoorInteraction = false;
            }
        }

        private void ExploreAndGatherLoot()
        {
            Debug.Log($"ExploreAndGatherLoot()");

            nearbyLoot = FindNearbyLoot();
            Debug.Log($"Nearby Loot: {nearbyLoot}");

            if (nearbyLoot.Length > 0)
            {
                GrabbableObject loot = nearbyLoot[0];
                Debug.Log($"Loot Position: {loot}");
                SetDestinationToPosition(loot.transform.position);

                if (Vector3.Distance(transform.position, loot.transform.position) < 1f)
                {
                    PickupLoot(loot);
                }
            }
            else
            {
                switch (aiLevel)
                {
                    case Helper.AiLevel.easy:
                        // Leave as is
                        Wander();
                        break;
                    case Helper.AiLevel.medium:
                        // Make more complicated later
                        Wander();
                        break;
                    case Helper.AiLevel.hard:
                        // Make the most complex later
                        Wander();
                        break;
                }
            }
        }

        private void Wander()
        {
            Debug.Log($"Wander()");
            Vector3 randomPosition = GetRandomPositionInsideFacility();
            Debug.Log($"RandomPosition: {randomPosition}");
            SetDestinationToPosition(randomPosition);
        }

        public Vector3 GetRandomPositionInsideFacility()
        {
            Debug.Log($"GetRandomPositionInsideFacility()");
            Bounds bounds = new Bounds();
            foreach (GameObject node in RoundManager.Instance.insideAINodes)
            {
                bounds.Encapsulate(node.transform.position);
            }

            bounds.Expand(1f);

            NavMeshHit hit;
            Vector3 randomPosition = Vector3.zero;
            int attempts = 0;
            while (attempts < 10)
            {
                randomPosition = RandomPointInBounds(bounds);
                Debug.Log($"Random Position: {randomPosition}");

                if (NavMesh.SamplePosition(randomPosition, out hit, 2f, NavMesh.AllAreas))
                {
                    return hit.position;
                }
                attempts++;
                Debug.Log($"Attempts: {attempts}");
            }

            Debug.Log($"Bounds: {bounds.center}");
            return bounds.center;
        }

        private static Vector3 RandomPointInBounds(Bounds bounds)
        {
            return new Vector3(
                UnityEngine.Random.Range(bounds.min.x, bounds.max.x),
                UnityEngine.Random.Range(bounds.min.y, bounds.max.y),
                UnityEngine.Random.Range(bounds.min.z, bounds.max.z)
            );
        }

        private void ReturnToShip()
        {
            Debug.Log("ReturnToShip()");

            Vector3 targetPosition = RoundManager.Instance.GetNavMeshPosition(Helper.PlayerSpawn.GetPlayerSpawnPosition(0));
            SetDestinationToPosition(targetPosition);

            if (Vector3.Distance(transform.position, RoundManager.FindMainEntrancePosition()) < 2f)
            {
                DropLootInShip();
            }
        }

        private InteractTrigger FindNearestLadder()
        {
            Debug.Log("FindNearestLadder()");

            InteractTrigger[] ladders = FindObjectsOfType<InteractTrigger>();
            InteractTrigger nearestLadder = null;
            float minDistance = float.MaxValue;

            foreach (InteractTrigger ladder in ladders)
            {
                if (ladder.isLadder && ladder.GetComponent<Collider>().bounds.Contains(transform.position))
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
            Debug.Log($"FindNearbyLoot()");
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
                        {
                            allowDoorInteraction = false;
                            return entrance2.entrancePoint;
                        }
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