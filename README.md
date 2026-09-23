# ONI Together API Example Mod

A minimal, buildable third-party mod demonstrating the **ONI Together API**: custom network
packets, session info, and an OxySync `NetworkBehaviour`.

## What it demonstrates

| File | Shows |
| --- | --- |
| `src/ExampleMod.cs` | `UserMod2` entry point, detection via `MP_Mod_Info.MultiplayerModPresent`, packet registration via `PacketRegistryAPI.AutoRegisterAll` |
| `src/ExampleModController.cs` | A persistent `MonoBehaviour` that drives the examples via hotkeys |
| `src/Networking/ExampleHelloPacket.cs` | A reliable packet using the host-rebroadcast relay pattern |
| `src/Networking/ExamplePingPacket.cs` | An unreliable, high-frequency packet (cell + colour) |
| `src/Networking/PacketExamples.cs` | Every `PacketSenderAPI` overload + `SessionInfoAPI` gating/querying |
| `src/OxySync/ExampleSyncBehaviour.cs` | `[SyncVar]` (+ hook / epsilon / send-mode), `[Command]`, `[ClientRpc]`, `[TargetRpc]`, `[Server]`/`[Client]` |
| `src/OxySync/ExampleSyncSpawner.cs` | Spawning/removing a synced entity and assigning a NetId via `NetIdentityHelper` |
| `src/World/EntityExamples.cs` | `SpawnUtilsAPI.KNetInstantiate` (prefab + element), `NetIdentityHelper` NetId helpers, and `NetworkIdentityRegistryAPI` lookups |

## In-game hotkeys

The controller only reacts while in a multiplayer session (`SessionInfoAPI.InSession`).

| Key | Action |
| --- | --- |
| `F5` | Spawn a `Hatch` prefab via `KNetInstantiate` (host only) |
| `F6` | Spawn an `IronOre` resource via `KNetInstantiate` (host only) |
| `F7` | Look up the last spawned NetId via `NetworkIdentityRegistryAPI` |
| `F8` | Remove the OxySync demo behaviour (if spawned) |
| `F9` | Send a hello packet to all other peers (`PacketExamples.SendToAllOtherPeers`) |
| `F10` | Dump session info (ids + host cursor/colour) |
| `F11` | Spawn the OxySync demo behaviour |
| `F12` | Increment the OxySync counter via a `[Command]` (requires `F11` first) |

> `ExamplePingPacket` and the other `PacketExamples` overloads are reference code and are **not**
> bound to keys. Call them from your own code to try them.

## OxySync demo behaviour

`ExampleSyncBehaviour` exercises the main OxySync features:

- **SyncVars**: `_temperature` (unreliable), `_counter` (reliable + change hook), `_label`
  (hooked string), and `_progress`/`PingPong` (epsilon-gated, written by the host every frame for
  a continuously-replicating value).
- **Commands** (client → host): `CmdIncrementCounter`, `CmdReset` (`RequiresHost`), and
  `CmdSetTemperature`/`CmdSetLabel`. `RequestIncrement()`/`RequestLabel(...)` run locally on the
  host or send a Command otherwise.
- **ClientRpcs** (host → clients): `RpcOnCounterIncremented`, and `RpcAnnounce`
  (`IncludeHost = true`).
- **TargetRpcs** (host → one client): `TargetSendMessage`.
- **Role gating**: `[Server]` `ServerOnlyLog()` and `[Client]` `ClientOnlyLog()`.

Because the host writes `_progress` every frame, the entity shows as continuously "Syncing" in the
main mod's dev tool. That is intentional for the demo; make the value change rarely (or not at all)
if you want an idle, at-rest entity.

## Spawning and looking up networked entities

`src/World/EntityExamples.cs` wraps the two host-only `SpawnUtilsAPI.KNetInstantiate` overloads and
the `NetworkIdentityRegistryAPI` lookups.

Only the host may spawn; both methods return `null` on a client (or when ONI Together is absent) and
replicate the spawn to every client automatically.

```csharp
// Prefab overload - any prefab from the game database.
GameObject hatch = EntityExamples.SpawnPrefab(Assets.GetPrefab("Hatch"), position);

// Element/resource overload - mass, temperature and disease data are preserved.
GameObject ore = EntityExamples.SpawnResource(SimHashes.IronOre, position, 100f, 293.15f);
```

> **Position:** `SpawnPrefab` snaps the position to the prefab's cell and scene layer. Cursor/session
> positions (e.g. from `SessionInfoAPI.TryGetPlayerCursorPos`) sit on the UI plane, and spawning a
> prefab at that z places it at the wrong depth (often invisible). `SpawnResource` already snaps
> internally.

To query an entity later you need its **NetId**. `Shared.Helpers.NetIdentityHelper` exposes three
helpers for that:

| Method | Behaviour |
| --- | --- |
| `AddNetId(go)` | Adds a network identity if the object lacks one, registers it if it has no id, and returns the NetId. |
| `GetNetId(go)` | Read-only: returns the object's existing NetId, or `0` if it has none. Never creates/registers. |
| `AddOrGetNetId(go, preferredId = 0)` | Combines the two; a non-zero `preferredId` overrides the assigned id. |

`KNetInstantiate` already assigns an identity, so read it back with `GetNetId`:

```csharp
GameObject go = EntityExamples.SpawnResource(SimHashes.IronOre, position, 100f, 293.15f);
int netId = NetIdentityHelper.GetNetId(go);

if (NetworkIdentityRegistryAPI.TryGet(netId, out GameObject found))
    Debug.Log($"Found {found.name}");

if (NetworkIdentityRegistryAPI.TryGetComponent<PrimaryElement>(netId, out var primaryElement))
    Debug.Log($"{primaryElement.ElementID}: {primaryElement.Mass}kg");
```

For a GameObject you created yourself, call `AddNetId` first; `GetNetId` alone will return `0`
because no identity exists yet:

```csharp
var go = new GameObject("MySyncedThing");
int netId = NetIdentityHelper.AddNetId(go);        // assign + register
// ... later ...
int again = NetIdentityHelper.GetNetId(go);        // same id, no side effects

// Or honour a preferred/deterministic id:
int preferred = NetIdentityHelper.AddOrGetNetId(go, 12345);
```

`EntityExamples.EnsureNetId` / `EntityExamples.ReadNetId` wrap these calls with logging.

`EntityExamples.SpawnResourceAndInspect(...)` performs that whole spawn → NetId → lookup loop in one
call. The `F5`/`F6`/`F7` hotkeys run the prefab spawn, the element spawn, and the registry lookup
respectively.

## Building

Requirements:
- .NET SDK (this project was built with 8.0)
- Oxygen Not Included installed
- A self-contained (ILRepacked) `ONI_Together_API.dll`, produced by building the API in **Release**:
  ```
  dotnet build ONI_Together_API/ONI_Together_API.csproj -c Release
  ```

Paths are configured in `Directory.Build.props`. To override them on your machine, copy
`Directory.Build.props.user.template` to `Directory.Build.props.user` and set:

- `GameLibsFolder` — your `OxygenNotIncluded_Data/Managed` folder
- `ModFolder` — your ONI dev mods folder
- `ApiDllPath` - path to your copy of the `ONI_Together_API.dll`

Then:

```
dotnet build ExampleMod.csproj
```

The build generates `mod.yaml` / `mod_info.yaml` and deploys `ExampleMod.dll`, its pdb, and
`ONI_Together_API.dll` into `<ModFolder>/ExampleMod_dev`.

## Namespaces cheat-sheet

Types come from two places, which can be confusing because the API DLL embeds `Shared`:

| Type | Namespace | Assembly |
| --- | --- | --- |
| `MP_Mod_Info`, `SessionInfoAPI`, `PacketSenderAPI`, `PacketRegistryAPI` | `ONI_Together_API[.Networking]` | `ONI_Together_API` |
| `IPacket` | `ONI_Together.Networking.Packets.Architecture` | `ONI_Together_API` (merged `Shared`) |
| `PacketSendMode` | `ONI_Together.Networking` | `ONI_Together_API` (merged `Shared`) |
| `NetworkBehaviour` | `Shared.OxySync` | `ONI_Together_API` (merged `Shared`) |
| OxySync attributes | `Shared.OxySync.Attributes` | `ONI_Together_API` (merged `Shared`) |
| `NetIdentityHelper` | `Shared.Helpers` | `ONI_Together_API` (merged `Shared`) |

## Writing packets

API packets are duck-typed by the main mod and wrapped in `ModApiPacket<T>`. Therefore:

- Your packet must implement `ONI_Together.Networking.Packets.Architecture.IPacket`.
- It **must have a public parameterless constructor** — the main mod instantiates it with
  `Activator.CreateInstance<T>()`.

```csharp
public class MyPacket : IPacket
{
    public int Value;
    public MyPacket() { }              // required
    public void Serialize(BinaryWriter w) => w.Write(Value);
    public void Deserialize(BinaryReader r) => Value = r.ReadInt32();
    public void OnDispatched() { /* handle on receive */ }
}
```

Register all packets once, after all mods are loaded (the main mod type must exist first):

```csharp
public override void OnAllModsLoaded(Harmony harmony, IReadOnlyList<Mod> mods)
{
    if (MP_Mod_Info.MultiplayerModPresent)
        PacketRegistryAPI.AutoRegisterAll(Assembly.GetExecutingAssembly());
}
```

### Send modes

`PacketSendMode` (from `ONI_Together.Networking`) selects the delivery profile:

- `Reliable` — guaranteed, ordered (state changes, chat).
- `ReliableImmediate` — reliable, flushed immediately (latency-sensitive events).
- `Unreliable` — best-effort (high-frequency position/effects).

## OxySync notes

`[SyncVar]` / `[Command]` / `[ClientRpc]` / `[TargetRpc]` behaviour is provided by
`Shared.OxySync.NetworkBehaviour`, which is ILRepacked into this mod's copy of `ONI_Together_API.dll`.
Because the main mod does not reference the API, those types have a different runtime identity than
the main mod's own copy; the main mod discovers and bridges each consuming mod's API copy at load.
This works as long as ONI Together is present. If ONI Together is absent, OxySync calls are inert.

`NetworkIdentity` belongs to the main mod and is **not** part of the API surface — don't add it
directly. Use `Shared.Helpers.NetIdentityHelper.AddNetId(gameObject)` to assign a NetId,
`GetNetId(gameObject)` to read one, or `AddOrGetNetId(gameObject, preferredId)` for both.

## Limitations

- The examples use `[Command]`/`[ClientRpc]` argument types supported by `RpcSerializer`
  (primitives, `string`, `Vector2/3`, `Color`, arrays/lists of those). Unsupported types throw
  at send time.
- `F11`/`F12` require ONI Together and an active session to do anything meaningful.
- Dev tools (the OxySync inspector) only exist in a Debug build of the main mod.
