using System;
using System.Reflection;
using UnityEngine;
using HarmonyLib;
using KMod;
using ONI_Together_API;
using ONI_Together_API.Networking;

namespace ExampleMod
{
    /// <summary>
    /// Entry point for the example ONI Together API mod.
    ///
    /// This mod is safe to enable without ONI Together installed: every API call is guarded so
    /// the mod simply does nothing when the multiplayer mod is absent.
    /// </summary>
    public class ExampleMod : UserMod2
    {
        public static Harmony Harmony;
        public static bool ApiAvailable;

        public override void OnLoad(Harmony harmony)
        {
            Harmony = harmony;
            base.OnLoad(harmony);

            Debug.Log("[ExampleMod] OnLoad");
        }

        /// <summary>
        /// Called once all mods are loaded. This is the earliest safe point to register packets,
        /// because the ONI Together main mod type must exist first (see PacketRegistryAPI docs).
        /// </summary>
        public override void OnAllModsLoaded(Harmony harmony, System.Collections.Generic.IReadOnlyList<Mod> mods)
        {
            base.OnAllModsLoaded(harmony, mods);

            if (!MP_Mod_Info.MultiplayerModPresent)
            {
                Debug.Log("[ExampleMod] ONI Together not detected - running in single-player no-op mode.");
                return;
            }

            ApiAvailable = true;

            // Register every IPacket implemented in this assembly with the networking layer.
            PacketRegistryAPI.AutoRegisterAll(Assembly.GetExecutingAssembly());

            // Spawn the runtime controller that drives the examples (hotkeys, OxySync spawning, etc).
            ExampleModController.Install();

            Debug.Log("[ExampleMod] Registered example packets and spawned controller.");
        }
    }
}
