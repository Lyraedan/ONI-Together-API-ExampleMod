using Shared.Helpers;
using Shared.OxySync;
using UnityEngine;

namespace ExampleMod.OxySync
{
    /// <summary>
    /// Spawns an <see cref="ExampleSyncBehaviour"/> and gives it a network identity so OxySync
    /// can route SyncVars/RPCs to it.
    /// </summary>
    public static class ExampleSyncSpawner
    {
        private static ExampleSyncBehaviour _instance;

        public static ExampleSyncBehaviour Instance => _instance;

        public static bool TryGet(out ExampleSyncBehaviour behaviour)
        {
            behaviour = _instance;
            return behaviour != null;
        }

        /// <summary>
        /// Creates the demo behaviour on its own persistent GameObject and assigns a NetId.
        /// Safe to call repeatedly - it returns the existing instance if one is alive.
        /// </summary>
        public static ExampleSyncBehaviour Spawn()
        {
            if (_instance != null)
                return _instance;

            var go = new GameObject("ExampleMod_SyncEntity");
            Object.DontDestroyOnLoad(go);

            // Ensure this object has a network identity and read its assigned NetId.
            int netId = NetIdentityHelper.AddNetId(go);

            var behaviour = go.AddComponent<ExampleSyncBehaviour>();
            _instance = behaviour;

            Debug.Log($"[ExampleMod] Spawned ExampleSyncBehaviour (NetId={netId})");
            return behaviour;
        }

        public static void Despawn()
        {
            if (_instance == null) return;
            Object.Destroy(_instance.gameObject);
            _instance = null;
        }
    }
}
