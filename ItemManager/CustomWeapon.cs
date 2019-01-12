using ItemManager.Utilities;
using RemoteAdmin;
using scp4aiur;
using UnityEngine;

namespace ItemManager
{
    public abstract class CustomWeapon : CustomItem
    {
        private readonly WeaponManager manager;
        private float prevDurability;
        private int playerId;

        private int WeaponManagerIndex
        {
            get
            {
                for (int i = 0; i < manager.weapons.Length; i++)
                {
                    if ((int)VanillaType == manager.weapons[i].inventoryID)
                    {
                        return i;
                    }
                }

                return -1;
            }
        }

        public new ICustomWeaponHandler Handler { get; private set; }

        public int ReserveAmmo
        {
            get => Dropped == null ? Items.customWeaponAmmo[Handler.PsuedoType][playerId] : -1;
            set
            {
                if (Dropped == null)
                {
                    Items.customWeaponAmmo[Handler.PsuedoType][playerId] = value;
                }
            }
        }

        public int MagazineAmmo { get; protected set; }
        public abstract int MagazineCapacity { get; }

        public abstract float FireRate { get; }

        protected CustomWeapon()
        {
            manager = GameObject.Find("Host").GetComponent<WeaponManager>();
        }

        public override void OnInitialize()
        {
            Handler = (ICustomWeaponHandler) base.Handler;

            playerId = PlayerObject?.GetComponent<QueryProcessor>()?.PlayerId ?? -1;

            Durability = MagazineCapacity;
            MagazineAmmo = MagazineCapacity;
            prevDurability = Durability;
        }

        internal void Reload()
        {
            int curMagAmmo = MagazineAmmo;
            MagazineAmmo = Mathf.Min(ReserveAmmo + curMagAmmo, MagazineCapacity);
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
