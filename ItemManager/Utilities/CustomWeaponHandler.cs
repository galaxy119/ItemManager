using RemoteAdmin;
using Smod2;
using Smod2.API;
using UnityEngine;

namespace ItemManager.Utilities
{
    public interface ICustomWeaponHandler : ICustomItemHandler
    {
        ItemType? DroppedAmmoType { get; }
        int? DroppedAmmoCount { get; }
        string AmmoName { get; }
        Color32 AmmoColor { get; }
        int DefaultReserveAmmo { get; }

        DroppedAmmo CreateAmmo(Vector3 position, Quaternion rotation, int count = -1);
    }

    public class CustomWeaponHandler<TWeapon> : CustomItemHandler<TWeapon>, ICustomWeaponHandler where TWeapon : CustomWeapon, new()
    {
        private readonly CustomItemHandler<DroppedAmmo> ammoFactory;

        public ItemType? DroppedAmmoType { get; set; }
        public int? DroppedAmmoCount { get; set; }
        public string AmmoName { get; set; }
        public Color32 AmmoColor { get; set; }
        public int DefaultReserveAmmo { get; set; }

        public CustomWeaponHandler(int psuedoId) : base(psuedoId)
        {
            AmmoColor = Color.white;

            ammoFactory = new CustomItemHandler<DroppedAmmo>(99);
        }

        public DroppedAmmo CreateAmmo(Vector3 position, Quaternion rotation)
        {
            return CreateAmmo(position, rotation, DroppedAmmoCount ?? Items.DefaultDroppedAmmoCount);
        }

        public DroppedAmmo CreateAmmo(Vector3 position, Quaternion rotation, int count)
        {
            DroppedAmmo ammo = ammoFactory.CreateOfType(position, rotation);
            ammo.handler = this;
            ammo.VanillaType = DroppedAmmoType ?? Items.DefaultDroppedAmmoType;
            ammo.Count = count;

            return ammo;
        }
    }

    public class DroppedAmmo : CustomItem
    {
        internal ICustomWeaponHandler handler;

        public int Count { get; set; }

        private static string ColorHex(Color32 c)
        {
            return $"{c.r:X2}{c.g:X2}{c.b:X2}";
        }

        private string AmmoCountText(byte alpha)
        {
            return handler.AmmoName == null ? $"<color=#{ColorHex(handler.AmmoColor)}{alpha:X2}>{Count} unnamed ammo</color>" : $"<color=#{ColorHex(handler.AmmoColor)}{alpha:X2}>{Count} {handler.AmmoName.Replace("<", "<<")}</color>";
        }

        private string AmmoBroadcastText(byte alpha)
        {
            return $"<b>{(handler.AmmoName == null ? $"<color=#ff0000{alpha:X2}>Unknown ammo</color>" : $"<color=#ffffff{alpha:X2}>Ammo:</color> {AmmoCountText(alpha)}")}</b>";
        }

        public override bool OnPickup()
        {
            QueryProcessor query = PlayerObject.GetComponent<QueryProcessor>();
            int playerId = query.PlayerId;

            if (Items.customWeaponAmmo[handler.PsuedoType].ContainsKey(playerId))
            {
                Items.customWeaponAmmo[handler.PsuedoType][playerId] += handler.DroppedAmmoCount ?? Items.DefaultDroppedAmmoCount;
            }
            else
            {
                Items.customWeaponAmmo[handler.PsuedoType].Add(playerId, handler.DroppedAmmoCount ?? Items.DefaultDroppedAmmoCount);
            }

            PlayerObject.GetComponent<CharacterClassManager>().TargetConsolePrint(query.connectionToClient, $"Picked up ammo: {AmmoCountText(64)}", "green");
            GameObject.Find("Host").GetComponent<Broadcast>().CallTargetAddElement(query.connectionToClient, AmmoBroadcastText(32), 4, false);

            Delete();
            return true;
        }
    }
}
