using ItemManager;
using ItemManager.Events;
using ItemManager.Recipes;

using Smod2.API;

using UnityEngine;

namespace Example
{
    public class BetterM4 : CustomItem, IWeapon
    {
        public float FireRate => 1f;
        public int MagazineSize => 3;
        public int CurrentAmmo { get; set; }

        public override ItemType DefaultItemId => ItemType.E11_STANDARD_RIFLE;

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

        public void OnShoot(GameObject target, ref float damage)
        {
            damage *= 4;

            Example.log("Shot super M4.");
        }
    }

    public class BetterM4Recipe : Custom914Recipe
    {
        public override KnobSetting Knob => KnobSetting.FINE;
        public override int Input => (int)ItemType.E11_STANDARD_RIFLE;

        public override void Run(Pickup pickup)
        {
            ItemManager.Items.ConvertItem(32, pickup);
        }
    }
}
