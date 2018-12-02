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
        
        private ItemType itemId;
        /// <summary>
        /// The ID of the item to impersonate.
        /// </summary>
        public ItemType ItemId {
            get => itemId;
            set {
                itemId = value;

                if (Pickup == null) {
                    Inventory.SyncItemInfo item = Inventory.items[Index];
                    item.id = (int)itemId;

                    Inventory.items[Index] = item;
                }
                else {
                    Pickup.PickupInfo info = Pickup.info;
                    info.itemId = (int)itemId;

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

        private Pickup pickup;

        /// <summary>
        /// The dropped item entity (null if none).
        /// </summary>
        public Pickup Pickup {
            get => pickup;
            set {
                pickup = value;

                if (pickup == null) {
                    Inventory.SyncItemInfo info = Inventory.items[Index];
                    info.durability = durability;
                    Inventory.items[Index] = info;
                }
                else {
                    durability = pickup.info.durability;

                    Pickup.PickupInfo info = pickup.info;
                    info.durability = UniqueId;
                    pickup.Networkinfo = info;
                }
            }
        }

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
                }
                else {
                    durability = value;
                }
            }
        }
        /// <summary>
        /// Default durability of a newly-created item.
        /// </summary>
        public virtual float DefaultDurability => Items.DefaultDurability;

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
        public virtual bool OnPickup() {
            return true;
        }

        public virtual void On914(KnobSetting knob, Vector3 output) { }
    }
}
