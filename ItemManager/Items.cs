using UnityEngine;
using Smod2.API;

using ItemManager.Recipes;
using ItemManager.Utilities;

using System;
using System.Collections.Generic;
using System.Linq;

namespace ItemManager
{
	public static class Items
	{
		internal static Inventory hostInventory;
		internal static Scp914 scp;
		internal static FloatIdManager ids = new FloatIdManager();

		private static readonly Dictionary<int, ICustomItemHandler> handlers = new Dictionary<int, ICustomItemHandler>();
		public static IReadOnlyDictionary<int, ICustomItemHandler> Handlers => handlers;

		internal static Dictionary<float, bool> readyForDoubleDrop = new Dictionary<float, bool>();
		internal static Dictionary<float, int> doubleDropTimers = new Dictionary<float, int>();

		internal static Dictionary<int, Dictionary<int, int>> customWeaponAmmo = new Dictionary<int, Dictionary<int, int>>();

		internal static Dictionary<float, CustomItem> customItems = new Dictionary<float, CustomItem>();
		internal static List<CustomItem> itemList = new List<CustomItem>();
		public static IReadOnlyList<CustomItem> AllItems => itemList;

		private static readonly List<Base914Recipe> recipes = new List<Base914Recipe>();
		public static IReadOnlyList<Base914Recipe> Recipes => recipes;

		public static ItemType DefaultDroppedAmmoType { get; internal set; } = ImPlugin.DefaultDropAmmoType;
		public static int DefaultDroppedAmmoCount { get; internal set; } = ImPlugin.DefaultDropAmmoCount;

		public const float DefaultDurability = -4.656647E+11f;

		/// <summary>
		/// Adds a 914 recipe to the recipe list.
		/// </summary>
		/// <param name="recipe">Recipe to register.</param>
		public static void AddRecipe(Base914Recipe recipe)
		{
			recipes.Add(recipe);
		}

		/// <summary>
		/// Removes a 914 recipe from the recipe list.
		/// </summary>
		/// <param name="recipe">Recipe to unregister.</param>
		public static void RemoveRecipe(Base914Recipe recipe)
		{
			recipes.Remove(recipe);
		}

		/// <summary>
		/// Registers a custom item to an ID.
		/// </summary>
		/// <param name="handler">Item to register.</param>
		internal static void RegisterItem(ICustomItemHandler handler)
		{
			if (handler is ICustomWeaponHandler weaponHandler)
			{
				customWeaponAmmo.Add(weaponHandler.PsuedoType, new Dictionary<int, int>());
			}
			
			handlers.Add(handler.PsuedoType, handler);
		}

		/// <summary>
		/// Unregisters a custom item from an ID.
		/// </summary>
		/// <param name="handler">Item to unregister.</param>
		internal static bool UnregisterItem(ICustomItemHandler handler)
		{
			if (handlers.Remove(handler.PsuedoType))
			{
				foreach (float uniqId in customItems.Where(x => x.Value.Handler.PsuedoType == handler.PsuedoType).Select(x => x.Key))
				{
					customItems.Remove(uniqId);
				}
				
				customWeaponAmmo.Remove(handler.PsuedoType);

				return true;
			}

			return false;
		}

		/// <summary>
		/// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
		/// </summary>
		/// <param name="player">PlayerObject of the held item to retrieve.</param>
		public static CustomItem HeldCustomItem(this Player player)
		{
			return HeldCustomItem((GameObject)player.GetGameObject());
		}

		/// <summary>
		/// Finds and returns the currently held custom item from a player. Null if they are not holding any custom item.
		/// </summary>
		/// <param name="player">PlayerObject of the held item to retrieve.</param>
		public static CustomItem HeldCustomItem(GameObject player)
		{
			int heldIndex = player.GetComponent<Inventory>().GetItemIndex();

			return heldIndex == -1 ? null : FindCustomItem(player, heldIndex);
		}

		/// <summary>
		/// Finds and returns all custom items within a player's inventory.
		/// </summary>
		/// <param name="player">PlayerObject that should be checked for custom items.</param>
		public static CustomItem[] GetCustomItems(this Player player)
		{
			GameObject unityPlayer = (GameObject)player.GetGameObject();

			return customItems.Values.Where(x => x.PlayerObject == unityPlayer).ToArray();
		}

		/// <summary>
		/// Finds and returns all custom items within a player's inventory.
		/// </summary>
		/// <param name="player">PlayerObject that should be checked for custom items.</param>
		public static CustomItem[] GetCustomItems(GameObject player)
		{
			return customItems.Values.Where(x => x.PlayerObject == player).ToArray();
		}

		/// <summary>
		/// Checks if an item is an instance of a custom item.
		/// </summary>
		/// <param name="pickup">Item to check if it is a custom item.</param>
		public static bool IsCustomItem(this Pickup pickup)
		{
			return customItems.ContainsKey(pickup.info.durability);
		}

		/// <summary>
		/// Checks if an item is an instance of a custom item.
		/// </summary>
		/// <param name="player">PlayerObject that is holding the item to check.</param>
		/// <param name="index">Index of the item in the player's inventory.</param>
		public static bool IsCustomItem(this Player player, int index)
		{
			return IsCustomItem((GameObject)player.GetGameObject(), index);
		}

		/// <summary>
		/// Checks if an item is an instance of a custom item.
		/// </summary>
		/// <param name="player">PlayerObject that is holding the item to check.</param>
		/// <param name="index">Index of the item in the player's inventory.</param>
		public static bool IsCustomItem(GameObject player, int index)
		{
			return customItems.Values.Any(x => x.Index == index && x.PlayerObject == player);
		}

		/// <summary>
		/// Finds a custom item from a vanilla item. 
		/// </summary>
		/// <param name="player">PlayerObject that is holding the custom item.</param>
		/// <param name="index">Index of the item in the player's inventory.</param>
		public static CustomItem FindCustomItem(this Player player, int index)
		{
			return FindCustomItem((GameObject)player.GetGameObject(), index);
		}

		/// <summary>
		/// Finds a custom item from a vanilla item. 
		/// </summary>
		/// <param name="player">PlayerObject that is holding the custom item.</param>
		/// <param name="index">Index of the item in the player's inventory.</param>
		public static CustomItem FindCustomItem(GameObject player, int index)
		{
			return customItems.Values.FirstOrDefault(x => x.Index == index && x.PlayerObject == player);
		}

		/// <summary>
		/// Finds a custom item from a vanilla item. 
		/// </summary>
		/// <param name="pickup">The vanilla item.</param>
		public static CustomItem FindCustomItem(this Pickup pickup)
		{
			return customItems.ContainsKey(pickup.info.durability) ? customItems[pickup.info.durability] : null;
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
