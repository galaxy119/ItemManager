using ItemManager.Events;
using UnityEngine;

namespace ItemManager.Utilities {
    internal abstract class CustomItemHandler {
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

    internal class CustomItemHandler<TItem> : CustomItemHandler where TItem : CustomItem, new() {
        public override int PsuedoId { get; }

        public CustomItemHandler(int psuedoId) {
            PsuedoId = psuedoId;
        }

        private static void RegisterEvents(TItem item) {
            if (item is IDoubleDroppable doubleDroppable) {
                Items.registeredDoubleDrop.Add(item.UniqueId, doubleDroppable);
                Items.readyForDoubleDrop.Add(item.UniqueId, false);
            }

            if (item is IWeapon shootable) {
                Items.registeredWeapons.Add(item.UniqueId, shootable);
            }
        }

        public override CustomItem Create(Vector3 position, Quaternion rotation) {
            TItem customItem = new TItem {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId()
            };

            customItem.Pickup = Items.hostInventory.SetPickup((int) customItem.DefaultItemId,
                customItem.DefaultDurability, position,
                rotation).GetComponent<Pickup>();

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public override CustomItem Create(Pickup pickup) {
            TItem customItem = new TItem {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                Pickup = pickup
            };

            Pickup.PickupInfo info = pickup.info;
            info.itemId = (int) customItem.ItemId;
            info.durability = customItem.UniqueId;
            pickup.Networkinfo = info;

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }

        public override CustomItem Create(Inventory inventory, int index) {
            TItem customItem = new TItem {
                PsuedoType = PsuedoId,
                UniqueId = Items.ids.NewId(),

                Inventory = inventory,
                Index = index
            };

            Inventory.SyncItemInfo info = inventory.items[index];
            info.id = (int) customItem.ItemId;
            inventory.items[index] = info;

            RegisterEvents(customItem);
            customItem.OnInitialize();

            return customItem;
        }
    }
}
