using ItemManager.Events;
using ItemManager.Utilities;
using scp4aiur;
using Smod2.API;
using UnityEngine;

namespace ItemManager
{
	public abstract class CustomItem
	{
		/// <summary>
		/// The durability of the item in the pickup state, used for ID purposes.
		/// </summary>
		public float UniqueId { get; internal set; }

		/// <summary>
		/// The handler that handles all the additional data for this type.
		/// </summary>
		public ICustomItemHandler Handler { get; private set; }

		/// <summary>
		/// The ID of the item to impersonate.
		/// </summary>
		public ItemType VanillaType
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

		private void Init(ICustomItemHandler handler)
		{
			Handler = handler;
			UniqueId = Items.ids.NewId();
		}

		private void RegisterEvents()
		{
			if (this is IDoubleDroppable)
			{
				Items.doubleDropTimers.Add(UniqueId, int.MinValue);
				Items.readyForDoubleDrop.Add(UniqueId, false);
			}

			Items.customItems.Add(UniqueId, this);
		}

		internal void SetData(ICustomItemHandler handler, Vector3 pos, Quaternion rot)
		{
			Init(handler);

			Dropped = Items.hostInventory.SetPickup((int)handler.DefaultType, UniqueId, pos, rot, 0, 0, 0).GetComponent<Pickup>();
			Index = -1;
			Durability = 0;

			RegisterEvents();
		}

		internal void SetData(ICustomItemHandler handler, Pickup pickup)
		{
			Init(handler);

			Dropped = pickup;
			Index = -1;
			Durability = 0;

			VanillaType = handler.DefaultType;

			RegisterEvents();
		}

		internal void SetData(ICustomItemHandler handler, Inventory inventory)
		{
			Init(handler);

			PlayerObject = inventory.gameObject;
			Inventory = inventory;
			Index = Inventory.items.Count;
			inventory.AddNewItem((int)handler.DefaultType, 0);

			RegisterEvents();
		}

		internal void SetData(ICustomItemHandler handler, Inventory inventory, int index)
		{
			Init(handler);

			PlayerObject = inventory.gameObject;
			Inventory = inventory;
			Index = index;

			VanillaType = handler.DefaultType;

			RegisterEvents();
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
				Items.CorrectItemIndexes(Items.GetCustomItems(Inventory.gameObject), Index);
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

			Items.customItems.Remove(UniqueId);

			if (this is IDoubleDroppable)
			{ //if double droppable
				if (Items.doubleDropTimers.ContainsKey(UniqueId))
				{
					Timing.Remove(Items.doubleDropTimers[UniqueId]);
					Items.doubleDropTimers.Remove(UniqueId);
				}

				Items.readyForDoubleDrop.Remove(UniqueId);
			}

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
		public virtual RadioStatus OnRadioSwitch(RadioStatus status)
		{
			return status;
		}
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
