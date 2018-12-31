using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ItemManager.Utilities
{
    internal interface ICustomWeaponHandler : ICustomItemHandler
    {
        int DefaultReserveAmmo { get; }
    }

    internal class CustomWeaponHandler<TWeapon> : CustomItemHandler<TWeapon>, ICustomWeaponHandler where TWeapon : CustomItem, new() {
        public int DefaultReserveAmmo { get; }

        public CustomWeaponHandler(int psuedoId, int defaultReserveAmmo) : base(psuedoId)
        {
            DefaultReserveAmmo = defaultReserveAmmo;
        }
    }
}
