using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace AiFriends.Helpers
{
    public static partial class Helper
    {
        public static class PlayerSpawn
        {
            public static Vector3 GetPlayerSpawnPosition(int playerNum, bool simpleTeleport = false)
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
}
