using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;

namespace Torpedo;

public class TorSreamReader
{
    private readonly int _protocolVersion;
    private readonly BEBinaryReader _reader;

    public TorSreamReader(Stream stream, int protocolVersion)
    {
        _protocolVersion = protocolVersion;
        _reader = new BEBinaryReader(stream);
    }

    public ICell ReadCell()
    {
        var circuitId = (_protocolVersion >= 4) 
            ? _reader.ReadUInt32()
            : _reader.ReadUInt16();

        var command = (CommandType)_reader.ReadByte();
        return command switch
        {
            CommandType.Padding => ReadPaddingCell(circuitId),
            CommandType.Create  => null,
            CommandType.Created => null,
            CommandType.Relay   => null,
            CommandType.Destroy => null,
            CommandType.CreateFast  => null,
            CommandType.CreatedFast => null,
            CommandType.NetInfo     => ReadNetInfoCell(circuitId),
            CommandType.RelayEarly  => null,
            CommandType.Create2  => null,
            CommandType.Created2 => ReadCreated2Cell(circuitId),
            CommandType.Versions => ReadVersionsCell(circuitId),
            CommandType.VPadding => null,
            CommandType.Certs => ReadCertsCell(circuitId),
            CommandType.AuthChallenge => ReadAuthChallengeCell(circuitId),
            CommandType.Authenticate => null,
            _ => throw new InvalidOperationException($"Unknown command {command} received.")
        };
    }

    private AuthChallengeCell ReadAuthChallengeCell(uint circuitId)
    {
        var payloadLength = _reader.ReadUInt16();
        var challenge = _reader.ReadBytes(payloadLength); // ignore it;
        return new AuthChallengeCell(circuitId, challenge);
    }

    private CertsCell ReadCertsCell(uint circuitId)
    {
        var payloadLength = _reader.ReadUInt16();
        var certs = _reader.ReadBytes(payloadLength);
        return new CertsCell(circuitId, certs);
    }

    private VersionsCell ReadVersionsCell(uint circuitId)
    {
        var payloadLength = _reader.ReadUInt16();
        var verCount = payloadLength / sizeof(ushort);
        var versions = Enumerable.Range(0, verCount).Select(_ => _reader.ReadUInt16()).ToImmutableList();
        return new VersionsCell(circuitId, versions);
    }

    private Created2Cell ReadCreated2Cell(uint circuitId)
    {
        var payload = _reader.ReadBytes(Constants.MaxPayloadSize);
        var len = _reader.ReadUInt16();
        var y = Ed25519Point.DecodePoint(_reader.ReadBytes(32));
        var auth = _reader.ReadBytes(len - 32);
        return new Created2Cell(circuitId, y, auth);
    }

    private NetInfoCell ReadNetInfoCell(uint circuitId)
    {
        var pos = _reader.BaseStream.Position;
        var timestamp = DateTimeOffset.FromUnixTimeSeconds(_reader.ReadUInt32());
        var myIPAddress = _reader.ReadIPAddress();
        var numOfAddresses  = _reader.ReadByte();
        var othersIP = Enumerable
            .Range(0, numOfAddresses)
            .Select(_ => _reader.ReadIPAddress())
            .TakeWhile(x => x is { })
            .ToImmutableList();
        _reader.BaseStream.Seek(Constants.MaxPayloadSize - pos, SeekOrigin.Current);
        return new NetInfoCell(circuitId, timestamp, myIPAddress, othersIP);
    }

    private PaddingCell ReadPaddingCell(uint circuitId)
    {
        var payloadLength = _reader.ReadUInt16();
        _reader.BaseStream.Seek(payloadLength, SeekOrigin.Current); // ignore it;
        return new PaddingCell(circuitId);
    }
}