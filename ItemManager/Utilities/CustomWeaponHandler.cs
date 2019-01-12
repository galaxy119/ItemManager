using UnityEngine;

namespace ItemManager.Utilities
{
    public interface ICustomWeaponHandler : ICustomItemHandler
    {
        int DefaultReserveAmmo { get; }
    }

    public class CustomWeaponHandler<TWeapon> : CustomItemHandler<TWeapon>, ICustomWeaponHandler where TWeapon : CustomWeapon, new()
    {
        public int DefaultReserveAmmo { get; set; }

        CustomItem ICustomItemHandler.Create(Vector3 position, Quaternion rotation)
        {
        }
    }
}
