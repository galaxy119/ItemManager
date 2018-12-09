using Smod2.API;

namespace ItemManager.Recipes {
    public abstract class Custom914Recipe : Base914Recipe {
        /// <summary>
        /// Knob required to perform this recipe.
        /// </summary>
        public abstract KnobSetting Knob { get; }
        
        /// <summary>
        /// The psuedo ID of the item required to perform this recipe.
        /// </summary>
        public abstract int Input { get; }

        public override bool IsMatch(KnobSetting knob, Pickup.PickupInfo pickup) {
            return Input < 30 && knob == Knob && pickup.itemId == Input;
        }

        public override bool IsMatch(KnobSetting knob, CustomItem item) {
            return !(Input < 30) && knob == Knob && item.PsuedoType == Input;
        }
    }
}
