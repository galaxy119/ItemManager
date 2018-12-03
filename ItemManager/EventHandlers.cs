using System;
using UnityEngine;

using Smod2.EventHandlers;
using Smod2.Events;

using scp4aiur;
using ItemManager.Recipes;
using ItemManager.Events;

using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace ItemManager {
    public class EventHandlers : IEventHandlerPlayerPickupItem, IEventHandlerPlayerDropItem, IEventHandlerSCP914Activate, IEventHandlerRoundStart, IEventHandlerRoundRestart, IEventHandlerPlayerHurt, IEventHandlerShoot {
        public void OnRoundStart(RoundStartEvent ev) {
            Items.scp = Object.FindObjectOfType<Scp914>();
            Items.hostInventory = GameObject.Find("Host").GetComponent<Inventory>();
        }

        public void OnRoundRestart(RoundRestartEvent ev) {
            Items.scp = null;
            Items.hostInventory = null;
        }

        private static void InvokePickupEvent(CustomItem customItem, GameObject player, Inventory inventory, int index, Inventory.SyncItemInfo item) {
            customItem.Player = player;
            customItem.Inventory = inventory;
            customItem.Index = index;
            customItem.Pickup = null;

            if (!customItem.OnPickup()) {
                customItem.Player = null;
                customItem.Inventory = null;
                customItem.Index = -1;
                customItem.Pickup = Items.hostInventory.SetPickup(item.id, customItem.UniqueId, player.transform.position,
                    player.transform.rotation).GetComponent<Pickup>();
            }
        }

        private static void BaseInvokeDropEvent(CustomItem customItem, Inventory inventory, int index,
            Pickup drop, Func<bool> result) {
            customItem.Pickup = drop;

            if (!result()) {
                customItem.Pickup = null;

                ReinsertItem(inventory, index, drop.info);
                drop.Delete();
            }

            customItem.Player = null;
            customItem.Inventory = null;
            customItem.Index = -1;
        }

        private static void InvokeDropEvent(CustomItem customItem, Inventory inventory, int index, Pickup drop) {
            BaseInvokeDropEvent(customItem, inventory, index, drop, customItem.OnDrop);
        }

        private static void InvokeDoubleDropEvent(CustomItem customItem, Inventory inventory, int index, Pickup drop) {
            BaseInvokeDropEvent(customItem, inventory, index, drop, Items.registeredDoubleDrop[customItem.UniqueId].OnDoubleDrop);
        }

        private static Inventory.SyncItemInfo ReinsertItem(Inventory inventory, int index, Pickup.PickupInfo info) {
            Inventory.SyncItemInfo item = new Inventory.SyncItemInfo {
                durability = info.durability,
                id = info.itemId,
                uniq = ++inventory.itemUniq
            };

            inventory.items.Insert(index, item);
            return item;
        }

        public void OnPlayerPickupItem(PlayerPickupItemEvent ev) {
            GameObject player = (GameObject) ev.Player.GetGameObject();
            Inventory inventory = player.GetComponent<Inventory>();

            Timing.NextTick(() => {
                Inventory.SyncItemInfo item = inventory.items.Last();

                if (Items.customItems.ContainsKey(item.durability)) {
                    CustomItem customItem = Items.customItems[item.durability];
                    
                    InvokePickupEvent(customItem, player, inventory, inventory.items.Count - 1, item);
                }
            });
        }

        public void OnPlayerDropItem(PlayerDropItemEvent ev) {
            GameObject player = (GameObject) ev.Player.GetGameObject();
            Inventory inventory = player.GetComponent<Inventory>();

            List<Inventory.SyncItemInfo> preItems = inventory.items.Select(x => x).ToList();
            Pickup[] prePickups = Object.FindObjectsOfType<Pickup>();
            
            Timing.NextTick(() => {
                Pickup[] postPickups = Object.FindObjectsOfType<Pickup>();
                Pickup drop = postPickups.First(x => !prePickups.Contains(x) && x.info.ownerPlayerID == ev.Player.PlayerId);

                Inventory.SyncItemInfo[] postItems = inventory.items.Select(x => x).ToArray();
                int dropIndex = preItems.IndexOf(preItems.First(x => !postItems.Contains(x)));

                CustomItem[] itemsInInventory = Items.customItems.Values.Where(x => x.Inventory == inventory).ToArray();
                CustomItem customItem = Items.customItems.ContainsKey(drop.info.durability) ? 
                    Items.customItems[drop.info.durability] :
                    itemsInInventory.FirstOrDefault(x => x.Index == dropIndex);

                if (customItem != null) {
                    if (Items.registeredDoubleDrop.ContainsKey(customItem.UniqueId)) {
                        if (Items.readyForDoubleDrop[customItem.UniqueId]) {
                            Items.readyForDoubleDrop[customItem.UniqueId] = false;
                            Timing.RemoveTimer(Items.doubleDropTimers[customItem.UniqueId]);

                            InvokeDoubleDropEvent(customItem, inventory, dropIndex, drop);
                        } else {
                            Pickup.PickupInfo info = drop.info;
                            drop.Delete(); //delete dropped item
                            Inventory.SyncItemInfo doubleDropDummy = ReinsertItem(inventory, dropIndex, info); //add item back to inventory

                            Items.readyForDoubleDrop[customItem.UniqueId] = true;

                            Items.doubleDropTimers.Remove(customItem.UniqueId);
                            Items.doubleDropTimers.Add(customItem.UniqueId, Timing.Timer(inaccuracy => {
                                Items.readyForDoubleDrop[customItem.UniqueId] = false;
                                inventory.items.Remove(doubleDropDummy); //remove dummy from inventory
                                drop = Items.hostInventory //create item in world
                                    .SetPickup(info.itemId, info.durability, player.transform.position, player.transform.rotation)
                                    .GetComponent<Pickup>();

                                InvokeDropEvent(customItem, inventory, dropIndex, drop);
                            }, Items.registeredDoubleDrop[customItem.UniqueId].DoubleDropWindow));
                        }
                    }
                    else {
                        InvokeDropEvent(customItem, inventory, dropIndex, drop);
                    }
                }
                
                foreach (CustomItem entry in itemsInInventory.Where(x => x.Index > dropIndex)) {
                    entry.Index--;
                }
            });
        }

        public void OnSCP914Activate(SCP914ActivateEvent ev) {
            foreach (Pickup pickup in ev.Inputs.Cast<Collider>().Select(x => x.GetComponent<Pickup>()).Where(x => x != null)) {
                if (Items.customItems.ContainsKey(pickup.info.durability)) {
                    CustomItem item = Items.customItems[pickup.info.durability];
                    
                    Base914Recipe recipe = Items.recipes.Where(x => x.IsMatch(ev.KnobSetting, item))
                        .OrderByDescending(x => x.Priority).FirstOrDefault(); //gets highest priority

                    item.Pickup = Items.hostInventory.SetPickup((int)item.ItemType, pickup.info.durability,
                        pickup.info.position + (Items.scp.output_obj.position - Items.scp.intake_obj.position),
                        pickup.info.rotation).GetComponent<Pickup>();
                    pickup.Delete();

                    if (recipe != null) { //recipe has higher priority
                        recipe.Run(item);
                    }
                    else {
                        item.On914(ev.KnobSetting, item.Pickup.transform.position);
                    }
                }
                else {
                    Pickup.PickupInfo info = pickup.info;
                    pickup.Delete();
                    Pickup output = Items.hostInventory.SetPickup(info.itemId, 
                        info.durability, 
                        info.position + (Items.scp.output_obj.position - Items.scp.intake_obj.position), 
                        info.rotation).GetComponent<Pickup>();

                    Items.recipes.Where(x => x.IsMatch(ev.KnobSetting, output))
                        .OrderByDescending(x => x.Priority).FirstOrDefault()
                        ?.Run(output);
                }
            }
        }

        public void OnPlayerHurt(PlayerHurtEvent ev) {
            if (ev.Attacker != null) {
                GameObject player = (GameObject)ev.Attacker.GetGameObject();
                Inventory inventory = player.GetComponent<Inventory>();
                CustomItem item = Items.customItems.FirstOrDefault(x =>
                    x.Value.Inventory == inventory && x.Value.Index == inventory.curItem).Value;

                if (item != null) {
                    IWeapon weapon = Items.customItems.ContainsKey(item.UniqueId)
                        ? Items.registeredWeapons[item.UniqueId]
                        : null;

                    if (weapon != null) {
                        float damage = ev.Damage;
                        weapon.OnHit((GameObject)ev.Player.GetGameObject(), ref damage);
                        ev.Damage = damage;
                    }
                }
            }
        }

        public void OnShoot(PlayerShootEvent ev) {
            if (ev.Target == null && ev.Player != null) {
                GameObject player = (GameObject)ev.Player.GetGameObject();
                Inventory inventory = player.GetComponent<Inventory>();
                CustomItem item = Items.customItems.FirstOrDefault(x =>
                    x.Value.Player == player && x.Value.Index == inventory.GetItemIndex()).Value;

                if (item != null) {
                    IWeapon weapon = Items.customItems.ContainsKey(item.UniqueId)
                        ? Items.registeredWeapons[item.UniqueId]
                        : null;
                    
                    if (weapon != null) {
                        float damage = 0;
                        weapon.OnHit(null, ref damage);
                    }
                }
            }
        }
    }
}
