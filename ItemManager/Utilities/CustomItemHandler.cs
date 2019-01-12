using System;
using System.Linq;
using UnityEngine;

namespace ItemManager.Utilities
{
    public interface ICustomItemHandler
    {
        /// <summary>
        /// The item manager used to handle the custom item.
        /// </summary>
        Items Manager { get; }

        /// <summary>
        /// Creates a custom item of this type at a location and rotation.
        /// </summary>
        /// <param name="position">Position of the custom item.</param>
        /// <param name="rotation">Rotation of the custom item.</param>
        CustomItem Create(Vector3 position, Quaternion rotation);
        /// <summary>
        /// Creates a custom item of this type and adds it to an inventory.
        /// </summary>
        /// <param name="inventory">Inventory of the player to create the item for.</param>
        CustomItem Create(Inventory inventory);
        /// <summary>
        /// Creates a custom item of this type from the pickup;
        /// </summary>
        /// <param name="pickup">Dropped to turn into custom item.</param>
        CustomItem Create(Pickup pickup);
        /// <summary>
        /// Creates a custom item of this type from an inventory.
        /// </summary>
        /// <param name="inventory">Inventory of the player to create the item from.</param>
        /// <param name="index">Index of the item in the inventory.</param>
        CustomItem Create(Inventory inventory, int index);
    }

    public class CustomItemHandler<TItem> : ICustomItemHandler where TItem : CustomItem, new()
    {
        private bool unmanaged;
        private readonly WorldCustomItems items;

        private Items manager;
        public Items Manager
        {
            get => manager;
            set
            {
                if (value == null)
                {
                    unmanaged = true;
                    manager.items.Remove(items);
                }
                else
                {
                    if (manager != null)
                    {
                        value.items.Remove(items);
                    }

                    value.items.Add(items);
                    unmanaged = false;
                }

                manager = value;
            }
        }

        public CustomItemHandler()
        {
            items = new WorldCustomItems(this);
        }

        private void ValidateCreationEnvironment()
        {
            if (unmanaged)
            {
                throw new InvalidOperationException($"Please set the {nameof(Manager)} to a non-null value.");
            }
        }

        public CustomItem Create(Vector3 position, Quaternion rotation)
        {
            ValidateCreationEnvironment();

            TItem customItem = new TItem
            {
                UniqueId = FloatIdManager.Instance.NewId(),

                durability = 0,
                Index = -1
            };

            customItem.Dropped = Manager.hostInventory.SetPickup((int)customItem.DefaultItemId,
                customItem.UniqueId,
                position,
                rotation,
                0, 0, 0).GetComponent<Pickup>();
            customItem.ApplyPickup();
            
            customItem.OnInitialize();

            items.Add(customItem.UniqueId, customItem);
            return customItem;
        }

        public CustomItem Create(Inventory inventory)
        {
            ValidateCreationEnvironment();

            if (inventory.items.Count > 8)
            {
                return Create(inventory.gameObject.transform.position, inventory.gameObject.transform.rotation);
            }

            TItem customItem = new TItem
            {
                UniqueId = FloatIdManager.Instance.NewId(),
                    
                durability = 0,
                PlayerObject = inventory.gameObject,
                Inventory = inventory,
                Index = inventory.items.Count
            };
            inventory.AddNewItem((int)customItem.DefaultItemId, 0);
            customItem.ApplyInventory();
            
            customItem.OnInitialize();

            items.Add(customItem.UniqueId, customItem);
            return customItem;
        }

        public CustomItem Create(Pickup pickup)
        {
            ValidateCreationEnvironment();

            TItem customItem = new TItem
            {
                UniqueId = FloatIdManager.Instance.NewId(),

                durability = pickup.info.durability,
                Dropped = pickup,
                Index = -1
            };
            customItem.ApplyPickup();
            customItem.Type = customItem.DefaultItemId;
            
            customItem.OnInitialize();

            items.Add(customItem.UniqueId, customItem);
            return customItem;
        }

        public CustomItem Create(Inventory inventory, int index)
        {
            ValidateCreationEnvironment();

            TItem customItem = new TItem
            {
                UniqueId = FloatIdManager.Instance.NewId(),

                durability = inventory.items[index].durability,
                PlayerObject = inventory.gameObject,
                Inventory = inventory,
                Index = index
            };
            customItem.ApplyInventory();
            customItem.Type = customItem.DefaultItemId;
            
            customItem.OnInitialize();

            items.Add(customItem.UniqueId, customItem);
            return customItem;
        }
    }
}
