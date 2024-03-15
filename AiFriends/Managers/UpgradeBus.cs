using AiFriends.Misc;
using AiFriends.AIHelper;
using System.Collections.Generic;
using UnityEngine;

namespace AiFriends.Managers
{
    class UpgradeBus : MonoBehaviour
    {
        public static UpgradeBus Instance { get; private set; }
        public List<CustomTerminalNode> terminalNodes = new List<CustomTerminalNode>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }

            InitializeReviveNode();
        }

        private void InitializeReviveNode()
        {
            CustomTerminalNode reviveNode = CustomTerminalNode.CreateHelperNode();
            terminalNodes.Add(reviveNode);
        }

        public void HandleHelperRequest(string level)
        {
            Debug.Log($"Handle Helper Request");
            GetComponent<AICompanion>().RequestSpawnHelperServerRpc(level);
        }

        public TerminalNode ConstructNode()
        {
            TerminalNode modStoreInterface = ScriptableObject.CreateInstance<TerminalNode>();
            modStoreInterface.clearPreviousText = true;

            foreach (CustomTerminalNode terminalNode in terminalNodes)
            {
                string saleStatus = terminalNode.salePerc == 1f ? "" : "SALE";

                if (!terminalNode.Unlocked)
                {
                    modStoreInterface.displayText += $"\\n{terminalNode.Name} // {(int)(terminalNode.UnlockPrice * terminalNode.salePerc)} // {saleStatus} ";
                }
                else
                {
                    modStoreInterface.displayText += $"\n{terminalNode.Name} // UNLOCKED ";
                }
            }

            if (modStoreInterface.displayText == "")
            {
                modStoreInterface.displayText = "No upgrades available";
            }

            modStoreInterface.displayText += "\n\n";
            return modStoreInterface;
        }
    }
}
