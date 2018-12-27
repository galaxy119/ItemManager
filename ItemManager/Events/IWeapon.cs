using UnityEngine;

namespace ItemManager.Events
{
    public interface IWeapon
    {
        float FireRate { get; }
        int MagazineSize { get; }
        int AmmoInMagazine { get; set; }
        int TotalAmmo { get; set; }

        void OnShoot(GameObject target, ref float damage);
    }
}
