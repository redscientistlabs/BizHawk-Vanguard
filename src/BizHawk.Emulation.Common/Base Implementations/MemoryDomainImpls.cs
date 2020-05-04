﻿using BizHawk.Common;
using System;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Common
{
	public class MemoryDomainDelegate : MemoryDomain
	{
		private Action<long, byte> _poke;

		// TODO: use an array of Ranges
		private Action<Range<long>, byte[]> _bulkPeekByte { get; set; }
		private Action<Range<long>, bool, ushort[]> _bulkPeekUshort { get; set; }
		private Action<Range<long>, bool, uint[]> _bulkPeekUint { get; set; }

		public Func<long, byte> Peek { get; set; }

		public Action<long, byte> Poke
		{
			get => _poke;
			set
			{
				_poke = value;
				Writable = value != null;
			}
		}

		public override byte PeekByte(long addr)
		{
			return Peek(addr);
		}

		public override void PokeByte(long addr, byte val)
		{
			_poke?.Invoke(addr, val);
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			if (_bulkPeekByte != null)
			{
				_bulkPeekByte.Invoke(addresses, values);
			}
			else
			{
				base.BulkPeekByte(addresses, values);
			}
		}

		public override void BulkPeekUshort(Range<long> addresses, bool bigEndian, ushort[] values)
		{
			if (_bulkPeekUshort != null)
			{
				_bulkPeekUshort.Invoke(addresses, EndianType == Endian.Big, values);
			}
			else
			{
				base.BulkPeekUshort(addresses, EndianType == Endian.Big, values);
			}
		}

		public override void BulkPeekUint(Range<long> addresses, bool bigEndian, uint[] values)
		{
			if (_bulkPeekUint != null)
			{
				_bulkPeekUint.Invoke(addresses, EndianType == Endian.Big, values);
			}
			else
			{
				base.BulkPeekUint(addresses, EndianType == Endian.Big, values);
			}
		}

		public MemoryDomainDelegate(
			string name,
			long size,
			Endian endian,
			Func<long, byte> peek,
			Action<long, byte> poke,
			int wordSize,
			Action<Range<long>, byte[]> bulkPeekByte = null,
			Action<Range<long>, bool, ushort[]> bulkPeekUshort = null,
			Action<Range<long>, bool, uint[]> bulkPeekUint = null)
		{
			Name = name;
			EndianType = endian;
			Size = size;
			Peek = peek;
			_poke = poke;
			Writable = poke != null;
			WordSize = wordSize;
			_bulkPeekByte = bulkPeekByte;
			_bulkPeekUshort = bulkPeekUshort;
			_bulkPeekUint = bulkPeekUint;
		}
	}

	public class MemoryDomainByteArray : MemoryDomain
	{
		private byte[] _data;

		public byte[] Data
		{
			get => _data;
			set
			{
				_data = value;
				Size = _data.LongLength;
			}
		}

		public override byte PeekByte(long addr)
		{
			return Data[addr];
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				Data[addr] = val;
			}
		}

		public MemoryDomainByteArray(string name, Endian endian, byte[] data, bool writable, int wordSize)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Writable = writable;
			WordSize = wordSize;
		}
	}

	public unsafe class MemoryDomainIntPtr : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				return ((byte*)Data)[addr];
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					((byte*)Data)[addr] = val;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public override void BulkPeekByte(Range<long> addresses, byte[] values)
		{
			var start = (ulong)addresses.Start;
			var count = addresses.Count();

			if (start < (ulong)Size && (start + count) <= (ulong)Size)
			{
				Marshal.Copy((IntPtr)((ulong)Data + start), values, 0, (int)count);
			}
			else
			{
				throw new ArgumentOutOfRangeException(nameof(addresses));
			}
		}

		public void SetSize(long size)
		{
			Size = size;
		}

		public MemoryDomainIntPtr(string name, Endian endian, IntPtr data, long size, bool writable, int wordSize)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = wordSize;
		}
	}

	public unsafe class MemoryDomainIntPtrMonitor : MemoryDomain
	{
		public IntPtr Data { get; set; }
		private readonly IMonitor _monitor;

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					return ((byte*)Data)[addr];
				}
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						((byte*)Data)[addr] = val;
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public void SetSize(long size)
		{
			Size = size;
		}

		public MemoryDomainIntPtrMonitor(string name, Endian endian, IntPtr data, long size, bool writable, int wordSize,
			IMonitor monitor)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = wordSize;
			_monitor = monitor;
		}
	}

	public unsafe class MemoryDomainIntPtrSwap16 : MemoryDomain
	{
		public IntPtr Data { get; set; }

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				return ((byte*)Data)[addr ^ 1];
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					((byte*)Data)[addr ^ 1] = val;
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public MemoryDomainIntPtrSwap16(string name, Endian endian, IntPtr data, long size, bool writable)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = 2;
		}
	}

	public unsafe class MemoryDomainIntPtrSwap16Monitor : MemoryDomain
	{
		public IntPtr Data { get; set; }
		private readonly IMonitor _monitor;

		public override byte PeekByte(long addr)
		{
			if ((ulong)addr < (ulong)Size)
			{
				using (_monitor.EnterExit())
				{
					return ((byte*)Data)[addr ^ 1];
				}
			}

			throw new ArgumentOutOfRangeException(nameof(addr));
		}

		public override void PokeByte(long addr, byte val)
		{
			if (Writable)
			{
				if ((ulong)addr < (ulong)Size)
				{
					using (_monitor.EnterExit())
					{
						((byte*)Data)[addr ^ 1] = val;
					}
				}
				else
				{
					throw new ArgumentOutOfRangeException(nameof(addr));
				}
			}
		}

		public MemoryDomainIntPtrSwap16Monitor(string name, Endian endian, IntPtr data, long size, bool writable,
			IMonitor monitor)
		{
			Name = name;
			EndianType = endian;
			Data = data;
			Size = size;
			Writable = writable;
			WordSize = 2;
			_monitor = monitor;
		}
	}
}
