using Smod2;
using Smod2.Attributes;
using scp4aiur;
using ItemManager.Recipes;

using System.Collections.Generic;
using System.Linq;

namespace ItemManager {
    [PluginDetails(
        author = "4aiur",
        description = "Allows other plugins to register custom item associations.",
        id = "4aiur.custom.itemmanager",
        version = "1.0",
        SmodMajor = 3,
        SmodMinor = 0,
        SmodRevision = 0)]
    public class ItemManager : Plugin {
        public override void Register() {
            Items.Init();

            AddEventHandlers(new Timing());
            AddEventHandlers(new EventHandlers());
        }

        public override void OnEnable() {

        }

        public override void OnDisable() {

        }
    }
}
