using ItemManager.Utilities;
using scp4aiur;
using Smod2.API;

using UnityEngine;
using RemoteAdmin;

namespace ItemManager
{
    public abstract class CustomWeapon : CustomItem
    {
        private readonly WorldCustomWeapons handler;
        private readonly WeaponManager wepManager;
        private float prevDurability;
        private int playerId;

        private int WeaponManagerIndex
        {
            get
            {
                for (int i = 0; i < wepManager.weapons.Length; i++)
                {
                    if ((int)Type == wepManager.weapons[i].inventoryID)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public int ReserveAmmo
        {
            get => Dropped == null ? handler.[playerId] : -1;
            set
            {
                if (Dropped == null)
                {
                    Items.customWeaponAmmo[PsuedoType][playerId] = value;
                }
            }
        }

        public int MagazineAmmo { get; protected set; }
        public abstract int MagazineCapacity { get; }

        public abstract float FireRate { get; }

        protected CustomWeapon(Items manager, ICustomWeaponHandler handler) : base(manager)
        {
            this.handler = handler;
            wepManager = GameObject.Find("Host").GetComponent<WeaponManager>();
        }

        public override void OnInitialize()
        {
            Durability = MagazineCapacity;
            MagazineAmmo = MagazineCapacity;
            prevDurability = Durability;

            playerId = PlayerObject?.GetComponent<QueryProcessor>()?.PlayerId ?? -1;
        }

        private void AddAmmo(int amount)
        {
            AmmoBox ammo = PlayerObject.GetComponent<AmmoBox>();
            WeaponManager.Weapon weapon = wepManager.weapons[WeaponManagerIndex];

            // Give player enough ammo for a reload
            ammo.SetOneAmount(weapon.ammoType, (ammo.GetAmmo(weapon.ammoType) + amount).ToString());
        }

        internal void Reload()
        {
            int curMagAmmo = MagazineAmmo;
            MagazineAmmo = Mathf.Min(ReserveAmmo + MagazineAmmo, MagazineCapacity);
            ReserveAmmo -= MagazineAmmo - curMagAmmo;
        }

        public override void OnShoot(GameObject target, ref float damage)
        {
            if (!CanShoot(target, damage))
            {
                Durability++;
                damage = 0;
                return;
            }
            
            if (prevDurability < Durability)
            {
                AddAmmo((int)(Durability + 1 - prevDurability));

                Reload();
                Durability = MagazineAmmo;
            }

            if (MagazineAmmo <= 0)
            {
                damage = 0;
                return;
            }

            if (MagazineAmmo > 0)
            {
                MagazineAmmo--;

                if (FireRate > 0)
                {
                    Durability = 0;

                    Timing.In(x => Durability = MagazineAmmo, FireRate);
                }
                else
                {
                    Durability = MagazineAmmo;
                }

                prevDurability = MagazineAmmo;
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

            return true;
        }
        protected virtual bool CanPickup() { return true; }
    }
}
