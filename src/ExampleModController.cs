using ExampleMod.Networking;
using ExampleMod.OxySync;
using ExampleMod.World;
using ONI_Together_API;
using Shared.Helpers;
using UnityEngine;

namespace ExampleMod
{
    /// <summary>
    /// Runtime driver for the example. Creates a persistent GameObject and exposes a few hotkeys
    /// so you can exercise the API in-game:
    ///
    ///   F5  - spawn a Hatch prefab via KNetInstantiate (host only)
    ///   F6  - spawn an IronOre resource via KNetInstantiate (host only)
    ///   F7  - look up the last spawned NetId via NetworkIdentityRegistryAPI
    ///   F8  - remove the OxySync demo behaviour (if spawned)
    ///   F9  - send a hello message (host-rebroadcast pattern)
    ///   F10 - dump session info (ids, host cursor/color)
    ///   F11 - spawn the OxySync demo behaviour
    ///   F12 - increment the OxySync counter via a Command
    ///
    /// All actions are guarded by <see cref="SessionInfoAPI"/> so they are inert in single-player.
    /// </summary>
    public class ExampleModController : MonoBehaviour
    {
        public static ExampleModController Instance { get; private set; }

        // NetId of the most recent KNetInstantiate spawn, for the F7 registry lookup.
        private static int _lastSpawnedNetId;

        private ExampleModController()
        {
        }

        public static void Install()
        {
            if (Instance != null)
                return;

            var go = new GameObject("ExampleMod_Controller");
            DontDestroyOnLoad(go);
            go.AddComponent<ExampleModController>();
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            if (!SessionInfoAPI.InSession)
                return;

            if (Input.GetKeyDown(KeyCode.F5))
            {
                var prefab = Assets.GetPrefab("Hatch");
                if (prefab == null)
                {
                    Debug.LogWarning("[ExampleMod] F5: could not find the Hatch prefab.");
                }
                else
                {
                    var go = EntityExamples.SpawnPrefab(prefab, ResolveSpawnPosition());
                    if (go != null)
                        _lastSpawnedNetId = NetIdentityHelper.AddOrGetNetId(go);
                }
            }

            if (Input.GetKeyDown(KeyCode.F6))
            {
                var go = EntityExamples.SpawnResource(SimHashes.IronOre, ResolveSpawnPosition(), 100f, 293.15f);
                if (go != null)
                    _lastSpawnedNetId = NetIdentityHelper.AddOrGetNetId(go);
            }

            if (Input.GetKeyDown(KeyCode.F7))
            {
                if (_lastSpawnedNetId == 0)
                {
                    Debug.Log("[ExampleMod] F7: spawn something first (F5 or F6).");
                }
                else
                {
                    EntityExamples.TryLookup(_lastSpawnedNetId, out _);
                    EntityExamples.TryLookupComponent<PrimaryElement>(_lastSpawnedNetId, out _);
                }
            }

            if (Input.GetKeyDown(KeyCode.F8))
            {
                if (ExampleSyncSpawner.TryGet(out _))
                {
                    ExampleSyncSpawner.Despawn();
                    Debug.Log("[ExampleMod] F8: removed the sync entity");
                }
                else
                {
                    Debug.Log("[ExampleMod] F8: no sync entity to remove");
                }
            }

            if (Input.GetKeyDown(KeyCode.F9))
            {
                PacketExamples.SendToAllOtherPeers($"Hello at {Time.unscaledTime:F1}s");
                Debug.Log("[ExampleMod] F9: sent hello via all-other-peers");
            }

            if (Input.GetKeyDown(KeyCode.F10))
            {
                PacketExamples.DumpSessionInfo();
            }

            if (Input.GetKeyDown(KeyCode.F11))
            {
                ExampleSyncSpawner.Spawn();
            }

            if (Input.GetKeyDown(KeyCode.F12))
            {
                if (ExampleSyncSpawner.TryGet(out var behaviour))
                {
                    behaviour.RequestIncrement();
                    Debug.Log("[ExampleMod] F12: requested counter increment");
                }
                else
                {
                    Debug.Log("[ExampleMod] F12: spawn the sync entity first (F11)");
                }
            }
        }

        /// <summary>
        /// Prefers the host's cursor position; falls back to the camera when it is unavailable.
        /// </summary>
        private static Vector3 ResolveSpawnPosition()
        {
            if (SessionInfoAPI.TryGetPlayerCursorPos(SessionInfoAPI.HostUserID, out var cursor))
                return cursor;

            var cam = Camera.main;
            return cam != null ? cam.ScreenToWorldPoint(Input.mousePosition) : Vector3.zero;
        }
    }
}
