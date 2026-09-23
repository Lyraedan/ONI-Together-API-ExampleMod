using System.IO;
using ONI_Together.Networking;
using ONI_Together.Networking.Packets.Architecture;
using ONI_Together_API;
using ONI_Together_API.Networking;
using UnityEngine;

namespace ExampleMod.Networking
{
    /// <summary>
    /// A high-frequency, unreliable "ping" at a world cell - the same delivery profile you would
    /// use for transient effects, cursors, or position snapshots where stale data is useless.
    ///
    /// Clients send it up; the host rebroadcasts to the other clients.
    /// </summary>
    public class ExamplePingPacket : IPacket
    {
        public ulong SenderId;
        public int Cell;
        public byte R;
        public byte G;
        public byte B;

        public ExamplePingPacket()
        {
        }

        public ExamplePingPacket(ulong senderId, int cell, Color color)
        {
            SenderId = senderId;
            Cell = cell;
            R = (byte)(Mathf.Clamp01(color.r) * 255f);
            G = (byte)(Mathf.Clamp01(color.g) * 255f);
            B = (byte)(Mathf.Clamp01(color.b) * 255f);
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(SenderId);
            writer.Write(Cell);
            writer.Write(R);
            writer.Write(G);
            writer.Write(B);
        }

        public void Deserialize(BinaryReader reader)
        {
            SenderId = reader.ReadUInt64();
            Cell = reader.ReadInt32();
            R = reader.ReadByte();
            G = reader.ReadByte();
            B = reader.ReadByte();
        }

        public void OnDispatched()
        {
            // Host receives pings from clients and passes them on to the other clients.
            if (SessionInfoAPI.IsHost && SenderId != SessionInfoAPI.HostUserID)
                PacketSenderAPI.SendToAllClients(this, PacketSendMode.Unreliable);

            var color = new Color32(R, G, B, 255);
            var pos = Grid.CellToPosCCC(Cell, Grid.SceneLayer.Move);
            Debug.Log($"[ExampleMod] Ping from {SenderId} at cell {Cell} {pos} color {color}");
        }
    }
}
