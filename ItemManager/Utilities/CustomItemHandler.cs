using ItemManager.Events;
using UnityEngine;

namespace ItemManager.Utilities
{
    internal abstract class CustomItemHandler
    {
        /// <summary>
        /// The item ID to use when handling 914.
        /// </summary>
        public abstract int PsuedoId { get; }

        /// <summary>
        /// Creates a custom item of this type at a location and rotation.
        /// </summary>
        /// <param name="position">Position of the custom item.</param>
        /// <param name="rotation">Rotation of the custom item.</param>
        public abstract CustomItem Create(Vector3 position, Quaternion rotation);
        /// <summary>
        /// Creates a custom item of this type and adds it to an inventory.
        /// </summary>
        /// <param name="inventory">Inventory of the player to create the item for.</param>
        public abstract CustomItem Create(Inventory inventory);
        /// <summary>
        /// Creates a custom item of this type from the pickup;
        /// </summary>
        /// <param name="pickup">Pickup to turn into custom item.</param>
        public abstract CustomItem Create(Pickup pickup);
        /// <summary>
        /// Creates a custom item of this type from an inventory.
        /// </summary>
        /// <param name="inventory">Inventory of the player to create the item from.</param>
        /// <param name="index">Index of the item in the inventory.</param>
        public abstract CustomItem Create(Inventory inventory, int index);
    }

    internal class CustomItemHandler<TItem> : CustomItemHandler where TItem : CustomItem, new()
    {
        public override int PsuedoId { get; }

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

        public override CustomItem Create(Vector3 position, Quaternion rotation)
        {
            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),
            };

            customItem.Pickup = Items.hostInventory.SetPickup((int)customItem.DefaultItemId,
                customItem.UniqueId,
                position,
                rotation,
                0, 0, 0).GetComponent<Pickup>();

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public override CustomItem Create(Inventory inventory)
        {
            if (inventory.items.Count > 8)
            {
                return Create(inventory.gameObject.transform.position, inventory.gameObject.transform.rotation);
            }
            else
            {
                TItem customItem = new TItem
                {
                    PsuedoType = PsuedoId,
                    UniqueId = Items.ids.NewId(),
                    
                    durability = Items.DefaultDurability,
                    Player = inventory.gameObject,
                    Inventory = inventory,
                    Index = inventory.items.Count
                };
                inventory.AddNewItem((int)customItem.DefaultItemId, 0);
                customItem.ApplyInventory();

                RegisterEvents(customItem);
                customItem.OnInitialize();

                return customItem;
            }
        }

        public override CustomItem Create(Pickup pickup)
        {
            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                durability = pickup.info.durability,
                Pickup = pickup
            };
            customItem.ApplyPickup();
            customItem.ItemType = customItem.DefaultItemId;

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public override CustomItem Create(Inventory inventory, int index)
        {
            TItem customItem = new TItem
            {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                durability = inventory.items[index].durability,
                Player = inventory.gameObject,
                Inventory = inventory,
                Index = index
            };
            customItem.ApplyInventory();
            customItem.ItemType = customItem.DefaultItemId;

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }
    }
}
