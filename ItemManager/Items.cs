using UnityEngine;
using ItemManager.Recipes;
using ItemManager.Utilities;
using ItemManager.Events;

using System;
using System.Collections.Generic;
using System.Linq;
using Smod2.API;

namespace ItemManager {
    public static class Items {
        internal static Inventory hostInventory;
        internal static Scp914 scp;
        internal static FloatIdManager ids = new FloatIdManager();

        internal static Dictionary<float, CustomItem> customItems = new Dictionary<float, CustomItem>();
        internal static Dictionary<int, CustomItemHandler> registeredItems = new Dictionary<int, CustomItemHandler>();

        internal static Dictionary<float, IDoubleDroppable> registeredDoubleDrop = new Dictionary<float, IDoubleDroppable>();
        internal static Dictionary<float, bool> readyForDoubleDrop = new Dictionary<float, bool>();
        internal static Dictionary<float, int> doubleDropTimers = new Dictionary<float, int>();

        internal static Dictionary<float, IWeapon> registeredWeapons = new Dictionary<float, IWeapon>();

        internal static List<Base914Recipe> recipes = new List<Base914Recipe>();

        public const float DefaultDurability = -4.656647E+11f;

        /// <summary>
        /// Adds a 914 recipe to the recipe list.
        /// </summary>
        /// <param name="recipe">Recipe to register.</param>
        public static void AddRecipe(Base914Recipe recipe) {
            recipes.Add(recipe);
        }

        /// <summary>
        /// Removes a 914 recipe from the recipe list.
        /// </summary>
        /// <param name="recipe">Recipe to unregister.</param>
        public static void RemoveRecipe(Base914Recipe recipe) {
            recipes.Remove(recipe);
        }

        /// <summary>
        /// Registers a custom item to an ID.
        /// </summary>
        /// <typeparam name="TItem">The type (which inherits CustomItem) to register.</typeparam>
        /// <param name="id">The ID to register the type to.</param>
        public static void AddItem<TItem>(int id) where TItem : CustomItem, new() {
            registeredItems.Add(id, new CustomItemHandler<TItem>(id));
        }

        /// <summary>
        /// Unregisters a custom item from an ID.
        /// </summary>
        /// <param name="id">ID of the registered item to remove.</param>
        public static bool RemoveItem(int id) {
            if (registeredItems.Remove(id)) {
                foreach (float uniqId in customItems.Where(x => x.Value.PsuedoType == id).Select(x => x.Key)) {
                    customItems.Remove(uniqId);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Creates a new custom item pickup in the world.
        /// </summary>
        /// <param name="id">ID of the registered custom item.</param>
        /// <param name="position">The position of the pickup.</param>
        /// <param name="rotation">The rotation of the pickup.</param>
        /// <returns></returns>
        public static CustomItem CreateItem(int id, Vector3 position, Quaternion rotation) {
            if (!registeredItems.ContainsKey(id)) {
                throw new ArgumentOutOfRangeException(nameof(id), "Psuedo ID is not registered to a custom item.");
            }

            CustomItem creation = registeredItems[id].Create(position, rotation);
            customItems.Add(creation.UniqueId, creation);

            return creation;
        }

        /// <summary>
        /// Creates and registers a custom item.
        /// </summary>
        /// <param name="id">Pseudo ID of the custom item.</param>
        /// <param name="inventory">Inventory of the player holding the item.</param>
        /// <param name="index">Index at which the item is being held at.</param>
        public static CustomItem ConvertItem(int id, Inventory inventory, int index) {
            if (!registeredItems.ContainsKey(id)) {
                throw new ArgumentOutOfRangeException(nameof(id), "Psuedo ID is not registered to a custom item.");
            }

            CustomItem creation = registeredItems[id].Create(inventory, index);
            customItems.Add(creation.UniqueId, creation);

            return creation;
        }

        /// <summary>
        /// Creates and registers a custom item.
        /// </summary>
        /// <param name="id">Psuedo ID of the custom item.</param>
        /// <param name="pickup">Item on the ground that should be registered.</param>
        public static CustomItem ConvertItem(int id, Pickup pickup) {
            if (!registeredItems.ContainsKey(id)) {
                throw new ArgumentOutOfRangeException(nameof(id), "Psuedo ID is not registered to a custom item.");
            }

            CustomItem creation = registeredItems[id].Create(pickup);
            customItems.Add(creation.UniqueId, creation);

            return creation;
        }

        /// <summary>
        /// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
        /// </summary>
        /// <param name="player">Player of the held item to retrieve.</param>
        public static CustomItem HeldCustomItem(this Player player) {
            int heldIndex = player.GetCurrentItemIndex();
            if (heldIndex == -1) {
                return null;
            }

            GameObject unityPlayer = (GameObject) player.GetGameObject();

            return customItems.Values.FirstOrDefault(x => x.Index == heldIndex && x.Player == unityPlayer);
        }

        /// <summary>
        /// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
        /// </summary>
        /// <param name="player">Player of the held item to retrieve.</param>
        public static CustomItem HeldCustomItem(GameObject player) {
            int heldIndex = player.GetComponent<Inventory>().GetItemIndex();

            return heldIndex == -1 ? null : customItems.Values.FirstOrDefault(x => x.Index == heldIndex && x.Player == player);
        }

        /// <summary>
        /// Finds and returns all custom items within a player's inventory.
        /// </summary>
        /// <param name="player">Player that should be checked for custom items.</param>
        public static CustomItem[] GetCustomItems(this Player player) {
            GameObject unityPlayer = (GameObject)player.GetGameObject();

            return customItems.Values.Where(x => x.Player == unityPlayer).ToArray();
        }

        /// <summary>
        /// Finds and returns all custom items within a player's inventory.
        /// </summary>
        /// <param name="player">Player that should be checked for custom items.</param>
        public static CustomItem[] GetCustomItems(GameObject player) {
            return customItems.Values.Where(x => x.Player == player).ToArray();
        }

        /// <summary>
        /// Checks if an item is an instance of a custom item.
        /// </summary>
        /// <param name="pickup">Item to check if it is a custom item.</param>
        public static bool IsCustomItem(this Pickup pickup) {
            return customItems.ContainsKey(pickup.info.durability);
        }

        /// <summary>
        /// Checks if an item is an instance of a custom item.
        /// </summary>
        /// <param name="player">Player that is holding the item to check.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public static bool IsCustomItem(this Player player, int index) {
            GameObject unityPlayer = (GameObject)player.GetGameObject();

            return customItems.Values.Any(x => x.Index == index && x.Player == unityPlayer);
        }

        /// <summary>
        /// Checks if an item is an instance of a custom item.
        /// </summary>
        /// <param name="player">Player that is holding the item to check.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public static bool IsCustomItem(GameObject player, int index) {
            return customItems.Values.Any(x => x.Index == index && x.Player == player);
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="player">Player that is holding the custom item.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public static CustomItem FindCustomItem(this Player player, int index) {
            GameObject unityPlayer = (GameObject)player.GetGameObject();

            return customItems.Values.First(x => x.Index == index && x.Player == unityPlayer);
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="player">Player that is holding the custom item.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        public static CustomItem FindCustomItem(this GameObject player, int index) {
            return customItems.Values.First(x => x.Index == index && x.Player == player);
        }

        /// <summary>
        /// Finds a custom item from a vanilla item. 
        /// </summary>
        /// <param name="pickup">The vanilla item.</param>
        public static CustomItem FindCustomItem(this Pickup pickup) {
            return customItems[pickup.info.durability];
        }

        /// <summary>
        /// Tries to find a custom item from a vanilla item, and returns its success and outputs the item.
        /// </summary>
        /// <param name="player">Player that is holding the custom item.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        /// <param name="item">Item that was found. Null if none.</param>
        public static bool TryFindCustomItem(this Player player, int index, out CustomItem item) {
            try {
                item = FindCustomItem(player, index);
                return true;
            }
            catch {
                item = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to find a custom item from a vanilla item, and returns its success and outputs the item.
        /// </summary>
        /// <param name="player">Player that is holding the custom item.</param>
        /// <param name="index">Index of the item in the player's inventory.</param>
        /// <param name="item">Item that was found. Null if none.</param>
        public static bool TryFindCustomItem(GameObject player, int index, out CustomItem item) {
            try {
                item = FindCustomItem(player, index);
                return true;
            } catch {
                item = null;
                return false;
            }
        }

        /// <summary>
        /// Tries to find a custom item from a vanilla item, and returns its success and outputs the item.
        /// </summary>
        /// <param name="pickup">The vanilla item.</param>
        /// <param name="item">Item that was found. Null if none.</param>
        public static bool TryFindCustomItem(this Pickup pickup, out CustomItem item) {
            try {
                item = FindCustomItem(pickup);
                return true;
            } catch {
                item = null;
                return false;
            }
        }
    }
}
