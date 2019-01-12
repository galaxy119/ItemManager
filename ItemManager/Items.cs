using UnityEngine;
using Smod2.API;

using ItemManager.Recipes;
using ItemManager.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;
using Object = UnityEngine.Object;

namespace ItemManager
{
    public class Items
    {
        internal List<WorldCustomItems> items;
        internal List<Base914Recipe> recipes;

        internal Inventory hostInventory;
        internal Scp914 scp;

        /*internal Dictionary<int, ICustomItemHandler> registeredItems = new Dictionary<int, ICustomItemHandler>();
        internal Dictionary<int, ICustomWeaponHandler> registeredWeapons = new Dictionary<int, ICustomWeaponHandler>();

        internal Dictionary<float, CustomItem> customItems = new Dictionary<float, CustomItem>();
        internal Dictionary<int, Dictionary<int, int>> customWeaponAmmo = new Dictionary<int, Dictionary<int, int>>();

        internal Dictionary<float, bool> readyForDoubleDrop = new Dictionary<float, bool>();
        internal Dictionary<float, int> doubleDropTimers = new Dictionary<float, int>();*/

        public const float DefaultDurability = -4.656647E+11f;
        public IReadOnlyList<WorldCustomItems> Registered => items;

        public Items()
        {
            recipes = new List<Base914Recipe>();
            items = new List<WorldCustomItems>();
        }

        internal void RefreshMap()
        {
            hostInventory = GameObject.Find("Host").GetComponent<Inventory>();
            scp = Object.FindObjectOfType<Scp914>();
        }

        /// <summary>
        /// Adds a 914 recipe to the recipe list.
        /// </summary>
        /// <param name="recipe">Recipe to register.</param>
        public void AddRecipe(Base914Recipe recipe)
        {
            recipes.Add(recipe);
        }

        /// <summary>
        /// Removes a 914 recipe from the recipe list.
        /// </summary>
        /// <param name="recipe">Recipe to unregister.</param>
        public void RemoveRecipe(Base914Recipe recipe)
        {
            recipes.Remove(recipe);
        }

        /// <summary>
        /// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
        /// </summary>
        /// <param name="player">PlayerObject of the held item to retrieve.</param>
        public CustomItem HeldCustomItem(Player player)
        {
            return HeldCustomItem((GameObject)player.GetGameObject());
        }

        /// <summary>
        /// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
        /// </summary>
        /// <param name="player">PlayerObject of the held item to retrieve.</param>
        internal CustomItemData InternalHeldCustomItem(Player player)
        {
            return InternalHeldCustomItem((GameObject)player.GetGameObject());
        }

        /// <summary>
        /// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
        /// </summary>
        /// <param name="player">PlayerObject of the held item to retrieve.</param>
        public CustomItem HeldCustomItem(GameObject player)
        {
            int heldIndex = player.GetComponent<Inventory>().GetItemIndex();

            return heldIndex == -1 ? null : FindCustomItem(player, heldIndex);
        }

        /// <summary>
        /// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
        /// </summary>
        /// <param name="player">PlayerObject of the held item to retrieve.</param>
        internal CustomItemData InternalHeldCustomItem(GameObject player)
        {
            int heldIndex = player.GetComponent<Inventory>().GetItemIndex();

            return heldIndex == -1 ? null : InternalFindCustomItem(player, heldIndex);
        }

        /// <summary>
        /// Finds and returns all custom items within a player's inventory.
        /// </summary>
        /// <param name="player">PlayerObject that should be checked for custom items.</param>
        public CustomItem[] GetCustomItems(Player player)
        {
            return GetCustomItems((GameObject) player.GetGameObject());
        }

        /// <summary>
        /// Finds and returns all custom items within a player's inventory.
        /// </summary>
        /// <param name="player">PlayerObject that should be checked for custom items.</param>
        public CustomItem[] GetCustomItems(GameObject player)
        {
            return items.SelectMany(x => x.Instances.Where(x => x.PlayerObject == player)).ToArray();
        }

        /// <summary>
        /// Checks if an item is an instance of a custom item.
        /// </summary>
        /// <param name="pickup">Item to check if it is a custom item.</param>
        public bool IsCustomItem(Pickup pickup)
        {
            return items.Any(x => x.Instances.Any(y => y.UniqueId == pickup.info.durability));
        }

        /// <summary>
        /// Checks if an item is an instance of a custom item.
        /// </summary>
        /// <param name="player">PlayerObject that is holding the item to check.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public bool IsCustomItem(Player player, int index)
        {
            return IsCustomItem((GameObject)player.GetGameObject(), index);
        }

        /// <summary>
        /// Checks if an item is an instance of a custom item.
        /// </summary>
        /// <param name="player">PlayerObject that is holding the item to check.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public bool IsCustomItem(GameObject player, int index)
        {
            return items.SelectMany(x => x.Instances).Any(x => x.Index == index && x.PlayerObject == player);
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="player">PlayerObject that is holding the custom item.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public CustomItem FindCustomItem(Player player, int index)
        {
            return FindCustomItem((GameObject)player.GetGameObject(), index);
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="player">PlayerObject that is holding the custom item.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public CustomItem FindCustomItem(GameObject player, int index)
        {
            return InternalFindCustomItem(player, index).Item;
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="pickup">The vanilla item.</param>
        internal CustomItemData InternalFindCustomItem(GameObject player, int index)
        {
            foreach (WorldCustomItems type in items)
            {
                CustomItemData data = type.instancePairs.Values.FirstOrDefault(y => y.Item.PlayerObject == player && y.Item.Index == index);
                if (data != null)
                {
                    return data;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="pickup">The vanilla item.</param>
        public CustomItem FindCustomItem(Pickup pickup)
        {
            return InternalFindCustomItem(pickup).Item;
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="pickup">The vanilla item.</param>
        internal CustomItemData InternalFindCustomItem(Pickup pickup)
        {
            return items.Select(x => x.instancePairs).FirstOrDefault(x => x.ContainsKey(pickup.info.durability))?[pickup.info.durability];
        }

        internal static void CorrectItemIndexes(CustomItem[] items, int index)
        {
            foreach (CustomItem item in items.Where(x => x.Index > index))
            {
                item.Index--;
            }
        }

        internal static Inventory.SyncItemInfo ReinsertItem(Inventory inventory, int index, Pickup.PickupInfo info)
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
    }
}
