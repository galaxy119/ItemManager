using scp4aiur;
using Smod2.API;
using UnityEngine;

namespace ItemManager {
    public abstract class CustomItem {
        /// <summary>
        /// The durability of the item in the pickup state, used for ID purposes.
        /// </summary>
        public float UniqueId { get; internal set; }
        /// <summary>
        /// The psuedo ID to check custom item types.
        /// </summary>
        public int PsuedoType { get; internal set; }
        
        /// <summary>
        /// The ID of the item to impersonate.
        /// </summary>
        public ItemType ItemType {
            get => Pickup == null ? (ItemType) Inventory.items[Index].id : (ItemType) Pickup.info.itemId;
            set {
                if (Pickup == null) {
                    Inventory.SyncItemInfo info = Inventory.items[Index];
                    info.id = (int)value;

                    Inventory.items[Index] = info;
                } else {
                    Pickup.PickupInfo info = Pickup.info;
                    info.itemId = (int)value;

                    Pickup.Networkinfo = info;
                }
            }
        }

        /// <summary>
        /// The item ID of a newly-created item.
        /// </summary>
        public abstract ItemType DefaultItemId { get; }

        /// <summary>
        /// The player (null if none) that is holding this item.
        /// </summary>
        public GameObject Player { get; internal set; }
        /// <summary>
        /// The inventory of the player (null if none) that is holding this item.
        /// </summary>
        public Inventory Inventory { get; internal set; }
        /// <summary>
        /// The index of the player's inventory (-1 if none) that has the item.
        /// </summary>
        public int Index { get; internal set; }
        
        /// <summary>
        /// The dropped item entity (null if none).
        /// </summary>
        public Pickup Pickup { get; internal set; }

        internal float durability;
        /// <summary>
        /// <para>ALWAYS USE THIS FOR DURABILITY.</para>
        /// <para>Because the ID system is durability based, this must be handled by this property's custom logic.</para>
        /// </summary>
        public float Durability {
            get => Pickup == null ? Inventory.items[Index].durability : durability;
            set {
                if (Pickup == null) {
                    Inventory.SyncItemInfo item = Inventory.items[Index];
                    item.durability = value;
                    Inventory.items[Index] = item;
                } else {
                    durability = value;
                }
            }
        }

        /// <summary>
        /// The status of whether or not this item has been deleted from the world.
        /// </summary>
        public bool Deleted { get; private set; }
        public void Delete() {
            Deleted = true;

            Items.customItems.Remove(UniqueId);

            if (Items.registeredDoubleDrop.Remove(UniqueId)) { //if double droppable
                Timing.RemoveTimer(Items.doubleDropTimers[UniqueId]);
                Items.doubleDropTimers.Remove(UniqueId);

                Items.readyForDoubleDrop.Remove(UniqueId);
            }

            OnDeleted();
        }

        public virtual void OnInitialize() { }
        public virtual void OnDeleted() { }

        public virtual bool OnDrop() {
            return true;
        }
        public virtual bool OnDeathDrop(GameObject attacker, DamageType damage) {
            return true;
        }
        public virtual bool OnPickup() {
            return true;
        }

        public virtual void On914(KnobSetting knob, Vector3 output, bool heldItem) { }

        internal void SetItemType(ItemType value) {
            ItemType = value;
        }

        internal void SetDurability(float value) {
            Durability = value;
        }

        internal void ApplyPickup() {
            durability = Pickup.info.durability;

            Pickup.PickupInfo info = Pickup.info;
            info.durability = UniqueId;
            Pickup.Networkinfo = info;
        }

        internal void ApplyInventory() {
            Inventory.SyncItemInfo info = Inventory.items[Index];
            info.durability = durability;
            Inventory.items[Index] = info;
        }

        internal void ApplyPickupChangee() {
            if (Pickup == null) {
                ApplyInventory();
            } else {
                ApplyPickup();
            }
        }
    }
}
