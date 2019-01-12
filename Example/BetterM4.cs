using ItemManager;
using ItemManager.Recipes;
using ItemManager.Utilities;
using Smod2.API;

using UnityEngine;

namespace Example
{
    public class BetterM4 : CustomWeapon
    {
        public override int MagazineCapacity => 4;
        public override float FireRate => 1.5f;

        public BetterM4()
        {
            Example.log("Created super M4.");
        }

        public override bool OnPickup()
        {
            Example.log($"Picked up super M4 {UniqueId}.");

            return true;
        }

        public override bool OnDrop()
        {
            Example.log($"Dropped super M4 {UniqueId}.");

            return true;
        }

        public bool OnDoubleDrop()
        {
            Example.log($"Double dropped super M4 {UniqueId}.");

            return true;
        }

        protected override void OnValidShoot(GameObject target, ref float damage)
        {
            damage *= 4;

            Example.log("Shot super M4.");
        }
    }

    public class BetterM4Recipe : Custom914Recipe
    {
        private readonly ICustomItemHandler handler;

        public override KnobSetting Knob => KnobSetting.FINE;
        public override int Input => (int)ItemType.E11_STANDARD_RIFLE;

        public BetterM4Recipe(ICustomItemHandler handler)
        {
            this.handler = handler;
        }

        public override void Run(Pickup pickup)
        {
            handler.Create(pickup);
        }
    }
}
