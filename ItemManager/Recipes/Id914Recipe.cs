using System;
using System.Collections.Generic;
using System.Linq;
using Smod2.API;
using UnityEngine;

namespace ItemManager.Recipes {
    public class Id914Recipe : Base914Recipe {
        public KnobSetting Knob { get; }

        public int InputId { get; }
        public bool InputIsCustom { get; }

        public int OutputId { get; }
        public bool OutputIsCustom { get; }

        public float OutputDurability { get; }

        public Id914Recipe(KnobSetting knob, int inputId, int outputId, float outputDurability = -4.656647E+11f) {
            Knob = knob;

            InputId = inputId;
            InputIsCustom = inputId < 30;

            OutputId = outputId;
            OutputIsCustom = outputId < 30;

            OutputDurability = outputDurability;
        }

        private void CreateOutput(Pickup pickup) {
            Vector3 position = pickup.info.position + (Items.scp.output_obj.position - Items.scp.intake_obj.position);

            if (OutputIsCustom) {
                Pickup drop = Items.hostInventory.SetPickup(OutputId, OutputDurability,
                    position,
                    pickup.info.rotation).GetComponent<Pickup>();

                Pickup.PickupInfo info = drop.info;
                info.durability = OutputDurability;
                drop.Networkinfo = info;
            }
            else {
                if (Items.registeredItems.ContainsKey(OutputId)) {
                    CustomItem item = Items.CreateItem(OutputId, position, pickup.info.rotation);
                    item.Durability = OutputDurability;
                } else {
                    throw new IndexOutOfRangeException("No registered items have the specified output ID.");
                }
            }
        }

        public override bool IsMatch(KnobSetting knob, Pickup pickup) {
            return !InputIsCustom && knob == Knob && pickup.info.itemId == InputId;
        }

        public override bool IsMatch(KnobSetting knob, CustomItem item) {
            return InputIsCustom && knob == Knob && item.PsuedoType == InputId;
        }

        public override void Run(Pickup pickup) {
            pickup.Delete();
            CreateOutput(pickup);
        }

        public override void Run(CustomItem item) {
            item.Delete();
            CreateOutput(item.Pickup);
        }
    }
}
