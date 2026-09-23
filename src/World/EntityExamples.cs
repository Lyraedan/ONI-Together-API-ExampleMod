using ONI_Together_API;
using ONI_Together_API.Misc;
using ONI_Together_API.Networking;
using Shared.Helpers;
using UnityEngine;

namespace ExampleMod.World
{
    /// <summary>
    /// Reference examples for spawning networked entities with <see cref="SpawnUtilsAPI.KNetInstantiate"/>
    /// and finding them again with <see cref="NetworkIdentityRegistryAPI"/>.
    ///
    /// Only the host may spawn. On the host, <c>KNetInstantiate</c> returns the live GameObject and
    /// automatically replicates it to every client. On a client (or when ONI Together is absent) it
    /// returns <c>null</c>.
    ///
    /// There is no API to read a NetId back out of the registry, but <see cref="NetIdentityHelper"/>
    /// returns the NetId that <c>KNetInstantiate</c> assigned, which is exactly what the registry
    /// lookup methods expect.
    /// </summary>
    public static class EntityExamples
    {
        // Spawning

        /// <summary>
        /// Spawns a prefab by <see cref="GameObject"/> on the host and replicates it to all clients.
        /// </summary>
        /// <param name="prefab">A prefab from the game database, e.g. <c>Assets.GetPrefab("Hatch")</c>.</param>
        /// <param name="position">World position for the new GameObject.</param>
        /// <param name="isActive">Whether the spawned GameObject should be active.</param>
        /// <returns>The spawned GameObject on the host, otherwise <c>null</c>.</returns>
        public static GameObject SpawnPrefab(GameObject prefab, Vector3 position, bool isActive = true)
        {
            if (!SessionInfoAPI.IsHost)
            {
                Debug.LogWarning("[ExampleMod] SpawnPrefab requires the host.");
                return null;
            }

            if (prefab == null)
            {
                Debug.LogWarning("[ExampleMod] SpawnPrefab got a null prefab.");
                return null;
            }

            // Positions from the cursor/session API sit on the UI plane, not the world grid.
            // Snap to the prefab's cell + scene layer or the entity renders at the wrong depth
            // (often invisible). Vanilla spawns creatures the same way.
            Vector3 spawnPosition = SnapToGrid(position, prefab);

            var go = SpawnUtilsAPI.KNetInstantiate(prefab, spawnPosition, isActive);
            if (go == null)
                Debug.LogWarning("[ExampleMod] KNetInstantiate(prefab) returned null (invalid prefab?).");
            else
                Debug.Log($"[ExampleMod] Spawned prefab '{go.name}' at {spawnPosition}");

            return go;
        }

        /// <summary>
        /// Snaps a world position to its cell using the prefab's intended scene layer, which is
        /// stored on the prefab's <see cref="KBatchedAnimController"/>.
        /// </summary>
        private static Vector3 SnapToGrid(Vector3 worldPosition, GameObject prefab)
        {
            int cell = Grid.PosToCell(worldPosition);
            if (!Grid.IsValidCell(cell))
                return worldPosition;

            if (prefab.TryGetComponent<KBatchedAnimController>(out var kbac))
                return Grid.CellToPosCBC(cell, kbac.sceneLayer);

            return worldPosition;
        }

        /// <summary>
        /// Spawns an element resource (e.g. ore from digging) on the host, preserving mass,
        /// temperature and disease data, and replicates it to all clients.
        /// </summary>
        /// <param name="element">The element to spawn, e.g. <see cref="SimHashes.IronOre"/>.</param>
        /// <param name="position">World position for the new resource.</param>
        /// <param name="mass">Mass of the resource in kg.</param>
        /// <param name="temperature">Temperature of the resource.</param>
        /// <param name="diseaseIdx">Disease index (0 = no disease).</param>
        /// <param name="diseaseCount">Disease germ count.</param>
        /// <returns>The spawned GameObject on the host, otherwise <c>null</c>.</returns>
        public static GameObject SpawnResource(SimHashes element, Vector3 position, float mass,
            float temperature, byte diseaseIdx = 0, int diseaseCount = 0)
        {
            if (!SessionInfoAPI.IsHost)
            {
                Debug.LogWarning("[ExampleMod] SpawnResource requires the host.");
                return null;
            }

            // The API takes the SimHashes value cast to int.
            var go = SpawnUtilsAPI.KNetInstantiate((int)element, position, mass, temperature, diseaseIdx, diseaseCount);
            if (go == null)
                Debug.LogWarning($"[ExampleMod] KNetInstantiate({element}) returned null (invalid element?).");
            else
                Debug.Log($"[ExampleMod] Spawned {mass}kg of {element} at {position}");

            return go;
        }

        // Registry lookups

        public static bool TryLookup(int netId, out GameObject gameObject)
        {
            bool found = NetworkIdentityRegistryAPI.TryGet(netId, out gameObject);
            if (found)
                Debug.Log($"[ExampleMod] NetId {netId} -> '{gameObject.name}' at {gameObject.transform.position}");
            else
                Debug.Log($"[ExampleMod] NetId {netId} not found in the registry.");
            return found;
        }

        public static bool TryLookupComponent<T>(int netId, out T component)
        {
            bool found = NetworkIdentityRegistryAPI.TryGetComponent<T>(netId, out component);
            if (found)
                Debug.Log($"[ExampleMod] NetId {netId} has a {typeof(T).Name}: {component}");
            else
                Debug.Log($"[ExampleMod] NetId {netId} has no {typeof(T).Name} (or was not found).");
            return found;
        }

        // Combined: spawn, resolve the NetId, then read it back through the registry.

        /// <summary>
        /// Spawns a resource and immediately resolves its NetId so the registry can be queried.
        /// This is the full loop a mod would use: spawn -> NetId -> lookup.
        /// </summary>
        public static void SpawnResourceAndInspect(SimHashes element, Vector3 position, float mass, float temperature)
        {
            var go = SpawnResource(element, position, mass, temperature);
            if (go == null)
                return;

            // KNetInstantiate already registered a NetworkIdentity; AddOrGetNetId returns it.
            int netId = NetIdentityHelper.AddOrGetNetId(go);
            Debug.Log($"[ExampleMod] Spawned entity NetId = {netId}");

            // Query it straight back out of the registry, by NetId.
            if (TryLookupComponent<PrimaryElement>(netId, out var primaryElement))
                Debug.Log($"[ExampleMod] PrimaryElement: {primaryElement.ElementID} " +
                          $"{primaryElement.Mass}kg @ {primaryElement.Temperature}K");
        }
    }
}
