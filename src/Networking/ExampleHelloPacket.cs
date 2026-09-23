using System.IO;
using ONI_Together.Networking;
using ONI_Together.Networking.Packets.Architecture;
using ONI_Together_API;
using ONI_Together_API.Networking;

namespace ExampleMod.Networking
{
    /// <summary>
    /// A reliable chat-style message packet demonstrating the host-rebroadcast pattern:
    /// clients send it to the host, the host relays it to everyone else.
    ///
    /// IMPORTANT: the packet must have a public parameterless constructor
    /// </summary>
    public class ExampleHelloPacket : IPacket
    {
        public ulong SenderId;
        public string Message;

        public ExampleHelloPacket()
        {
            Message = string.Empty;
        }

        public ExampleHelloPacket(ulong senderId, string message)
        {
            SenderId = senderId;
            Message = message;
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId);
            writer.Write(Message ?? string.Empty);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = reader.ReadUInt64();
            Message = reader.ReadString();
        }

        public void OnDispatched()
        {
            // This runs on every peer that receives the packet.
            string direction = SessionInfoAPI.IsHost ? "host" : "client";
            UnityEngine.Debug.Log($"[ExampleMod] Hello from {SenderId} (received on {direction}): {Message}");

            // Clients relay their own messages through the host, so everybody sees them.
            if (SessionInfoAPI.IsClient)
                PacketSenderAPI.SendToHost(this);
            else if (SessionInfoAPI.IsHost)
                PacketSenderAPI.SendToAllClients(this, PacketSendMode.Reliable);
        }
    }
}
