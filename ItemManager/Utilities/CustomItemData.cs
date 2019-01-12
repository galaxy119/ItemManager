namespace ItemManager.Utilities
{
    internal class CustomItemData
    {
        public bool readyForDoubleDrop;
        public int doubleDropTimer;
        public bool justShot;

        public CustomItem Item { get; }

        public CustomItemData(CustomItem item)
        {
            Item = item;
        }
    }
}
