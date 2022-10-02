using System;
using System.Collections.Immutable;
using System.IO;
using System.Net;

namespace Torpedo;

public static class Constants
{
    public const int FixLengthCellSize = 514;
    public const int MaxPayloadSize = 509;
}

public interface ICell
{}

public static class CellExtensions 
{
    public static byte[] ToByteArray(this ICell cell, ushort protocolVersion)
    {
        using MemoryStream mem = new();
        TorStreamWriter writer = new(mem, protocolVersion);
        writer.Write(cell);
        return mem.ToArray();
    }
}

public enum HandshakeType
{
    Tap = 1,
    NTor = 2
};

public record Create2Cell(uint CircuitId, HandshakeType HandshakeType, byte[] Handshake) : ICell;

public record Created2Cell(uint CircuitId, Ed25519Point Y, byte[] Auth) : ICell;

public record NetInfoCell(uint CircuitId, DateTimeOffset Timestamp, IPAddress MyIPAddress, ImmutableList<IPAddress> OtherIPs) : ICell;

public record PaddingCell(uint CircuitId) : ICell;

public record AuthChallengeCell(uint CircuitId, byte[] Challenge) : ICell;

public record CertsCell(uint CircuitId, byte[] Certs) : ICell
{
    public byte[] Certs { get; set; } = Certs;
}

public record VersionsCell(uint CircuitId, ImmutableList<ushort> Versions) : ICell 
{
    public VersionsCell(uint circuitId, params ushort[] versions)
        : this(circuitId, ImmutableList.Create(versions))
    {
    }
}
