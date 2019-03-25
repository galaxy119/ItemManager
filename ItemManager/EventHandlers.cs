using System;
using System.Collections.Generic;
using UnityEngine;

using Smod2.EventHandlers;
using Smod2.Events;

using scp4aiur;
using ItemManager.Recipes;
using ItemManager.Events;

using System.Linq;
using ItemManager.Utilities;
using Smod2.API;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace ItemManager
{
	public class EventHandlers : IEventHandlerRoundStart, IEventHandlerRoundRestart, IEventHandlerPlayerPickupItemLate,
		IEventHandlerPlayerDropItem, IEventHandlerSCP914Activate, IEventHandlerPlayerHurt, IEventHandlerShoot,
		IEventHandlerMedkitUse, IEventHandlerPlayerDie, IEventHandlerRadioSwitch, IEventHandlerSpawn,
		IEventHandlerWaitingForPlayers, IEventHandlerReload
	{
		private readonly ImPlugin plugin;
		private readonly List<CustomItem> justShot;
		private static readonly System.Random getrandom = new System.Random();

		public EventHandlers(ImPlugin plugin)
		{
			this.plugin = plugin;

			justShot = new List<CustomItem>();
		}

		public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
		{
			plugin.RefreshConfig();
		}

		public void OnRoundStart(RoundStartEvent ev)
		{
			Items.scp = Object.FindObjectOfType<Scp914>();
			Items.hostInventory = GameObject.Find("Host").GetComponent<Inventory>();
		}

		public void OnSpawn(PlayerSpawnEvent ev)
		{
			foreach (ICustomWeaponHandler handler in Items.Handlers.Select(x => x.Value as ICustomWeaponHandler).Where(x => x != null))
			{
				if (Items.customWeaponAmmo[handler.PsuedoType].ContainsKey(ev.Player.PlayerId))
				{
					Items.customWeaponAmmo[handler.PsuedoType][ev.Player.PlayerId] = handler.DefaultReserveAmmo;
				}
				else
				{
					Items.customWeaponAmmo[handler.PsuedoType].Add(ev.Player.PlayerId, handler.DefaultReserveAmmo);
				}
			}

			foreach (float id in Items.customItems.Keys.ToArray())
			{
				CustomItem item = Items.customItems[id];
				if (item.PlayerObject == (GameObject) ev.Player.GetGameObject())
				{
					item.Delete();
				}
			}
		}

		public void OnRoundRestart(RoundRestartEvent ev)
		{
			foreach (float uniq in Items.customItems.Keys.ToArray())
			{
				Items.customItems[uniq].Unhook();
				Items.customItems.Remove(uniq);
			}

			foreach (Dictionary<int, int> weaponAmmo in Items.customWeaponAmmo.Values)
			{
				weaponAmmo.Clear();
			}
		}

		private static void InvokePickupEvent(CustomItem customItem, GameObject player, Inventory inventory, int index, Inventory.SyncItemInfo item)
		{
			customItem.PlayerObject = player;
			customItem.Inventory = inventory;
			customItem.Index = index;
			customItem.Dropped = null;

			customItem.ApplyInventory();

			if (!customItem.OnPickup())
			{
				inventory.items.RemoveAt(index);

				customItem.PlayerObject = null;
				customItem.Inventory = null;
				customItem.Index = -1;
				customItem.Dropped = Items.hostInventory.SetPickup(item.id, customItem.UniqueId, player.transform.position,
					player.transform.rotation, item.modSight, item.modBarrel, item.modOther).GetComponent<Pickup>();

				customItem.ApplyPickup();
			}
		}

		private static void BaseInvokeDropEvent(CustomItem customItem, Inventory inventory, int index,
			Pickup drop, Func<bool> result)
		{
			customItem.Dropped = drop;

			customItem.ApplyPickup();

			if (!result())
			{
				ReinsertItem(inventory, index, drop.info);
				customItem.Dropped = null;
				drop.Delete();

				customItem.ApplyInventory();
			}
			else
			{
				customItem.PlayerObject = null;
				customItem.Inventory = null;
				customItem.Index = -1;

				Items.CorrectItemIndexes(Items.GetCustomItems(inventory.gameObject), index);
			}
		}

		private static void InvokeDropEvent(CustomItem customItem, Inventory inventory, int index, Pickup drop)
		{
			BaseInvokeDropEvent(customItem, inventory, index, drop, customItem.OnDrop);
		}

		private static void InvokeDoubleDropEvent(CustomItem customItem, Inventory inventory, int index, Pickup drop)
		{
			BaseInvokeDropEvent(customItem, inventory, index, drop, ((IDoubleDroppable)customItem).OnDoubleDrop);
		}

		private static void InvokeDeathDropEvent(CustomItem customItem, Pickup drop, GameObject attacker, DamageType damage)
		{
			customItem.Dropped = drop;

			customItem.ApplyPickup();

			if (!customItem.OnDeathDrop(attacker, damage))
			{
				customItem.Dropped = null;

				drop.Delete();
				customItem.Unhook();
			}
			else
			{
				customItem.PlayerObject = null;
				customItem.Inventory = null;
				customItem.Index = -1;
			}
		}

		private static Inventory.SyncItemInfo ReinsertItem(Inventory inventory, int index, Pickup.PickupInfo info)
		{
			Inventory.SyncItemInfo item = new Inventory.SyncItemInfo
			{
				durability = info.durability,
				id = info.itemId,
				uniq = ++inventory.itemUniq,
				modSight = info.weaponMods[0],
				modBarrel = info.weaponMods[1],
				modOther = info.weaponMods[2]
			};

			inventory.items.Insert(index, item);
			return item;
		}

		public void OnPlayerPickupItemLate(PlayerPickupItemLateEvent ev)
		{
			GameObject player = (GameObject)ev.Player.GetGameObject();
			Inventory inventory = player.GetComponent<Inventory>();
			
			Inventory.SyncItemInfo? item = inventory.items.Last();

			if (Items.customItems.ContainsKey(item.Value.durability))
			{
				CustomItem customItem = Items.customItems[item.Value.durability];

				InvokePickupEvent(customItem, player, inventory, inventory.items.Count - 1, item.Value);
			}
		}

		public void OnPlayerDropItem(PlayerDropItemEvent ev)
		{
			GameObject player = (GameObject)ev.Player.GetGameObject();
			Inventory inventory = player.GetComponent<Inventory>();

			GetLostItem(inventory, out Pickup[] prePickups, out int[] preItems);

			Timing.Next(() => {
				Pickup drop = GetLostItemTick(inventory, prePickups, preItems, out int dropIndex);

				if (dropIndex == -1)
				{
					return;
				}

				CustomItem customItem = Items.FindCustomItem(player, dropIndex);

				switch (customItem)
				{
					case null:
						Items.CorrectItemIndexes(Items.GetCustomItems(inventory.gameObject), dropIndex);
						return;

					case IDoubleDroppable doubleDroppable when doubleDroppable.DoubleDropWindow > 0:
					{
						if (Items.readyForDoubleDrop[customItem.UniqueId])
						{
							Items.readyForDoubleDrop[customItem.UniqueId] = false;
							Timing.Remove(Items.doubleDropTimers[customItem.UniqueId]);
							
							InvokeDoubleDropEvent(customItem, inventory, dropIndex, drop);
						}
						else
						{
							Pickup.PickupInfo info = drop.info;
							drop.Delete(); //delete dropped item
							Inventory.SyncItemInfo doubleDropDummy = ReinsertItem(inventory, dropIndex, info); //add item back to inventory
							Items.readyForDoubleDrop[customItem.UniqueId] = true;

							Items.doubleDropTimers.Remove(customItem.UniqueId);
							Items.doubleDropTimers.Add(customItem.UniqueId, Timing.In(inaccuracy =>
							{
								Items.readyForDoubleDrop[customItem.UniqueId] = false;
								inventory.items.Remove(doubleDropDummy); //remove dummy from inventory
								drop = Items.hostInventory //create item in world
									.SetPickup(info.itemId, info.durability, player.transform.position, player.transform.rotation, info.weaponMods[0], info.weaponMods[1], info.weaponMods[2])
									.GetComponent<Pickup>();
								
								InvokeDropEvent(customItem, inventory, dropIndex, drop);
							}, doubleDroppable.DoubleDropWindow));
						}

						break;
					}

					default:
						InvokeDropEvent(customItem, inventory, dropIndex, drop);
						break;
				}
			});
		}

		public void OnSCP914Activate(SCP914ActivateEvent ev)
		{
			Collider[] colliders = ev.Inputs.Cast<Collider>().ToArray();

			foreach (Pickup pickup in colliders.Select(x => x.GetComponent<Pickup>()).Where(x => x != null))
			{
				if (Items.customItems.ContainsKey(pickup.info.durability))
				{
					CustomItem item = Items.customItems[pickup.info.durability];

					Base914Recipe recipe = Items.Recipes.Where(x => x.IsMatch(ev.KnobSetting, item, false))
						.OrderByDescending(x => x.Priority).FirstOrDefault(); //gets highest priority

					item.Dropped = Items.hostInventory.SetPickup((int)item.VanillaType, pickup.info.durability,
						pickup.info.position + (Items.scp.output_obj.position - Items.scp.intake_obj.position),
						pickup.info.rotation, item.Sight, item.Barrel, item.MiscAttachment).GetComponent<Pickup>();
					pickup.Delete();

					if (recipe != null)
					{ //recipe has higher priority
						recipe.Run(item, false);
					}
					else
					{
						item.On914(ev.KnobSetting, item.Dropped.transform.position, false);
					}
				}
				else
				{
					Pickup.PickupInfo info = pickup.info;

					Base914Recipe recipe = Items.Recipes.Where(x => x.IsMatch(ev.KnobSetting, info))
						.OrderByDescending(x => x.Priority).FirstOrDefault();

					if (recipe != null)
					{
						pickup.Delete();

						recipe.Run(Items.hostInventory.SetPickup(info.itemId,
							info.durability,
							info.position + (Items.scp.output_obj.position - Items.scp.intake_obj.position),
							info.rotation, info.weaponMods[0], info.weaponMods[1], info.weaponMods[2]).GetComponent<Pickup>());
					}
				}
			}
			object[] inputs = ev.Inputs;
			Scp914 objectOfType = Object.FindObjectOfType<Scp914>();
			if ((Object)objectOfType == (Object)null)
			{
				this.plugin.Error("Couldn't find SCP-914");
				return;
			}
			if (plugin.HeldItems != HeldSetting.None)
			{
				foreach (Collider collider in inputs)
				{
					Pickup component1 = collider.GetComponent<Pickup>();
					if ((Object)component1 == (Object)null)
					{
						NicknameSync component2 = collider.GetComponent<NicknameSync>();
						CharacterClassManager component3 = collider.GetComponent<CharacterClassManager>();
						PlyMovementSync component4 = collider.GetComponent<PlyMovementSync>();
						PlayerStats component5 = collider.GetComponent<PlayerStats>();
						if ((UnityEngine.Object)component2 != (UnityEngine.Object)null && (UnityEngine.Object)component3 != (UnityEngine.Object)null && ((UnityEngine.Object)component4 != (UnityEngine.Object)null && (UnityEngine.Object)component5 != (UnityEngine.Object)null) && (UnityEngine.Object)collider.gameObject != (UnityEngine.Object)null)
						{
							GameObject gameObject = collider.gameObject;
							Player player = new ServerMod2.API.SmodPlayer(gameObject);
							if (player.TeamRole.Team != Smod2.API.Team.SCP && player.TeamRole.Team != Smod2.API.Team.NONE && player.TeamRole.Team != Smod2.API.Team.SPECTATOR)
							{
								if (plugin.currentonly)
								{
									Smod2.API.Item item = player.GetCurrentItem();
									doRecipe(item, objectOfType, player, ev.KnobSetting);
								}
								else
								{
									foreach (Smod2.API.Item item in player.GetInventory())
									{
										doRecipe(item, objectOfType, player, ev.KnobSetting);
									}
								}
							}
						}
					}
				}
			}
		}

		public void doRecipe(Smod2.API.Item item, Scp914 objectOfType, Smod2.API.Player player, Smod2.API.KnobSetting knobSetting)
		{
			sbyte outputitem = -2;
			try
			{
				outputitem = (sbyte)(objectOfType.recipes[(byte)item.ItemType].outputs[(byte)knobSetting].outputs[getrandom.Next(0, objectOfType.recipes[(byte)item.ItemType].outputs[(byte)knobSetting].outputs.Count)]);
			}
			catch (System.Exception)
			{
				this.plugin.Error("Recipe for " + item.ItemType + "does not exist!  Ask the game devs to add a recipe for it!");
			}
			if (outputitem != -2)
			{
				item.Remove();
				this.plugin.Debug(item.ItemType + " ==> " + (ItemType)outputitem);
			}
			if (outputitem >= 0)
			{
				player.GiveItem((ItemType)outputitem);
			}
		}		

		public void OnPlayerHurt(PlayerHurtEvent ev)
		{
			if (ev.Attacker.PlayerId != ev.Player.PlayerId)
			{
				CustomItem customItem = ev.Attacker?.HeldCustomItem();

				if (customItem != null && !justShot.Contains(customItem))
				{
					justShot.Add(customItem);

					float damage = ev.Damage;
					customItem.OnShoot((GameObject)ev.Player.GetGameObject(), ref damage);
					ev.Damage = damage;

					justShot.Remove(customItem);
				}
			}
		}

		public void OnShoot(PlayerShootEvent ev)
		{
			if (ev.Target != null)
			{
				return;
			}

			CustomItem customItem = ev.Player?.HeldCustomItem();

			if (customItem != null)
			{
				float damage = 0;
				customItem.OnShoot(null, ref damage);
			}
		}

		public void OnMedkitUse(PlayerMedkitUseEvent ev)
		{
			GameObject player = (GameObject)ev.Player.GetGameObject();
			Inventory inventory = player.GetComponent<Inventory>();
			GetLostItem(inventory, out Inventory.SyncItemInfo[] preItems);

			Timing.Next(() => {
				GetLostItemTick(inventory, preItems, out int index);

				if (index == -1)
				{
					return;
				}

				CustomItem item = Items.FindCustomItem(player, index);
				Items.CorrectItemIndexes(ev.Player.GetCustomItems(), index);

				item?.OnMedkitUse();
			});
		}

		private static void GetLostItem(Inventory inventory, out Inventory.SyncItemInfo[] preItems)
		{
			preItems = inventory.items.ToArray();
		}

		private static Inventory.SyncItemInfo GetLostItemTick(Inventory inventory, Inventory.SyncItemInfo[] preItems, out int index)
		{
			if (preItems.Length == inventory.items.Count)
			{
				index = -1;
				return default(Inventory.SyncItemInfo);
			}

			int[] postItems = inventory.items.Select(x => x.uniq).ToArray();

			index = postItems.Length;
			for (int i = 0; i < postItems.Length; i++)
			{
				if (preItems[i].uniq != postItems[i])
				{
					index = i;

					break;
				}
			}

			return preItems[index];
		}

		private static void GetLostItem(Inventory inventory, out Pickup[] prePickups, out int[] preItems)
		{
			prePickups = Object.FindObjectsOfType<Pickup>();
			preItems = inventory.items.Select(x => x.uniq).ToArray();
		}

		private static Pickup GetLostItemTick(Inventory inventory, Pickup[] prePickups, int[] preItems, out int index)
		{
			Pickup[] postPickups = Object.FindObjectsOfType<Pickup>();

			if (postPickups.Length == prePickups.Length)
			{
				index = -1;
				return null;
			}

			Pickup postPickup = postPickups.Length - prePickups.Length > 1 ? postPickups.Except(prePickups).OrderBy(x => Vector3.Distance(inventory.transform.position, x.transform.position)).First() : postPickups[0];

			int[] postItems = inventory.items.Select(x => x.uniq).ToArray();

			index = postItems.Length;
			for (int i = 0; i < postItems.Length; i++)
			{
				if (preItems[i] != postItems[i])
				{
					index = i;

					break;
				}
			}

			return postPickup;
		}

		public void OnPlayerDie(PlayerDeathEvent ev)
		{
			CustomItem[] items = ev.Player.GetCustomItems();

			if (items.Length > 0)
			{
				Dictionary<CustomItem, ItemType> itemTypes = items.ToDictionary(x => x, x => x.VanillaType);
				Vector3 deathPosition = ((GameObject)ev.Player.GetGameObject()).transform.position;
				Pickup[] prePickups = Object.FindObjectsOfType<Pickup>();

				Timing.Next(() => {
					Pickup[] postPickups = Object.FindObjectsOfType<Pickup>();
					Pickup[] deathPickups = postPickups
						.Except(prePickups)
						.Where(x => Vector3.Distance(deathPosition, x.transform.position) < 10).ToArray();

					foreach (Pickup pickup in deathPickups)
					{
						CustomItem customItemOfType = itemTypes.FirstOrDefault(x => (int)x.Value == pickup.info.itemId).Key;

						if (customItemOfType != null)
						{
							itemTypes.Remove(customItemOfType);

							InvokeDeathDropEvent(customItemOfType, pickup, (GameObject)ev.Killer.GetGameObject(), ev.DamageTypeVar);
						}
					}
				});
			}
		}

		public void OnPlayerRadioSwitch(PlayerRadioSwitchEvent ev)
		{
			if (ev.Player.HeldCustomItem() is CustomItem item)
			{
				ev.ChangeTo = item.OnRadioSwitch(ev.ChangeTo);
			}
		}

		public void OnReload(PlayerReloadEvent ev)
		{
			if (ev.Player.HeldCustomItem() is CustomWeapon weapon)
			{
				ev.AmmoRemoved = 0;
				weapon.Reload();
				ev.ClipAmmoCountAfterReload = weapon.MagazineAmmo;
			}
		}
	}
}
