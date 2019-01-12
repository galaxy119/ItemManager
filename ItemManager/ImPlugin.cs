using System;
using scp4aiur;
using ItemManager.Commands;
using ItemManager.Utilities;
using Smod2.Attributes;
using Smod2;
using Smod2.API;
using Smod2.Config;

namespace ItemManager
{
    [Flags]
    public enum HeldSetting
    {
        None = 0,
        Custom = 1,
        Normal = 2,
        All = Normal | Custom
    }

    [PluginDetails(
        author = "4aiur",
        description = "Allows other plugins to register custom item associations.",
        id = "4aiur.custom.itemmanager",
        version = "1.0.0",
        SmodMajor = 3,
        SmodMinor = 2,
        SmodRevision = 2)]
    public class ImPlugin : Plugin
    {
        public HeldSetting heldItems;
        public string[] giveRanks;

        public Items Manager { get; private set; }

        public override void Register()
        {
            AddConfig(new ConfigSetting("im_helditems", (int)HeldSetting.None, SettingType.NUMERIC, true, "Whether or not ItemManager will take held items into account in 914. 0 for none, 1 for only custom items, 2 for only normal items, 4 for all items."));
            AddConfig(new ConfigSetting("im_give_ranks", new[]
            {
                "owner",
                "admin"
            }, SettingType.LIST, true, "Rank names that should be allowed to use the give command."));
            AddConfig(new ConfigSetting("im_ammo_id", (int)ItemType.COIN, SettingType.NUMERIC, true, "Item ID of the default custom-ammo pickup."));

            Timing.Init(this);
            
            FloatIdManager.Instance = new FloatIdManager();
            Manager = new Items();

            AddEventHandlers(new EventHandlers(this));
            AddCommand("imgive", new GiveCommand(this));
        }

        public void RefreshConfig()
        {
            heldItems = (HeldSetting)GetConfigInt("im_helditems");
            giveRanks = GetConfigList("im_give_ranks");
        }

        public override void OnEnable()
        {

        }

        public override void OnDisable()
        {

        }
    }
}
