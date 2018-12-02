using Smod2;
using Smod2.Attributes;
using scp4aiur;

namespace ItemManager {
    [PluginDetails(
        author = "4aiur",
        description = "Allows other plugins to register custom item associations.",
        id = "4aiur.custom.itemmanager",
        version = "1.0.0",
        SmodMajor = 3,
        SmodMinor = 0,
        SmodRevision = 0)]
    public class ItemManager : Plugin {
        public override void Register() {
            AddEventHandlers(new Timing(Info));
            AddEventHandlers(new EventHandlers());
        }

        public override void OnEnable() {

        }

        public override void OnDisable() {

        }
    }
}
