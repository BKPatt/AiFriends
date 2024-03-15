using UnityEngine;

namespace AiFriends.Misc
{
    public class CustomTerminalNode
    {
        public string Name;
        public int[] Prices;
        public int UnlockPrice;
        public string Description;
        public GameObject Prefab;
        public bool Unlocked = false;
        public float salePerc = 1f;

        public CustomTerminalNode(string name, int unlockPrice, string description, GameObject prefab, int[] prices = null)
        {
            if (prices == null) { prices = new int[0]; }
            Name = name;
            Prices = prices;
            Description = description;
            Prefab = prefab;
            UnlockPrice = unlockPrice;
        }

        public static CustomTerminalNode CreateHelperNode()
        {
            return new CustomTerminalNode("AI Helper", 100, "Hire a Helper", null);
        }

        public CustomTerminalNode Copy()
        {
            return new CustomTerminalNode
            (
                Name = this.Name,
                UnlockPrice = this.UnlockPrice,
                Description = this.Description,
                Prefab = this.Prefab,
                Prices = this.Prices
            );
        }
    }
}
