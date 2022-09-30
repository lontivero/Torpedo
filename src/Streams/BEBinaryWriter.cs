using System;
using System.IO;
using System.Text;

namespace Torpedo;

class BEBinaryWriter : BinaryWriter
{
    public BEBinaryWriter(Stream output) : base(output, Encoding.ASCII, leaveOpen: true)
    {
    }

    public override void Write(short value)
    {
        WriteByteArray(BitConverter.GetBytes(value));
    }

    public override void Write(int value)
    {
        WriteByteArray(BitConverter.GetBytes(value));
    }
    public override void Write(long value)
    {
        WriteByteArray(BitConverter.GetBytes(value));
    }

    public override void Write(ushort value)
    {
        WriteByteArray(BitConverter.GetBytes(value));
    }

    public override void Write(uint value)
    {
        WriteByteArray(BitConverter.GetBytes(value));
    }

    public override void Write(ulong value)
    {
        WriteByteArray(BitConverter.GetBytes(value));
    }

    public override void Write(byte[] buffer)
    {
        base.Write(buffer);
    }

    private void WriteByteArray(byte[] data)
    {
        Array.Reverse(data);
        base.Write(data);
    }
}