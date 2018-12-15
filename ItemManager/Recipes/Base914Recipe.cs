using Smod2.API;

namespace ItemManager.Recipes {
    public abstract class Base914Recipe {
        /// <summary>
        /// The priority of the 914 recipe.
        /// </summary>
        public virtual ushort Priority => 0;

        /// <summary>
        /// Verifies whether or not a 914 input and setting matches a recipe.
        /// </summary>
        /// <param name="knob">The knob setting of 914 when activated.</param>
        /// <param name="inventory">The inventory that contains the item.</param>
        /// <param name="index">The index of the item in the inventory.</param>
        public virtual bool IsMatch(KnobSetting knob, Inventory inventory, int index) {
            return false;
        }

        /// <summary>
        /// Verifies whether or not a 914 input and setting matches a recipe.
        /// </summary>
        /// <param name="knob">The knob setting of 914 when activated.</param>
        /// <param name="pickup">The vanilla item dropped in 914.</param>
        public virtual bool IsMatch(KnobSetting knob, Pickup.PickupInfo pickup) {
            return false;
        }

        /// <summary>
        /// Verifies whether or not a 914 input and setting matches a recipe.
        /// </summary>
        /// <param name="knob">The knob setting of 914 when activated.</param>
        /// <param name="item">The custom item dropped in 914.</param>
        /// <param name="held">Whether or not the item is being held</param>
        public virtual bool IsMatch(KnobSetting knob, CustomItem item, bool held) {
            return false;
        }

        /// <summary>
        /// Runs the recipe.
        /// </summary>
        /// <param name="inventory">The inventory that contains the item.</param>
        /// <param name="index">The index of the item.</param>
        public virtual void Run(Inventory inventory, int index) { }

        /// <summary>
        /// Runs the recipe.
        /// </summary>
        /// <param name="pickup">The dropped vanilla item.</param>
        public virtual void Run(Pickup pickup) { }

        /// <summary>
        /// Runs the recipe.
        /// </summary>
        /// <param name="item">The dropped custom item.</param>
        /// <param name="held">Whether or not the item is being held.</param>
        public virtual void Run(CustomItem item, bool held) { }
    }
}
