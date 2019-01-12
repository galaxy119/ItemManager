using System;
using Smod2.API;
using UnityEngine;

namespace ItemManager.Utilities
{
    public interface ICustomItemHandler
    {
        /// <summary>
        /// The item ID to use when handling 914.
        /// </summary>
        int PsuedoType { get; }
        /// <summary>
        /// The item ID of a newly-created item.
        /// </summary>
        ItemType DefaultType { get; }
        
        /// <summary>
        /// Creates a custom item of this type at a location and rotation.
        /// </summary>
        /// <param name="position">Position of the custom item.</param>
        /// <param name="rotation">Rotation of the custom item.</param>
        CustomItem Create(Vector3 position, Quaternion rotation);
        /// <summary>
        /// Creates a custom item of this type and adds it to an inventory.
        /// </summary>
        /// <param name="inventory">Inventory of the player to create the item for.</param>
        CustomItem Create(Inventory inventory);
        /// <summary>
        /// Creates a custom item of this type from the pickup;
        /// </summary>
        /// <param name="pickup">Dropped to turn into custom item.</param>
        CustomItem Create(Pickup pickup);
        /// <summary>
        /// Creates a custom item of this type from an inventory.
        /// </summary>
        /// <param name="inventory">Inventory of the player to create the item from.</param>
        /// <param name="index">Index of the item in the inventory.</param>
        CustomItem Create(Inventory inventory, int index);
    }

    public class CustomItemHandler<TItem> : ICustomItemHandler where TItem : CustomItem, new()
    {
        public int PsuedoType { get; }
        public ItemType DefaultType { get; set;  }

        public CustomItemHandler(int psuedoId)
        {
            PsuedoType = psuedoId;
        }

        public void Register()
        {
            Items.RegisterItem(this);
        }

        public bool Unregister()
        {
            return Items.UnregisterItem(this);
        }

        private static TItem Create(Action<TItem> data)
        {
            TItem customItem = new TItem();
            data(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public CustomItem Create(Vector3 position, Quaternion rotation) => CreateOfType(position, rotation);
        public TItem CreateOfType(Vector3 position, Quaternion rotation) => Create(x => x.SetData(this, position, rotation));

        public CustomItem Create(Inventory inventory) => CreateOfType(inventory);
        public TItem CreateOfType(Inventory inventory) => Create(x => x.SetData(this, inventory));

        public CustomItem Create(Pickup pickup) => CreateOfType(pickup);
        public TItem CreateOfType(Pickup pickup) => Create(x => x.SetData(this, pickup));

        public CustomItem Create(Inventory inventory, int index) => CreateOfType(inventory, index);
        public TItem CreateOfType(Inventory inventory, int index) => Create(x => x.SetData(this, inventory, index));
    }
}
