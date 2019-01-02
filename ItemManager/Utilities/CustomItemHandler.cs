using ItemManager.Events;

using UnityEngine;

namespace ItemManager.Utilities
{
    internal interface ICustomItemHandler
    {
        /// <summary>
        /// The item ID to use when handling 914.
        /// </summary>
        int PsuedoId { get; }

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

    internal class CustomItemHandler<TItem> : ICustomItemHandler where TItem : CustomItem, new()
    {
        public int PsuedoId { get; }

        public CustomItemHandler(int psuedoId)
        {
            PsuedoId = psuedoId;
        }

        private static void RegisterEvents(TItem item)
        {
            if (item is IDoubleDroppable)
            {
                Items.readyForDoubleDrop.Add(item.UniqueId, false);
            }
        }

        public CustomItem Create(Vector3 position, Quaternion rotation)
        {
            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                durability = 0,
                Index = -1
            };

            customItem.Dropped = Items.hostInventory.SetPickup((int)customItem.DefaultItemId,
                customItem.UniqueId,
                position,
                rotation,
                0, 0, 0).GetComponent<Pickup>();
            customItem.ApplyPickup();

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public CustomItem Create(Inventory inventory)
        {
            if (inventory.items.Count > 8)
            {
                return Create(inventory.gameObject.transform.position, inventory.gameObject.transform.rotation);
            }

            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),
                    
                durability = 0,
                PlayerObject = inventory.gameObject,
                Inventory = inventory,
                Index = inventory.items.Count
            };
            inventory.AddNewItem((int)customItem.DefaultItemId, 0);
            customItem.ApplyInventory();

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public CustomItem Create(Pickup pickup)
        {
            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                durability = pickup.info.durability,
                Dropped = pickup,
                Index = -1
            };
            customItem.ApplyPickup();
            customItem.Type = customItem.DefaultItemId;

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public CustomItem Create(Inventory inventory, int index)
        {
            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                durability = inventory.items[index].durability,
                PlayerObject = inventory.gameObject,
                Inventory = inventory,
                Index = index
            };
            customItem.ApplyInventory();
            customItem.Type = customItem.DefaultItemId;

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }
    }
}
