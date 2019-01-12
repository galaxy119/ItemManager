using ItemManager.Utilities;
using Smod2.API;
using UnityEngine;

namespace ItemManager
{
    public abstract class CustomItem
    {
        private readonly Items manager;
        private readonly WorldCustomItems items;

        /// <summary>
        /// The durability of the item in the pickup state, used for ID purposes.
        /// </summary>
        public float UniqueId { get; internal set; }

        /// <summary>
        /// The ID of the item to impersonate.
        /// </summary>
        public ItemType Type
        {
            get => Dropped == null ? (ItemType)Inventory.items[Index].id : (ItemType)Dropped.info.itemId;
            set
            {
                if (Dropped == null)
                {
                    Inventory.SyncItemInfo info = Inventory.items[Index];
                    info.id = (int)value;

                    Inventory.items[Index] = info;
                }
                else
                {
                    Pickup.PickupInfo info = Dropped.info;
                    info.itemId = (int)value;

                    Dropped.Networkinfo = info;
                }
            }
        }

        /// <summary>
        /// The item ID of a newly-created item.
        /// </summary>
        public abstract ItemType DefaultItemId { get; }

        public int Sight
        {
            get => Dropped == null ? Inventory.items[Index].modSight : Dropped.info.weaponMods[0];
            set
            {
                if (Dropped == null)
                {
                    Inventory.SyncItemInfo info = Inventory.items[Index];
                    info.modSight = value;

                    Inventory.items[Index] = info;
                }
                else
                {
                    Pickup.PickupInfo info = Dropped.info;
                    info.weaponMods[0] = value;

                    Dropped.Networkinfo = info;
                }
            }
        }

        public int Barrel
        {
            get => Dropped == null ? Inventory.items[Index].modBarrel : Dropped.info.weaponMods[1];
            set
            {
                if (Dropped == null)
                {
                    Inventory.SyncItemInfo info = Inventory.items[Index];
                    info.modBarrel = value;

                    Inventory.items[Index] = info;
                }
                else
                {
                    Pickup.PickupInfo info = Dropped.info;
                    info.weaponMods[1] = value;

                    Dropped.Networkinfo = info;
                }
            }
        }

        public int MiscAttachment
        {
            get => Dropped == null ? Inventory.items[Index].modOther : Dropped.info.weaponMods[2];
            set
            {
                if (Dropped == null)
                {
                    Inventory.SyncItemInfo info = Inventory.items[Index];
                    info.modOther = value;

                    Inventory.items[Index] = info;
                }
                else
                {
                    Pickup.PickupInfo info = Dropped.info;
                    info.weaponMods[2] = value;

                    Dropped.Networkinfo = info;
                }
            }
        }

        /// <summary>
        /// The player (null if none) that is holding this item.
        /// </summary>
        public GameObject PlayerObject { get; internal set; }
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
        public Pickup Dropped { get; internal set; }

        internal float durability;
        /// <summary>
        /// <para>ALWAYS USE THIS FOR DURABILITY.</para>
        /// <para>Because the ID system is durability based, this must be handled by this property's custom logic.</para>
        /// <para>Durability is used for if a micro has a charge (1), how many rounds are in a gun (# of rounds), and how much battery there is in a radio (battery % as whole number).</para>
        /// </summary>
        public float Durability
        {
            get => Dropped == null ? Inventory.items[Index].durability : durability;
            set
            {
                if (Dropped == null)
                {
                    Inventory.SyncItemInfo item = Inventory.items[Index];
                    item.durability = value;
                    Inventory.items[Index] = item;
                }
                else
                {
                    durability = value;
                }
            }
        }

        protected CustomItem(Items manager, WorldCustomItems items)
        {
            this.manager = manager;
            this.items = items;
        }

        public void SetPlayer(GameObject target)
        {
            if (target == null)
            {
                if (PlayerObject != null)
                {
                    durability = Durability;
                    Vector3 dropPos = PlayerObject.transform.position;
                    Quaternion dropRot = PlayerObject.transform.rotation;

                    Inventory.items.RemoveAt(Index);

                    PlayerObject = null;
                    Inventory = null;
                    Index = -1;

                    Dropped = manager.hostInventory.SetPickup((int)Type, UniqueId, dropPos, dropRot, Sight,
                        Barrel, MiscAttachment).GetComponent<Pickup>();
                }
            }
            else
            {
                if (PlayerObject == null)
                {
                    manager.ReinsertItem(Inventory, Inventory.items.Count, Dropped.info);
                    Dropped.Delete();
                }
            }
        }

        public virtual void OnInitialize() { }

        public bool Deleted { get; private set; }
        public void Delete()
        {
            Deleted = true;

            if (!Unhooked)
            {
                Unhook();
            }

            if (Dropped == null)
            {
                Inventory.items.RemoveAt(Index);
                Items.CorrectItemIndexes(manager.GetCustomItems(Inventory.gameObject), Index);
            }
            else
            {
                Dropped.Delete();
            }

            OnDelete();
        }
        public virtual void OnDelete() { }

        /// <summary>
        /// The status of whether or not this item has been deleted from the world.
        /// </summary>
        public bool Unhooked { get; private set; }
        public void Unhook()
        {
            Unhooked = true;

            items.Remove(UniqueId);

            OnUnhooked();
        }
        public virtual void OnUnhooked() { }

        public virtual bool OnDrop()
        {
            return true;
        }
        public virtual bool OnDeathDrop(GameObject attacker, DamageType damage)
        {
            return true;
        }
        public virtual bool OnPickup()
        {
            return true;
        }

        public virtual void On914(KnobSetting knob, Vector3 output, bool heldItem) { }
        public virtual void OnRadioSwitch(RadioStatus status) { }
        public virtual void OnMedkitUse() { }
        public virtual void OnShoot(GameObject target, ref float damage) { }

        internal void ApplyPickup()
        {
            durability = Dropped.info.durability;

            Pickup.PickupInfo info = Dropped.info;
            info.durability = UniqueId;
            Dropped.Networkinfo = info;
        }

        internal void ApplyInventory()
        {
            Inventory.SyncItemInfo info = Inventory.items[Index];
            info.durability = durability;
            Inventory.items[Index] = info;
        }
    }
}
