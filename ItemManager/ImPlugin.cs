using System;

using scp4aiur;
using ItemManager.Commands;

using Smod2;
using Smod2.Config;
using Smod2.Attributes;

namespace ItemManager
{
    [Flags]
    public enum HeldSetting
    {
        None = 0,
        Custom = 1,
        Vanilla = 2,
        All = Custom | Vanilla
    }

    [PluginDetails(
        author = "4aiur",
        name = "Item Manager",
        description = "Allows other plugins to register custom item associations.",
        id = "4aiur.custom.itemmanager",
        version = "1.0.0",
        SmodMajor = 3,
        SmodMinor = 2,
        SmodRevision = 2)]
    public class ImPlugin : Plugin
    {
        public HeldSetting HeldItems { get; private set; }
        public string[] GiveRanks { get; private set; }

        public override void Register()
        {
            AddConfig(new ConfigSetting("im_helditems", (int)HeldSetting.All, SettingType.NUMERIC, true, "Whether or not ItemManager will take held items into account in 914. 0 for none, 1 for only custom items, 2 for only normal items, 3 for all items."));
            AddConfig(new ConfigSetting("im_give_ranks", new[]
            {
                "owner",
                "admin"
            }, SettingType.LIST, true, "Rank names that should be allowed to use the give command."));

            Timing.Init(this);
            AddEventHandlers(new EventHandlers(this));
            AddCommand("imgive", new GiveCommand(this));
        }

        public void RefreshConfig()
        {
            HeldItems = (HeldSetting)GetConfigInt("im_helditems");
            GiveRanks = GetConfigList("im_give_ranks");
        }

        public override void OnEnable()
        {

        }

        public override void OnDisable()
        {

        }
    }
}
