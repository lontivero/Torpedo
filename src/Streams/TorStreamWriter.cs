using System;
using System.IO;

namespace Torpedo;

public class TorStreamWriter
{
    private readonly int _protocolVersion;
    private readonly BEBinaryWriter _writer;

    public TorStreamWriter(Stream stream, int protocolVersion)
    {
        _protocolVersion = protocolVersion;
        _writer = new BEBinaryWriter(stream);
    }

    public void Write(ICell cell)
    {
        switch (cell)
        {
            case Create2Cell create2Cell: Write(create2Cell); break;
            case Created2Cell created2Cell: Write(created2Cell); break;
            case NetInfoCell netInfoCell: Write(netInfoCell); break;
            case VersionsCell versionsCell: Write(versionsCell); break;
            default: throw new NotImplementedException();
        }    
    }
    
    public void Write(Create2Cell cell)
    {
        WriteCircuitId(cell.CircuitId);
        Write(CommandType.Create2);
        var pos = _writer.BaseStream.Position;
        _writer.Write((short)cell.HandshakeType);
        _writer.WriteVariableBytes(cell.Handshake);
        FillLength(pos);
    }

    public void Write(Created2Cell cell)
    {
        WriteCircuitId(cell.CircuitId);
        Write(CommandType.Created2);

        var length = 32 + cell.Auth.Length;

        var pos = _writer.BaseStream.Position;
        _writer.Write((short)length);
        _writer.Write(cell.Y.EncodePoint());
        _writer.Write(cell.Auth);
        FillLength(pos);
    }

    public void Write(NetInfoCell cell)
    {
        WriteCircuitId(cell.CircuitId);
        Write(CommandType.NetInfo);
        
        var pos = _writer.BaseStream.Position;
        _writer.Write((uint)cell.Timestamp.ToUnixTimeSeconds());
        _writer.Write(cell.MyIPAddress);
        _writer.Write((byte)cell.OtherIPs.Count);
        foreach(var remote in cell.OtherIPs)
        {
            _writer.Write(remote);
        }
        FillLength(pos);
    }

    public void Write(VersionsCell cell)
    {
        WriteCircuitId(cell.CircuitId);
        Write(CommandType.Versions);
        _writer.Write((ushort)(cell.Versions.Count * sizeof(ushort)));
            
        foreach(var ver in cell.Versions)
        {
            _writer.Write(ver);
        }
    }
    
    private void FillLength(long pos)
    {
        var messageSize = (int)(_writer.BaseStream.Position - pos);
        var buffer = DummyBuffer.AsSpan(messageSize);
        _writer.Write(buffer);
    }

    private void WriteCircuitId(uint circuitId)
    {
        if(_protocolVersion >= 4)
        {
            _writer.Write((uint)circuitId);
        }
        else
        {
            _writer.Write((ushort)circuitId);
        }
    }

    private void Write(CommandType command)
    {
        _writer.Write((byte)command);
    }
    
    private static readonly byte[] DummyBuffer = new byte[Constants.MaxPayloadSize];
}