using System;
using Smod2.API;
using UnityEngine;

namespace ItemManager.Recipes {
    public class Id914Recipe : Base914Recipe {
        public KnobSetting Knob { get; }

        public int InputId { get; }
        public bool InputIsVanilla { get; }

        public int OutputId { get; }
        public bool OutputIsVanilla { get; }

        public float OutputDurability { get; }

        public Id914Recipe(KnobSetting knob, int inputId, int outputId, float outputDurability = -4.656647E+11f) {
            Knob = knob;

            InputId = inputId;
            InputIsVanilla = inputId < 30;

            OutputId = outputId;
            OutputIsVanilla = outputId < 30;

            OutputDurability = outputDurability;
        }

        private void CreateOutput(Pickup pickup) {
            if (OutputIsVanilla) {
                Pickup.PickupInfo info = pickup.info;
                info.itemId = OutputId;
                info.durability = OutputDurability;

                pickup.Networkinfo = info;
            }
            else {
                if (Items.registeredItems.ContainsKey(OutputId)) {
                    Items.ConvertItem(OutputId, pickup);
                } else {
                    throw new IndexOutOfRangeException("No registered items have the specified output ID.");
                }
            }
        }

        private void CreateOutput(CustomItem item) {
            if (OutputIsVanilla) {
                item.Delete();

                Pickup.PickupInfo info = item.Pickup.info;
                info.itemId = OutputId;
                info.durability = OutputDurability;
                item.Pickup.Networkinfo = info;
            } else {
                if (Items.registeredItems.ContainsKey(OutputId)) {
                    item.Delete();

                    item = Items.CreateItem(OutputId, item.Pickup.transform.position, item.Pickup.transform.rotation);
                    item.Durability = OutputDurability;
                } else {
                    throw new IndexOutOfRangeException("No registered items have the specified output ID.");
                }
            }
        }

        public override bool IsMatch(KnobSetting knob, Pickup pickup) {
            return InputIsVanilla && knob == Knob && pickup.info.itemId == InputId;
        }

        public override bool IsMatch(KnobSetting knob, CustomItem item) {
            return !InputIsVanilla && knob == Knob && item.PsuedoType == InputId;
        }

        public override void Run(Pickup pickup) {
            CreateOutput(pickup);
        }

        public override void Run(CustomItem item) {
            CreateOutput(item);
        }
    }
}
