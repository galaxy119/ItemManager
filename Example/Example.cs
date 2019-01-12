using System;

using ItemManager;
using ItemManager.Recipes;
using ItemManager.Utilities;
using Smod2.API;
using Smod2.Attributes;

namespace Example
{
    [PluginDetails(
        author = "4aiur",
        description = "ItemManager testing plugin",
        id = "4aiur.custom.itemmanagerexample",
        version = "1.0",
        SmodMajor = 3,
        SmodMinor = 0,
        SmodRevision = 0)]
    public class Example : Smod2.Plugin
    {
        internal static Action<string> log;

        public override void Register()
        {
            CustomItemHandler<BetterMedkit> betterMedkit = new CustomItemHandler<BetterMedkit>(31)
            {
                DefaultType = ItemType.MEDKIT
            };
            betterMedkit.Register();
            Items.AddRecipe(new Id914Recipe(KnobSetting.FINE, (int)ItemType.MEDKIT, 31));

            CustomWeaponHandler<BetterM4> betterM4 = new CustomWeaponHandler<BetterM4>(32)
            {
                DefaultType = ItemType.E11_STANDARD_RIFLE,
                DefaultReserveAmmo = 10
            };
            betterM4.Register();
            Items.AddRecipe(new Id914Recipe(KnobSetting.FINE, (int)ItemType.E11_STANDARD_RIFLE, 32, 1));

            log = Info;
        }

        public override void OnEnable()
        {
        }

        public override void OnDisable()
        {
        }
    }
}
