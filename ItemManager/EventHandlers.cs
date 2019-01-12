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
using RemoteAdmin;
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
        private readonly List<CustomItem> waitingForShot;

        public EventHandlers(ImPlugin plugin)
        {
            this.plugin = plugin;

            waitingForShot = new List<CustomItem>();
        }

        public void OnWaitingForPlayers(WaitingForPlayersEvent ev)
        {
            plugin.RefreshConfig();
        }

        public void OnRoundStart(RoundStartEvent ev)
        {
            plugin.Manager.RefreshMap();
        }

        public void OnSpawn(PlayerSpawnEvent ev)
        {
            foreach (WorldCustomWeapons weapons in plugin.Manager.items.OfType<WorldCustomWeapons>())
            {
                if (weapons.AmmoReserves.ContainsKey(ev.Player.PlayerId))
                {
                    plugin.Manager.customWeaponAmmo[id][ev.Player.PlayerId] = plugin.Manager.registeredWeapons[id].DefaultReserveAmmo;
                }
                else
                {
                    weapons.AmmoReserves.Add(ev.Player.PlayerId, weapons.DefaultAmmo));
                }
            }
        }

        public void OnRoundRestart(RoundRestartEvent ev)
        {
            foreach (float uniq in plugin.Manager.customItems.Keys.ToArray())
            {
                plugin.Manager.customItems[uniq].Unhook();
                plugin.Manager.customItems.Remove(uniq);
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
                customItem.Dropped = plugin.Manager.hostInventory.SetPickup(item.id, customItem.UniqueId, player.transform.position,
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

            Timing.Next(() =>
            {
                Inventory.SyncItemInfo? item = null;
                try
                {
                    item = inventory.items.Last();
                }
                catch { }

                if (item != null && plugin.Manager.customItems.ContainsKey(item.Value.durability))
                {
                    CustomItem customItem = plugin.Manager.customItems[item.Value.durability];

                    InvokePickupEvent(customItem, player, inventory, inventory.items.Count - 1, item.Value);
                }
            });
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

                CustomItem customItem = plugin.Manager.FindCustomItem(player, dropIndex);
                plugin.Manager.CorrectItemIndexes(plugin.Manager.GetCustomItems(inventory.gameObject), dropIndex);

                switch (customItem) {
                    case null:
                        return;

                    case IDoubleDroppable doubleDroppable when doubleDroppable.DoubleDropWindow > 0:
                    {
                        if (plugin.Manager.readyForDoubleDrop[customItem.UniqueId])
                        {
                            plugin.Manager.readyForDoubleDrop[customItem.UniqueId] = false;
                            Timing.Remove(plugin.Manager.doubleDropTimers[customItem.UniqueId]);

                            InvokeDoubleDropEvent(customItem, inventory, dropIndex, drop);
                        }
                        else
                        {
                            Pickup.PickupInfo info = drop.info;
                            drop.Delete(); //delete dropped item
                            Inventory.SyncItemInfo doubleDropDummy = ReinsertItem(inventory, dropIndex, info); //add item back to inventory
                            plugin.Manager.readyForDoubleDrop[customItem.UniqueId] = true;

                            plugin.Manager.doubleDropTimers.Remove(customItem.UniqueId);
                            plugin.Manager.doubleDropTimers.Add(customItem.UniqueId, Timing.In(inaccuracy => 
                            {
                                plugin.Manager.readyForDoubleDrop[customItem.UniqueId] = false;
                                inventory.items.Remove(doubleDropDummy); //remove dummy from inventory
                                drop = plugin.Manager.hostInventory //create item in world
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
                if (plugin.Manager.customItems.ContainsKey(pickup.info.durability))
                {
                    CustomItem item = plugin.Manager.customItems[pickup.info.durability];

                    Base914Recipe recipe = plugin.Manager.recipes.Where(x => x.IsMatch(ev.KnobSetting, item, false))
                        .OrderByDescending(x => x.Priority).FirstOrDefault(); //gets highest priority

                    item.Dropped = plugin.Manager.hostInventory.SetPickup((int)item.Type, pickup.info.durability,
                        pickup.info.position + (plugin.Manager.scp.output_obj.position - plugin.Manager.scp.intake_obj.position),
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

                    Base914Recipe recipe = plugin.Manager.recipes.Where(x => x.IsMatch(ev.KnobSetting, info))
                        .OrderByDescending(x => x.Priority).FirstOrDefault();

                    if (recipe != null)
                    {
                        pickup.Delete();

                        recipe.Run(plugin.Manager.hostInventory.SetPickup(info.itemId,
                            info.durability,
                            info.position + (plugin.Manager.scp.output_obj.position - plugin.Manager.scp.intake_obj.position),
                            info.rotation, info.weaponMods[0], info.weaponMods[1], info.weaponMods[2]).GetComponent<Pickup>());
                    }
                }
            }

            if (plugin.heldItems > 0)
            {
                foreach (Inventory inventory in colliders.Select(x => x.GetComponent<Inventory>()).Where(x => x != null))
                {
                    for (int i = 0; i < inventory.items.Count; i++)
                    {
                        CustomItem item = plugin.Manager.FindCustomItem(inventory.gameObject, i);

                        if (item == null)
                        {
                            if ((plugin.heldItems & HeldSetting.Custom) == HeldSetting.Custom)
                            {
                                Base914Recipe recipe = plugin.Manager.recipes.Where(x => x.IsMatch(ev.KnobSetting, inventory, i))
                                    .OrderByDescending(x => x.Priority).FirstOrDefault();

                                if (recipe != null)
                                {
                                    recipe.Run(inventory, i);
                                }
                                else
                                {
                                    byte itemId = (byte)inventory.items[i].id;
                                    byte knobId = (byte)ev.KnobSetting;
                                    sbyte outputType = (sbyte)plugin.Manager.scp.recipes[itemId].outputs[knobId].outputs[Random.Range(0, plugin.Manager.scp.recipes[itemId].outputs[knobId].outputs.Count)];

                                    if (outputType > 0)
                                    {
                                        inventory.items[i] = new Inventory.SyncItemInfo
                                        {
                                            id = outputType,
                                            uniq = inventory.items[i].uniq
                                        };
                                    }
                                    else
                                    {
                                        plugin.Manager.CorrectItemIndexes(plugin.Manager.GetCustomItems(inventory.gameObject), i);
                                        inventory.items.RemoveAt(i);
                                    }
                                }
                            }
                        }
                        else
                        {
                            if ((plugin.heldItems & HeldSetting.Normal) == HeldSetting.Normal)
                            {
                                Base914Recipe recipe = plugin.Manager.recipes.Where(x => x.IsMatch(ev.KnobSetting, item, true))
                                    .OrderByDescending(x => x.Priority).FirstOrDefault(); //gets highest priority

                                if (recipe != null)
                                {
                                    recipe.Run(item, true);
                                }
                                else
                                {
                                    item.On914(ev.KnobSetting, item.PlayerObject.transform.position + (plugin.Manager.scp.output_obj.position - plugin.Manager.scp.intake_obj.position), true);
                                }
                            }
                        }
                    }
                }
            }
        }

        public void OnPlayerHurt(PlayerHurtEvent ev)
        {
            if (ev.Attacker != null && ev.Attacker.PlayerId != ev.Player.PlayerId)
            {
                CustomItem customItem = plugin.Manager.HeldCustomItem(ev.Attacker);

                if (customItem != null)
                {
                    waitingForShot.Add(customItem);

                    Timing.Next(() =>
                    {
                        if (waitingForShot.Contains(customItem))
                        {
                            waitingForShot.Remove(customItem);
                            return;
                        }

                        customItem.justShot = true;

                        float damage = ev.Damage;
                        customItem.OnShoot((GameObject)ev.Player.GetGameObject(), ref damage);
                        GameObject target = (GameObject) ev.Player.GetGameObject();
                        target.GetComponent<PlayerStats>().HurtPlayer(new PlayerStats.HitInfo(
                            damage - ev.Damage - 1,
                            customItem.PlayerObject.GetComponent<NicknameSync>().myNick + " (" +
                            customItem.PlayerObject.GetComponent<CharacterClassManager>().SteamId + ")",
                            DamageTypes.FromIndex((int) ev.DamageType),
                            customItem.PlayerObject.GetComponent<QueryProcessor>().PlayerId
                        ), target);

                        customItem.justShot = false;
                    });
                }
            }
        }

        public void OnShoot(PlayerShootEvent ev)
        {
            CustomItem customItem = ev.Player?.HeldCustomItem();

            if (customItem != null)
            {
                if (waitingForShot.Contains(customItem))
                {
                    waitingForShot.Remove(customItem);
                }
                else
                {
                    float damage = 0;
                    customItem.OnShoot(null, ref damage);
                }
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

                CustomItem item = plugin.Manager.FindCustomItem(player, index);
                plugin.Manager.CorrectItemIndexes(ev.Player.GetCustomItems(), index);

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
                Dictionary<CustomItem, ItemType> itemTypes = items.ToDictionary(x => x, x => x.Type);
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
            foreach (CustomItem customItem in ev.Player.GetCustomItems())
            {
                customItem.OnRadioSwitch(ev.ChangeTo);
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
