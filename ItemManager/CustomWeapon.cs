using RemoteAdmin;
using scp4aiur;
using UnityEngine;

namespace ItemManager
{
    public abstract class CustomWeapon : CustomItem
    {
        private readonly WeaponManager manager;

        private int playerId;
        private int WeaponManagerIndex
        {
            get
            {
                for (int i = 0; i < manager.weapons.Length; i++)
                {
                    if ((int)Type == manager.weapons[i].inventoryID)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public int ReserveAmmo { get; protected set; }

        public int MagazineAmmo { get; protected set; }
        public abstract int MagazineCapacity { get; }

        public abstract float FireRate { get; }

        protected CustomWeapon()
        {
            manager = GameObject.Find("Host").GetComponent<WeaponManager>();
        }

        public override void OnInitialize()
        {
            Durability = MagazineCapacity;
        }

        public override void OnShoot(GameObject target, ref float damage)
        {
            if (!CanShoot(target, damage))
            {
                Durability++;
                damage = 0;
                return;
            }

            // Player just reloaded
            if (Durability + 1 == manager.weapons[WeaponManagerIndex].maxAmmo)
            {
                MagazineAmmo = MagazineCapacity;
            }

            Durability = 0;

            if (--MagazineAmmo > 0)
            {
                if (FireRate > 0)
                {
                    Timing.In(x => Durability = MagazineAmmo, FireRate);
                }
                else
                {
                    Durability = MagazineAmmo;
                }
            }
            else
            {
                AmmoBox ammo = PlayerObject.GetComponent<AmmoBox>();
                WeaponManager.Weapon weapon = manager.weapons[WeaponManagerIndex];

                // Give player enough ammo for a reload
                ammo.SetOneAmount(weapon.ammoType, (ammo.GetAmmo(weapon.ammoType) + weapon.maxAmmo).ToString());

                if (ReserveAmmo > 0)
                {
                    // Remove mag ammo
                    ReserveAmmo -= Mathf.Min(ReserveAmmo, MagazineCapacity);
                    Items.customWeaponAmmo[PsuedoType][playerId] = ReserveAmmo;
                }
                else
                {
                    damage = 0;
                    return;
                }
            }

            OnValidShoot(target, ref damage);
        }
        protected virtual bool CanShoot(GameObject target, float damage) { return true; }
        protected virtual void OnValidShoot(GameObject target, ref float damage) { }

        public override bool OnPickup()
        {
            if (!CanPickup())
            {
                return false;
            }

            playerId = PlayerObject.GetComponent<QueryProcessor>().PlayerId;
            ReserveAmmo = Items.customWeaponAmmo[PsuedoType][playerId];

            return true;
        }
        protected virtual bool CanPickup() { return true; }
    }
}
