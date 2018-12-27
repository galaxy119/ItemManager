using UnityEngine;

namespace ItemManager.Events
{
    public interface IWeapon
    {
        float FireRate { get; }
        int MagazineSize { get; }
        int CurrentAmmo { get; set; }

        void OnShoot(GameObject target, ref float damage);
    }
}
