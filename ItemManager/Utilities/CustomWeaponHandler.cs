namespace ItemManager.Utilities
{
    public interface ICustomWeaponHandler : ICustomItemHandler
    {
        int DefaultReserveAmmo { get; }
    }

    public class CustomWeaponHandler<TWeapon> : CustomItemHandler<TWeapon>, ICustomWeaponHandler where TWeapon : CustomItem, new()
    {
        public int DefaultReserveAmmo { get; set; }

        public CustomWeaponHandler(int psuedoId) : base(psuedoId) { }
    }
}
