using System.Collections.Generic;
using ONI_Together.Networking;
using ONI_Together_API;
using ONI_Together_API.Networking;
using UnityEngine;

namespace ExampleMod.Networking
{
    /// <summary>
    /// Shows every <see cref="PacketSenderAPI"/> overload and how to gate on
    /// <see cref="SessionInfoAPI"/>. None of these are wired to hotkeys - they exist as a
    /// reference you can call from your own code.
    /// </summary>
    public static class PacketExamples
    {
        /// <summary>Send to everyone (optionally excluding one player). Host-only API.</summary>
        public static void SendToEveryone(string message)
        {
            if (!SessionInfoAPI.IsHost)
            {
                Debug.LogWarning("[ExampleMod] SendToEveryone requires host.");
                return;
            }

            var packet = new ExampleHelloPacket(SessionInfoAPI.LocalUserID, message);
            PacketSenderAPI.SendToAll(packet, exclude: null, PacketSendMode.Reliable);
        }

        /// <summary>Host -> all clients (excludes the host automatically).</summary>
        public static void SendToAllClients(string message)
        {
            if (!SessionInfoAPI.IsHost) return;

            var packet = new ExampleHelloPacket(SessionInfoAPI.LocalUserID, message);
            PacketSenderAPI.SendToAllClients(packet, PacketSendMode.Reliable);
        }

        /// <summary>Host -> all clients except a set of players (e.g. the ones already told).</summary>
        public static void SendToAllExcluding(string message, HashSet<ulong> excluded)
        {
            if (!SessionInfoAPI.IsHost) return;

            var packet = new ExampleHelloPacket(SessionInfoAPI.LocalUserID, message);
            PacketSenderAPI.SendToAllExcluding(packet, excluded, PacketSendMode.Reliable);
        }

        /// <summary>Send to one specific player by userId. Host-only.</summary>
        public static void SendToPlayer(ulong userId, string message)
        {
            if (!SessionInfoAPI.IsHost) return;

            var packet = new ExampleHelloPacket(SessionInfoAPI.LocalUserID, message);
            PacketSenderAPI.SendToPlayer(userId, packet, PacketSendMode.ReliableImmediate);
        }

        /// <summary>Client -> host. Also usable by the host as a no-op guard.</summary>
        public static void SendToHost(string message)
        {
            if (!SessionInfoAPI.InSession) return;

            var packet = new ExampleHelloPacket(SessionInfoAPI.LocalUserID, message);
            PacketSenderAPI.SendToHost(packet, PacketSendMode.ReliableImmediate);
        }

        /// <summary>
        /// Send to all other peers regardless of role: the host broadcasts to clients, a client
        /// wraps it and asks the host to rebroadcast.
        /// </summary>
        public static void SendToAllOtherPeers(string message)
        {
            if (!SessionInfoAPI.InSession) return;

            var packet = new ExampleHelloPacket(SessionInfoAPI.LocalUserID, message);
            PacketSenderAPI.SendToAllOtherPeers(packet);
        }

        /// <summary>
        /// Example of reading session state. TryGetPlayerCursorPos/Color let you query other
        /// players without knowing the networking internals.
        /// </summary>
        public static void DumpSessionInfo()
        {
            if (!SessionInfoAPI.InSession)
            {
                Debug.Log("[ExampleMod] Not in a session.");
                return;
            }

            Debug.Log($"[ExampleMod] Local={SessionInfoAPI.LocalUserID} Host={SessionInfoAPI.HostUserID} " +
                      $"(isHost={SessionInfoAPI.IsHost}, isClient={SessionInfoAPI.IsClient})");

            if (SessionInfoAPI.TryGetPlayerCursorPos(SessionInfoAPI.HostUserID, out var hostCursor))
                Debug.Log($"[ExampleMod] Host cursor at {hostCursor}");

            if (SessionInfoAPI.TryGetPlayerColor(SessionInfoAPI.HostUserID, out var hostColor))
                Debug.Log($"[ExampleMod] Host color {hostColor}");
        }
    }
}
