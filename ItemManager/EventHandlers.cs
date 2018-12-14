using System;
using System.Collections.Generic;
using UnityEngine;

using Smod2.EventHandlers;
using Smod2.Events;

using scp4aiur;
using ItemManager.Recipes;
using ItemManager.Events;

using System.Linq;
using Smod2.API;
using Object = UnityEngine.Object;

namespace ItemManager {
    public class EventHandlers : IEventHandlerPlayerPickupItem, IEventHandlerPlayerDropItem, IEventHandlerSCP914Activate, IEventHandlerRoundStart, IEventHandlerRoundRestart, IEventHandlerPlayerHurt, IEventHandlerShoot, IEventHandlerMedkitUse, IEventHandlerPlayerDie {
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
                ReinsertItem(inventory, index, drop.info);
                customItem.Pickup = null;

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

        private static void InvokeDeathDropEvent(CustomItem customItem, Pickup drop, GameObject attacker, DamageType damage) {
            customItem.Pickup = drop;

            if (!customItem.OnDeathDrop(attacker, damage)) {
                customItem.Pickup = null;

                drop.Delete();
                customItem.Delete();
            }

            customItem.Player = null;
            customItem.Inventory = null;
            customItem.Index = -1;
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
            
            GetLostItem(inventory, out Pickup[] prePickups, out int[] preItems);
            
            Timing.NextTick(() => {
                Pickup drop = GetLostItemTick(inventory, prePickups, preItems, out int dropIndex);

                CustomItem customItem = Items.FindCustomItem(player, dropIndex);
                CorrectItemIndexes(Items.GetCustomItems(inventory.gameObject), dropIndex);

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

                    Base914Recipe recipe = Items.recipes.Where(x => x.IsMatch(ev.KnobSetting, info))
                        .OrderByDescending(x => x.Priority).FirstOrDefault();

                    if (recipe != null) {
                        pickup.Delete();
                        
                        recipe.Run(Items.hostInventory.SetPickup(info.itemId,
                            info.durability,
                            info.position + (Items.scp.output_obj.position - Items.scp.intake_obj.position),
                            info.rotation).GetComponent<Pickup>());
                    }
                }
            }
        }

        public void OnPlayerHurt(PlayerHurtEvent ev) {
            CustomItem customItem = ev.Attacker?.HeldCustomItem();

            if (customItem != null) {
                if (Items.registeredWeapons.ContainsKey(customItem.UniqueId)) {
                    float damage = ev.Damage;
                    
                    Items.registeredWeapons[customItem.UniqueId].OnHit((GameObject)ev.Player.GetGameObject(), ref damage);

                    ev.Damage = damage;
                }
            }
        }

        public void OnShoot(PlayerShootEvent ev) {
            if (ev.Target == null && ev.Player != null) {
                CustomItem customItem = ev.Player.HeldCustomItem();
                if (customItem != null &&
                    Items.registeredWeapons.ContainsKey(customItem.UniqueId)) {

                    float damage = 0;
                    Items.registeredWeapons[customItem.UniqueId].OnHit(null, ref damage);
                }
            }
        }

        public void OnMedkitUse(PlayerMedkitUseEvent ev) {
            Inventory inventory = ((GameObject) ev.Player.GetGameObject()).GetComponent<Inventory>();
            GetLostItem(inventory, out Inventory.SyncItemInfo[] preItems);

            Timing.NextTick(() => {
                GetLostItemTick(inventory, preItems, out int index);

                CorrectItemIndexes(ev.Player.GetCustomItems(), index);
            });
        }

        private static void GetLostItem(Inventory inventory, out Inventory.SyncItemInfo[] preItems) {
            preItems = inventory.items.ToArray();
        }

        private static Inventory.SyncItemInfo GetLostItemTick(Inventory inventory, Inventory.SyncItemInfo[] preItems, out int index) {
            int[] postItems = inventory.items.Select(x => x.uniq).ToArray();

            index = postItems.Length;
            for (int i = 0; i < postItems.Length; i++) {
                if (preItems[i].uniq != postItems[i]) {
                    index = i;

                    break;
                }
            }

            return preItems[index];
        }

        private static void GetLostItem(Inventory inventory, out Pickup[] prePickups, out int[] preItems) {
            prePickups = Object.FindObjectsOfType<Pickup>();
            preItems = inventory.items.Select(x => x.uniq).ToArray();
        }

        private static Pickup GetLostItemTick(Inventory inventory, Pickup[] prePickups, int[] preItems, out int index) {
            Pickup[] postPickups = Object.FindObjectsOfType<Pickup>();
            int[] postItems = inventory.items.Select(x => x.uniq).ToArray();

            index = postItems.Length;
            for (int i = 0; i < postItems.Length; i++) {
                if (preItems[i] != postItems[i]) {
                    index = i;

                    break;
                }
            }
            
            return postPickups.Except(prePickups).First();
        }

        private static void CorrectItemIndexes(CustomItem[] items, int index) {
            foreach (CustomItem item in items.Where(x => x.Index > index)) {
                item.Index--;
            }
        }

        public void OnPlayerDie(PlayerDeathEvent ev) {
            List<CustomItem> items = ev.Player.GetCustomItems().ToList();

            if (items.Count > 0) {
                Dictionary<CustomItem, ItemType> itemTypes = items.ToDictionary(x => x, x => x.ItemType);
                Vector3 deathPosition = ((GameObject)ev.Player.GetGameObject()).transform.position;
                Pickup[] prePickups = Object.FindObjectsOfType<Pickup>();

                Timing.NextTick(() => {
                    Pickup[] postPickups = Object.FindObjectsOfType<Pickup>();
                    Pickup[] pickupsThisTick = postPickups.Except(prePickups).ToArray();
                    Pickup[] deathPickups = pickupsThisTick
                        .Where(x => Vector3.Distance(deathPosition, x.transform.position) < 10).ToArray();

                    foreach (Pickup pickup in deathPickups) {
                        CustomItem customItemOfType = itemTypes.FirstOrDefault(x => (int)x.Value == pickup.info.itemId).Key;

                        if (customItemOfType != null) {
                            items.Remove(customItemOfType);

                            InvokeDeathDropEvent(customItemOfType, pickup, (GameObject)ev.Killer.GetGameObject(), ev.DamageTypeVar);
                        }
                    }
                });
            }
        }
    }
}
