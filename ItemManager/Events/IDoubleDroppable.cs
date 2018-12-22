namespace ItemManager.Events
{
    /// <summary>
    /// <para>Implements double droppability.</para>
    /// <para>Note: This must be inherited instead of being normal code in order to prevent drop lag for all custom items and to increase overall performance.</para>
    /// </summary>
    public interface IDoubleDroppable
    {
        /// <summary>
        /// Amount of time between two clicks to register for a double click.
        /// </summary>
        float DoubleDropWindow { get; }

        /// <summary>
        /// Invoked when the item is right clicked twice in a player's inventory. Return value determines if the item will drop.
        /// </summary>
        bool OnDoubleDrop();
    }
}
