﻿using RemoteAdmin;
using scp4aiur;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ItemManager
{
    public abstract class CustomWeapon : CustomItem
    {
        private int playerId;
        private WeaponManager manager;
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
        private float DurabilityThisShot => (Durability + 1) / MagazineAmmo;

        public int ReserveAmmo { get; protected set; }
        public abstract int DefaultReserveAmmo { get; }

        public int MagazineAmmo { get; protected set; }
        public abstract int MagazineCapacity { get; }

        public abstract float FireRate { get; }

        public CustomWeapon()
        {
            manager = GameObject.Find("Host").GetComponent<WeaponManager>();
        }

        public override void OnShoot(GameObject target, ref float damage)
        {
            if (!CanShoot(target, damage))
            {
                Durability++;
                damage = 0;
                return;
            }

            Durability = 0;

            if (--MagazineAmmo > 0)
            {
                float curDurability = Durability + 1 - DurabilityThisShot;

                if (FireRate > 0)
                {
                    Timing.In(x => Durability = curDurability, FireRate);
                }
                else
                {
                    Durability = curDurability;
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
                    // Reload managed weapon and get ready
                    MagazineAmmo = Mathf.Min(ReserveAmmo, MagazineCapacity);
                    ReserveAmmo -= MagazineAmmo;
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

            if (!Items.customWeaponAmmo[PsuedoType].ContainsKey(playerId))
            {
                Items.customWeaponAmmo[PsuedoType].Add(playerId, DefaultReserveAmmo);
            }
            ReserveAmmo = Items.customWeaponAmmo[PsuedoType][playerId];

            return true;
        }
        protected virtual bool CanPickup() { return true; }
    }
}
