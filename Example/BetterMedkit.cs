using ItemManager;
using ItemManager.Events;
using Smod2.API;

namespace Example
{
    public class BetterMedkit : CustomItem, IDoubleDroppable
    {
        public override ItemType DefaultItemId => ItemType.MEDKIT;

        public float DoubleDropWindow => 0.25f;

        public BetterMedkit()
        {
            Example.log("Created super medkit.");
        }

        public override bool OnPickup()
        {
            Example.log($"Picked up super medkit {UniqueId}.");

            return true;
        }

        public override bool OnDrop()
        {
            Example.log($"Dropped super medkit {UniqueId}.");

            return true;
        }

        public bool OnDoubleDrop()
        {
            Example.log($"Double dropped super medkit {UniqueId}.");

            return true;
        }
    }
}
