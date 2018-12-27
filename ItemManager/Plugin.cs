using Smod2.Attributes;
using scp4aiur;
using Smod2.Config;

namespace ItemManager
{
    [PluginDetails(
        author = "4aiur",
        description = "Allows other plugins to register custom item associations.",
        id = "4aiur.custom.itemmanager",
        version = "1.0.0",
        SmodMajor = 3,
        SmodMinor = 2,
        SmodRevision = 0)]
    public class Plugin : Smod2.Plugin
    {
        internal static Plugin instance;
        internal static int heldItems;

        public override void Register()
        {
            instance = this;

            AddConfig(new ConfigSetting("itemmanager_helditems", 3, SettingType.NUMERIC, true, "Whether or not ItemManager will take held items into account in 914. 0 for none, 1 for only custom items, 2 for only normal items, 3 for all items."));
            heldItems = 3;

            Timing.Init(this);
            AddEventHandlers(new EventHandlers());
        }

        public override void OnEnable()
        {

        }

        public override void OnDisable()
        {

        }
    }
}
