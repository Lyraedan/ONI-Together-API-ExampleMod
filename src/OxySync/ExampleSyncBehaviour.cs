using ONI_Together_API;
using ONI_Together.Networking;
using Shared.OxySync;
using Shared.OxySync.Attributes;
using UnityEngine;

namespace ExampleMod.OxySync
{
    /// <summary>
    /// Demonstrates an OxySync <see cref="NetworkBehaviour"/>: SyncVars (auto + hooked),
    /// Commands (client -> host), ClientRpcs (host -> clients), TargetRpcs (host -> one client),
    /// and role-gated methods.
    /// </summary>
    public class ExampleSyncBehaviour : NetworkBehaviour
    {
        // Sent every tick if changed. SendMode 0 == PacketSendMode.Unreliable (default).
        [SyncVar]
        private float _temperature = 25f;

        // Hook fires on the receiving side when the value changes.
        [SyncVar(Hook = nameof(OnCounterChanged), SendMode = (int) PacketSendMode.Reliable)]
        private int _counter;

        // A server-authoritative string.
        [SyncVar(Hook = nameof(OnLabelChanged))]
        private string _label = "Hello";

        // Epsilon controls how much a float may move before it is considered changed.
        [SyncVar(Epsilon = 0.05f)]
        private float _progress;

        public int Counter => _counter;
        public float Temperature => _temperature;
        public string Label => _label;
        public float Progress => _progress;

        public override void OnSpawn()
        {
            base.OnSpawn();

            // -1 broadcasts to everyone; set a group id to limit who receives updates.
            InterestGroup = -1;
        }

        private void Update()
        {
            // Only the host writes authoritative state; SyncVars replicate it to clients.
            if (!isServer) return;

            _progress = Mathf.PingPong(Time.unscaledTime * 0.1f, 1f);
        }

        // Commands (client -> host)

        [Command]
        public void CmdIncrementCounter()
        {
            _counter++;
            CallClientRpc(RpcOnCounterIncremented, _counter);
        }

        [Command(RequiresHost = true)]
        public void CmdReset()
        {
            _counter = 0;
            _temperature = 25f;
            _label = "Reset";
        }

        [Command(SendMode = (int) PacketSendMode.Unreliable)]
        public void CmdSetTemperature(float value)
        {
            _temperature = value;
        }

        [Command]
        public void CmdSetLabel(string value)
        {
            _label = value;
        }

        // ClientRpcs (host -> clients)
        [ClientRpc]
        private void RpcOnCounterIncremented(int value)
        {
            Debug.Log($"[ExampleMod] Counter incremented to {value}");
        }

        [ClientRpc(IncludeHost = true)]
        private void RpcAnnounce(string message)
        {
            Debug.Log($"[ExampleMod] Announcement: {message}");
        }

        // TargetRpcs (host -> one client)

        [TargetRpc]
        private void TargetSendMessage(ulong player, string message)
        {
            Debug.Log($"[ExampleMod] Direct message for {player}: {message}");
        }

        // SyncVar hooks

        private void OnCounterChanged(int oldValue, int newValue)
        {
            Debug.Log($"[ExampleMod] Counter changed {oldValue} -> {newValue} (isServer={isServer})");
        }

        private void OnLabelChanged(string oldValue, string newValue)
        {
            Debug.Log($"[ExampleMod] Label changed '{oldValue}' -> '{newValue}'");
        }

        // Role-gated methods

        [Server]
        public void ServerOnlyLog()
        {
            Debug.Log("[ExampleMod] This only runs on the host.");
        }

        [Client]
        public void ClientOnlyLog()
        {
            Debug.Log("[ExampleMod] This only runs on a client.");
        }

        // Public API used by the controller

        public void RequestIncrement()
        {
            // Run locally on host, otherwise send a Command to it.
            CallCommand(CmdIncrementCounter);
        }

        public void RequestLabel(string value)
        {
            CallCommand(CmdSetLabel, value);
        }

        public void Announce(string message)
        {
            if (!isServer) return;
            CallClientRpc(RpcAnnounce, message);
        }
    }
}
