using ItemManager;
using scp4aiur;
using Smod2.API;
using UnityEngine;

namespace Example {
    public class BetterM4 : CustomItem, IWeapon {
        public override ItemType DefaultItemId => ItemType.E11_STANDARD_RIFLE;

        public void OnHit(GameObject target, ref float damage) {
            Inventory.SyncItemInfo info = Inventory.items[Index];
            info.durability += 2;
            Inventory.items[Index] = info;

            Timing.NextTick(() => {
                Inventory.GetComponent<WeaponManager>().CallRpcReload(Inventory.GetComponent<WeaponManager>().curWeapon);
            });

            Example.log("Shot super M4.");
        }
    }
}
