using System.Collections.Generic;

namespace ItemManager.Utilities
{
    public class WorldCustomItems
    {
        private readonly List<CustomItem> instances;
        internal readonly Dictionary<float, CustomItemData> instancePairs;

        public IReadOnlyList<CustomItem> Instances => instances;
        public ICustomItemHandler Handler { get; }

        public WorldCustomItems(ICustomItemHandler handler)
        {
            Handler = handler;
            instancePairs = new Dictionary<float, CustomItemData>();
            instances = new List<CustomItem>();
        }

        internal void Add(float id, CustomItem item)
        {
            instancePairs.Add(id, new CustomItemData(item));
            instances.Add(item);
        }

        internal void Remove(float id)
        {
            instances.Remove(instancePairs[id].Item);
            instancePairs.Remove(id);
        }

        internal void Clear()
        {
            instancePairs.Clear();
            instances.Clear();
        }
    }

    public class WorldCustomWeapons : WorldCustomItems
    {
        public int DefaultAmmo => ((ICustomWeaponHandler) Handler).DefaultReserveAmmo;
        public Dictionary<int, int> AmmoReserves { get; }

        public WorldCustomWeapons(ICustomItemHandler handler) : base(handler)
        {
            AmmoReserves = new Dictionary<int, int>();
        }
    }
}
