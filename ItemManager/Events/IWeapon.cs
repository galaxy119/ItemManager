using UnityEngine;

namespace ItemManager {
    public interface IWeapon {
        /// <summary>
        /// Invoked when the item is used (e.g. shot one round, one laceration from micro, etc).
        /// </summary>
        void OnHit(GameObject target, ref float damage);
    }
}
