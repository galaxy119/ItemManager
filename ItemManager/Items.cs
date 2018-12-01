using UnityEngine;
using ItemManager.Recipes;
using ItemManager.Utilities;
using ItemManager.Events;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemManager {
    public class Items {
        internal static Inventory hostInventory;
        internal static Scp914 scp;
        internal static FloatIdManager ids;

        internal static Dictionary<float, CustomItem> customItems;
        internal static Dictionary<int, CustomItemHandler> registeredItems;

        internal static Dictionary<float, IDoubleDroppable> registeredDoubleDrop;
        internal static Dictionary<float, bool> readyForDoubleDrop;
        internal static Dictionary<float, int> doubleDropTimers;
        
        internal static Dictionary<float, IWeapon> registeredWeapons;
        
        internal static List<Base914Recipe> recipes;

        public const float DefaultDurability = -4.656647E+11f;

        internal static void Init() {
            ids = new FloatIdManager();

            customItems = new Dictionary<float, CustomItem>();
            registeredItems = new Dictionary<int, CustomItemHandler>();

            registeredDoubleDrop = new Dictionary<float, IDoubleDroppable>();
            readyForDoubleDrop = new Dictionary<float, bool>();
            doubleDropTimers = new Dictionary<float, int>();

            registeredWeapons = new Dictionary<float, IWeapon>();

            recipes = new List<Base914Recipe>();
        }

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
            foreach (float uniqId in customItems.Where(x => x.Value.PsuedoType == id).Select(x => x.Key)) {
                customItems.Remove(uniqId);
            }

            return registeredItems.Remove(id);
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

            return registeredItems[id].Create(position, rotation);
        }

        /// <summary>
        /// Creates and registers a custom item.
        /// </summary>
        /// <param name="id">Pseudo ID of the custom item.</param>
        /// <param name="inventory">Inventory of the player holding the item.</param>
        /// <param name="index">Index at which the item is being held at.</param>
        public static CustomItem ConvertItem(int id, Inventory inventory, int index) {
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
            CustomItem creation = registeredItems[id].Create(pickup);

            customItems.Add(creation.UniqueId, creation);
            return creation;
        }
    }
}
