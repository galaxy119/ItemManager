using System;
using Smod2.API;

namespace ItemManager.Recipes
{
    public class Id914Recipe : Base914Recipe
    {
        public KnobSetting Knob { get; }

        public int InputId { get; }
        public bool InputIsVanilla { get; }
        public bool InputCanBeHeld { get; }

        public int OutputId { get; }
        public bool OutputIsVanilla { get; }

        public float OutputDurability { get; }

        public Id914Recipe(KnobSetting knob, int inputId, int outputId, float outputDurability = -4.656647E+11f, bool heldCompatible = true)
        {
            Knob = knob;

            InputId = inputId;
            InputIsVanilla = inputId < 30;
            InputCanBeHeld = heldCompatible;

            OutputId = outputId;
            OutputIsVanilla = outputId < 30;

            OutputDurability = outputDurability;
        }

        private void CreateOutput(Pickup pickup)
        {
            if (OutputIsVanilla)
            {
                Pickup.PickupInfo info = pickup.info;
                info.itemId = OutputId;
                info.durability = OutputDurability;

                pickup.Networkinfo = info;
            }
            else
            {
                if (Items.registeredItems.ContainsKey(OutputId))
                {
                    Items.ConvertItem(OutputId, pickup);
                }
                else
                {
                    throw new IndexOutOfRangeException("No registered items have the specified output ID.");
                }
            }
        }

        private void CreateOutput(CustomItem item, bool held)
        {
            item.Unhook();

            if (OutputIsVanilla)
            {
                if (held)
                {
                    item.Inventory.AddNewItem(OutputId, OutputDurability);
                }
                else
                {
                    Pickup.PickupInfo info = item.Pickup.info;
                    info.itemId = OutputId;
                    info.durability = OutputDurability;
                    item.Pickup.Networkinfo = info;
                }
            }
            else
            {
                if (Items.registeredItems.ContainsKey(OutputId))
                {
                    item = held ? Items.GiveItem(item.Player, OutputId) : Items.CreateItem(OutputId, item.Pickup.transform.position, item.Pickup.transform.rotation);
                    item.Durability = OutputDurability;
                }
                else
                {
                    throw new IndexOutOfRangeException("No registered items have the specified output ID.");
                }
            }
        }

        public override bool IsMatch(KnobSetting knob, Inventory inventory, int index)
        {
            return InputCanBeHeld && InputIsVanilla && knob == Knob && inventory.items[index].id == InputId;
        }

        public override bool IsMatch(KnobSetting knob, Pickup.PickupInfo pickup)
        {
            return InputIsVanilla && knob == Knob && pickup.itemId == InputId;
        }

        public override bool IsMatch(KnobSetting knob, CustomItem item, bool held)
        {
            return !InputIsVanilla && knob == Knob && item.PsuedoType == InputId && (!held || InputCanBeHeld);
        }

        public override void Run(Pickup pickup)
        {
            CreateOutput(pickup);
        }

        public override void Run(CustomItem item, bool held)
        {
            CreateOutput(item, held);
        }
    }
}
