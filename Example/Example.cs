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
            CustomWeaponHandler<BetterMedkit> betterMedkit = new CustomWeaponHandler<BetterMedkit>(31);
            betterMedkit.Register();
            Items.AddRecipe(new Id914Recipe(KnobSetting.FINE, (int)ItemType.MEDKIT, 31));

            CustomWeaponHandler<BetterM4> betterM4 = new CustomWeaponHandler<BetterM4>(32);
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
