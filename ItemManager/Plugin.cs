using Smod2.Attributes;
using scp4aiur;
using Smod2.Config;

namespace ItemManager {
    [PluginDetails(
        author = "4aiur",
        description = "Allows other plugins to register custom item associations.",
        id = "4aiur.custom.itemmanager",
        version = "1.0.0",
        SmodMajor = 3,
        SmodMinor = 1,
        SmodRevision = 0)]
    public class Plugin : Smod2.Plugin {
        internal static Plugin instance;
        internal static bool heldItems;

        public override void Register() {
            instance = this;

            AddConfig(new ConfigSetting("itemmanager_helditems", true, SettingType.BOOL, true, "Whether or not ItemManager will take held items into account in 914."));
            heldItems = true;

            AddEventHandlers(new Timing(Info));
            AddEventHandlers(new EventHandlers());
        }

        public override void OnEnable() {

        }

        public override void OnDisable() {

        }
    }
}
