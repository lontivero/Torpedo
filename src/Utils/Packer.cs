using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;

namespace Torpedo
{
	public abstract class Packer
	{
		private static readonly Packer SwapConv = new SwapConverter();
		private static readonly Packer CopyConv = new CopyConverter();

		public static Packer LittleEndian => BitConverter.IsLittleEndian 
			? CopyConv 
			: SwapConv;

		public static Packer BigEndian => BitConverter.IsLittleEndian 
			? SwapConv 
			: CopyConv;

		public static Packer Native => CopyConv;

		public static readonly bool IsLittleEndian = BitConverter.IsLittleEndian;

		public bool ToBoolean(byte[] value, int offset) => BitConverter.ToBoolean(value, offset);
		public char ToChar(byte[] value, int offset)    =>	unchecked((char)(FromBytes(value, offset, sizeof(char))));
		public short ToInt16(byte[] value, int offset)  => unchecked((short)(FromBytes(value, offset, sizeof(short))));
		public int ToInt32(byte[] value, int offset)    => unchecked((int)(FromBytes(value, offset, sizeof(int))));
		public long ToInt64(byte[] value, int offset)   => 	FromBytes(value, offset, sizeof(long));
		public ushort ToUInt16(byte[] value, int offset)=> unchecked((ushort)(FromBytes(value, offset, sizeof(ushort))));
		public uint ToUInt32(byte[] value, int offset)  => unchecked((uint)(FromBytes(value, offset, sizeof(uint))));
		public ulong ToUInt64(byte[] value, int offset) => unchecked((ulong)(FromBytes(value, offset, sizeof(ulong))));

		private byte[] GetBytes(long value, int bytes)
		{
			var buffer = new byte[bytes];
			CopyBytes(value, bytes, buffer, 0);
			return buffer;
		}

		private byte[] GetBytes(byte[] value, int bytes)
		{
			var buffer = new byte[bytes];
			CopyBytes(value, 0, buffer, 0, bytes);
			return buffer;
		}

		public byte[] GetBytes(bool value)   => BitConverter.GetBytes(value);
		public byte[] GetBytes(char value)   => GetBytes(value, sizeof(char));
		public byte[] GetBytes(short value)  => GetBytes(value, sizeof(short));
		public byte[] GetBytes(int value)    => GetBytes(value, sizeof(int));
		public byte[] GetBytes(long value)   => GetBytes(value, sizeof(long));
		public byte[] GetBytes(ushort value) => GetBytes(value, sizeof(ushort));
		public byte[] GetBytes(uint value)   => GetBytes(value, sizeof(uint));
		public byte[] GetBytes(ulong value)  => GetBytes(unchecked((long)value), sizeof(ulong));
		public byte[] GetBytes(byte[] value) => GetBytes(value, value.Length);

		protected abstract long FromBytes(byte[] value, int offset, int count);
		protected abstract void CopyBytes(long value, int bytes, byte[] buffer, int index);
		protected abstract void CopyBytes(byte[] from, int fromOffset, byte[] to, int toOffset, int count);

		private static int Align(int current, int align) => ((current + align - 1) / align) * align;

		private class PackContext
		{
			private byte[] _buffer;
			private int _next;

			internal string description;
			internal int i; // position in the description
			internal Packer conv;
			internal int repeat;

			private ArrayPool<byte> _byteArrayPool;

			public PackContext()
			{
				_byteArrayPool = ArrayPool<byte>.Create();
				_buffer = ArrayPool<byte>.Shared.Rent(1024);
				_next = 0;
			}

			public void Add(byte[] group)
			{
				if (_next + group.Length > _buffer.Length)
				{
					var nextBuffer = ArrayPool<byte>.Shared.Rent(Math.Max(_next, 16) * 2 + group.Length);
					Buffer.BlockCopy(_buffer, 0, nextBuffer, 0, _buffer.Length);
					Buffer.BlockCopy(group, 0, nextBuffer, _next, group.Length);
					ArrayPool<byte>.Shared.Return(_buffer);
					_buffer = nextBuffer;
					_next = _next + group.Length;
				}
				else
				{
					Buffer.BlockCopy(group, 0, _buffer, _next, group.Length);
					_next += group.Length;
				}
			}

			public byte[] Get()
			{
				var ret = new byte[_next];
				Buffer.BlockCopy(_buffer, 0, ret, 0, _next);
				return ret;
			}
		}

		//
		// Format includes:
		// Control:
		//   ^    Switch to big endian encoding
		//   _    Switch to little endian encoding
		//   %    Switch to host (native) encoding
		//   !    aligns the next data type to its natural boundary (for strings this is 4).
		//
		// Types:
		//   s    Int16
		//   S    UInt16
		//   i    Int32
		//   I    UInt32
		//   l    Int64
		//   L    UInt64
		//   f    float
		//   d    double
		//   b    byte
		//   c    1-byte signed character
		//   C    1-byte unsigned character
		//   A    byte array
		//   z8   string encoded as UTF8 with 1-byte null terminator
		//   z6   string encoded as UTF16 with 2-byte null terminator
		//   z7   string encoded as UTF7 with 1-byte null terminator
		//   zb   string encoded as BigEndianUnicode with 2-byte null terminator
		//   z3   string encoded as UTF32 with 4-byte null terminator
		//   z4   string encoded as UTF32 big endian with 4-byte null terminator
		//   $8   string encoded as UTF8
		//   $6   string encoded as UTF16
		//   $7   string encoded as UTF7
		//   $b   string encoded as BigEndianUnicode
		//   $3   string encoded as UTF32
		//   $4   string encoded as UTF-32 big endian encoding
		//   x    null byte
		//
		// Repeats, these are prefixes:
		//   N    a number between 1 and 9, indicates a repeat count (process N items
		//        with the following datatype
		//   [N]  For numbers larger than 9, use brackets, for example [20]
		//   *    Repeat the next data type until the arguments are exhausted
		//
		public static byte[] Pack(string description, params object[] args)
		{
			int argn = 0;
			var b = new PackContext();
			b.conv = CopyConv;
			b.description = description;

			for (b.i = 0; b.i < description.Length; )
			{
				object oarg;

				if (argn < args.Length)
				{
					oarg = args[argn];
				}
				else
				{
					if (b.repeat != 0)
						break;

					oarg = null;
				}

				int save = b.i;

				if (PackOne(b, oarg))
				{
					argn++;
					if (b.repeat > 0)
					{
						if (--b.repeat > 0)
							b.i = save;
						else
							b.i++;
					}
					else
						b.i++;
				}
				else
					b.i++;
			}
			return b.Get();
		}

		//
		// Packs one datum `oarg' into the buffer `b', using the string format
		// in `description' at position `i'
		//
		// Returns: true if we must pick the next object from the list
		//
		private static bool PackOne(PackContext b, object oarg)
		{
			int n;

			switch (b.description[b.i])
			{
				case '^':
					b.conv = BigEndian;
					return false;

				case '_':
					b.conv = LittleEndian;
					return false;

				case '%':
					b.conv = Native;
					return false;

				case '!':
					return false;

				case 'x':
					b.Add(new byte[] { 0 });
					return false;

				// Type Conversions
				case 'i':
					b.Add(b.conv.GetBytes(Convert.ToInt32(oarg)));
					break;

				case 'I':
					b.Add(b.conv.GetBytes(Convert.ToUInt32(oarg)));
					break;

				case 's':
					b.Add(b.conv.GetBytes(Convert.ToInt16(oarg)));
					break;

				case 'S':
					b.Add(b.conv.GetBytes(Convert.ToUInt16(oarg)));
					break;

				case 'l':
					b.Add(b.conv.GetBytes(Convert.ToInt64(oarg)));
					break;

				case 'L':
					b.Add(b.conv.GetBytes(Convert.ToUInt64(oarg)));
					break;

				case 'b':
					b.Add(new[] { Convert.ToByte(oarg) });
					break;

				case 'c':
					b.Add(new[] { (byte)(Convert.ToSByte(oarg)) });
					break;

				case 'C':
					b.Add(new[] { Convert.ToByte(oarg) });
					break;

				case 'A':
					b.Add(b.conv.GetBytes((byte[])oarg));
					break;

				// Repeat acount;
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
					b.repeat = ((short)b.description[b.i]) - ((short)'0');
					return false;

				case '*':
					b.repeat = Int32.MaxValue;
					return false;

				case '[':
					int count = -1, j;

					for (j = b.i + 1; j < b.description.Length; j++)
					{
						if (b.description[j] == ']')
							break;
						n = ((short)b.description[j]) - ((short)'0');
						if (n >= 0 && n <= 9)
						{
							if (count == -1)
								count = n;
							else
								count = count * 10 + n;
						}
					}
					if (count == -1)
						throw new ArgumentException("invalid size specification");
					b.i = j;
					b.repeat = count;
					return false;

				case '$':
				case 'z':
					var add_null = b.description[b.i] == 'z';
					b.i++;
					if (b.i >= b.description.Length)
						throw new ArgumentException("$ description needs a type specified", "description");
					char d = b.description[b.i];
					System.Text.Encoding e;

					switch (d)
					{
						case '8':
							e = System.Text.Encoding.UTF8;
							n = 1;
							break;
						case '6':
							e = System.Text.Encoding.Unicode;
							n = 2;
							break;
						case '7':
							e = System.Text.Encoding.UTF7;
							n = 1;
							break;
						case 'b':
							e = System.Text.Encoding.BigEndianUnicode;
							n = 2;
							break;
						case '3':
							e = System.Text.Encoding.GetEncoding(12000);
							n = 4;
							break;
						case '4':
							e = System.Text.Encoding.GetEncoding(12001);
							n = 4;
							break;

						default:
							throw new ArgumentException("Invalid format for $ specifier", "description");
					}
					b.Add(e.GetBytes(Convert.ToString(oarg)));
					if (add_null)
						b.Add(new byte[n]);
					break;
				default:
					throw new ArgumentException($"invalid format specified '{b.description[b.i]}'", "description");
			}
			return true;
		}

		private static bool Prepare(byte[] buffer, ref int idx, int size, ref bool align)
		{
			if (align)
			{
				idx = Align(idx, size);
				align = false;
			}
			if (idx + size > buffer.Length)
			{
				idx = buffer.Length;
				return false;
			}
			return true;
		}

		public static IList Unpack(string description, byte[] buffer, int startIndex)
		{
			Packer conv = CopyConv;
			var result = new List<object>();
			var idx = startIndex;
			var align = false;
			var repeat = 0;

			for (var i = 0; i < description.Length && idx < buffer.Length; )
			{
				var save = i;

				int n;
				switch (description[i])
				{
					case '^':
						conv = BigEndian;
						break;
					case '_':
						conv = LittleEndian;
						break;
					case '%':
						conv = Native;
						break;
					case 'x':
						idx++;
						break;

					case '!':
						align = true;
						break;

					// Type Conversions
					case 'i':
						if (Prepare(buffer, ref idx, 4, ref align))
						{
							result.Add(conv.ToInt32(buffer, idx));
							idx += 4;
						}
						break;

					case 'I':
						if (Prepare(buffer, ref idx, 4, ref align))
						{
							result.Add(conv.ToUInt32(buffer, idx));
							idx += 4;
						}
						break;

					case 's':
						if (Prepare(buffer, ref idx, 2, ref align))
						{
							result.Add(conv.ToInt16(buffer, idx));
							idx += 2;
						}
						break;

					case 'S':
						if (Prepare(buffer, ref idx, 2, ref align))
						{
							result.Add(conv.ToUInt16(buffer, idx));
							idx += 2;
						}
						break;

					case 'l':
						if (Prepare(buffer, ref idx, 8, ref align))
						{
							result.Add(conv.ToInt64(buffer, idx));
							idx += 8;
						}
						break;

					case 'L':
						if (Prepare(buffer, ref idx, 8, ref align))
						{
							result.Add(conv.ToUInt64(buffer, idx));
							idx += 8;
						}
						break;

					case 'b':
						if (Prepare(buffer, ref idx, 1, ref align))
						{
							result.Add(buffer[idx]);
							idx++;
						}
						break;

					case 'c':
					case 'C':
						if (Prepare(buffer, ref idx, 1, ref align))
						{
							char c;

							if (description[i] == 'c')
								c = ((char)((sbyte)buffer[idx]));
							else
								c = ((char)(buffer[idx]));

							result.Add(c);
							idx++;
						}
						break;
					case 'A':
						if (Prepare(buffer, ref idx, buffer.Length, ref align))
						{
							result.Add(buffer);
							idx += buffer.Length;
						}
						break;

					// Repeat acount;
					case '1':
					case '2':
					case '3':
					case '4':
					case '5':
					case '6':
					case '7':
					case '8':
					case '9':
						repeat = (short)description[i] - (short)'0';
						save = i + 1;
						break;

					case '*':
						repeat = int.MaxValue;
						break;

					case '[':
						int count = -1, j;

						for (j = i + 1; j < description.Length; j++)
						{
							if (description[j] == ']')
								break;
							n = ((short)description[j]) - ((short)'0');
							if (n < 0 || n > 9) continue;
							if (count == -1)
								count = n;
							else
								count = count * 10 + n;
						}
						if (count == -1)
							throw new ArgumentException("invalid size specification");
						i = j;
						save = i + 1;
						repeat = count;
						break;

					case '$':
					case 'z':
						// bool with_null = description [i] == 'z';
						i++;
						if (i >= description.Length)
							throw new ArgumentException("$ description needs a type specified", nameof(description));
						char d = description[i];
						System.Text.Encoding e;
						if (align)
						{
							idx = Align(idx, 4);
							align = false;
						}
						if (idx >= buffer.Length)
							break;

						switch (d)
						{
							case '8':
								e = System.Text.Encoding.UTF8;
								n = 1;
								break;
							case '6':
								e = System.Text.Encoding.Unicode;
								n = 2;
								break;
							case '7':
								e = System.Text.Encoding.UTF7;
								n = 1;
								break;
							case 'b':
								e = System.Text.Encoding.BigEndianUnicode;
								n = 2;
								break;
							case '3':
								e = System.Text.Encoding.GetEncoding(12000);
								n = 4;
								break;
							case '4':
								e = System.Text.Encoding.GetEncoding(12001);
								n = 4;
								break;

							default:
								throw new ArgumentException("Invalid format for $ specifier", nameof(description));
						}
						int k = idx;
						switch (n)
						{
							case 1:
								for (; k < buffer.Length && buffer[k] != 0; k++)
								{
								}
								result.Add(e.GetChars(buffer, idx, k - idx));
								if (k == buffer.Length)
									idx = k;
								else
									idx = k + 1;
								break;

							case 2:
								for (; k < buffer.Length; k++)
								{
									if (k + 1 == buffer.Length)
									{
										k++;
										break;
									}
									if (buffer[k] == 0 && buffer[k + 1] == 0)
										break;
								}
								result.Add(e.GetChars(buffer, idx, k - idx));
								if (k == buffer.Length)
									idx = k;
								else
									idx = k + 2;
								break;

							case 4:
								for (; k < buffer.Length; k++)
								{
									if (k + 3 >= buffer.Length)
									{
										k = buffer.Length;
										break;
									}
									if (buffer[k] == 0 && buffer[k + 1] == 0 && buffer[k + 2] == 0 && buffer[k + 3] == 0)
										break;
								}
								result.Add(e.GetChars(buffer, idx, k - idx));
								if (k == buffer.Length)
									idx = k;
								else
									idx = k + 4;
								break;
						}
						break;
					default:
						throw new ArgumentException($"invalid format specified '{description[i]}'", nameof(description));
				}

				if (repeat > 0)
				{
					if (--repeat > 0)
						i = save;
				}
				else
					i++;
			}
			return result;
		}

		private class CopyConverter : Packer
		{
			protected override long FromBytes(byte[] value, int offset, int count)
			{
				long ret = 0;
				for (var i = 0; i < count; i++)
				{
					ret = unchecked((ret << 8) | value[offset + count - 1 - i]);
				}
				return ret;
			}

			protected override void CopyBytes(long value, int bytes, byte[] buffer, int index)
			{
				for (var i = 0; i < bytes; i++)
				{
					buffer[i + index] = unchecked((byte)(value & 0xff));
					value = value >> 8;
				}
			}

			protected override void CopyBytes(byte[] from, int fromOffset, byte[] to, int toOffset, int count)
			{
				Buffer.BlockCopy(from, fromOffset, to, toOffset, count);
			}
		}

		private class SwapConverter : Packer
		{
			protected override long FromBytes(byte[] buffer, int offset, int count)
			{
				long ret = 0;
				for (var i = 0; i < count; i++)
				{
					ret = unchecked((ret << 8) | buffer[offset + i]);
				}
				return ret;
			}

			protected override void CopyBytes(long value, int bytes, byte[] buffer, int index)
			{
				var endOffset = index + bytes - 1;
				for (var i = 0; i < bytes; i++)
				{
					buffer[endOffset - i] = unchecked((byte)(value & 0xff));
					value = value >> 8;
				}
			}

			protected override void CopyBytes(byte[] from, int fromOffset, byte[] to, int toOffset, int count)
			{
				Buffer.BlockCopy(from, fromOffset, to, toOffset, count);
				Array.Reverse(to);
			}
		}
	}
}