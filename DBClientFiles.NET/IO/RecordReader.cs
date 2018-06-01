﻿using DBClientFiles.NET.Utils;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace DBClientFiles.NET.IO
{
    /// <summary>
    /// Metainformation for <see cref="RecordReader"/>.
    /// </summary>
    public static class _RecordReader
    {
        private static Type[] arrayArgs            = { typeof(int) };
        private static Type[] arrayPackedArgs      = { typeof(int), typeof(int), typeof(int) };
        private static Type[] packedArgs           = { typeof(int), typeof(int) };

        public static MethodInfo ReadPackedUInt64  = typeof(RecordReader).GetMethod("ReadUInt64", packedArgs);
        public static MethodInfo ReadPackedUInt32  = typeof(RecordReader).GetMethod("ReadUInt32", packedArgs);
        public static MethodInfo ReadPackedUInt16  = typeof(RecordReader).GetMethod("ReadUInt16", packedArgs);
        public static MethodInfo ReadPackedSByte   = typeof(RecordReader).GetMethod("ReadSByte",  packedArgs);

        public static MethodInfo ReadPackedInt64   = typeof(RecordReader).GetMethod("ReadInt64", packedArgs);
        public static MethodInfo ReadPackedInt32   = typeof(RecordReader).GetMethod("ReadInt32", packedArgs);
        public static MethodInfo ReadPackedInt16   = typeof(RecordReader).GetMethod("ReadInt16", packedArgs);
        public static MethodInfo ReadPackedByte    = typeof(RecordReader).GetMethod("ReadByte",  packedArgs);

        public static MethodInfo ReadPackedSingle  = typeof(RecordReader).GetMethod("ReadSingle", new[] { typeof(int) });

        public static MethodInfo ReadPackedString  = typeof(RecordReader).GetMethod("ReadString", packedArgs);
        public static MethodInfo ReadPackedStrings = typeof(RecordReader).GetMethod("ReadStrings", arrayPackedArgs);
        public static MethodInfo ReadPackedArray   = typeof(RecordReader).GetMethod("ReadArray", arrayPackedArgs);

        public static MethodInfo ReadUInt64        = typeof(RecordReader).GetMethod("ReadUInt64", Type.EmptyTypes);
        public static MethodInfo ReadUInt32        = typeof(RecordReader).GetMethod("ReadUInt32", Type.EmptyTypes);
        public static MethodInfo ReadUInt16        = typeof(RecordReader).GetMethod("ReadUInt16", Type.EmptyTypes);
        public static MethodInfo ReadSByte         = typeof(RecordReader).GetMethod("ReadSByte", Type.EmptyTypes);

        public static MethodInfo ReadInt64         = typeof(RecordReader).GetMethod("ReadInt64", Type.EmptyTypes);
        public static MethodInfo ReadInt32         = typeof(RecordReader).GetMethod("ReadInt32", Type.EmptyTypes);
        public static MethodInfo ReadInt16         = typeof(RecordReader).GetMethod("ReadInt16", Type.EmptyTypes);
        public static MethodInfo ReadByte          = typeof(RecordReader).GetMethod("ReadByte", Type.EmptyTypes);

        public static MethodInfo ReadSingle        = typeof(RecordReader).GetMethod("ReadSingle", Type.EmptyTypes);
        public static MethodInfo ReadString        = typeof(RecordReader).GetMethod("ReadString", Type.EmptyTypes);
        public static MethodInfo ReadStrings       = typeof(RecordReader).GetMethod("ReadStrings", arrayArgs);
        public static MethodInfo ReadArray         = typeof(RecordReader).GetMethod("ReadArray", arrayArgs);

        public static Dictionary<TypeCode, MethodInfo> PackedReaders = new Dictionary<TypeCode, MethodInfo>()
        {
            { TypeCode.UInt64, ReadPackedUInt64 },
            { TypeCode.UInt32, ReadPackedUInt32 },
            { TypeCode.UInt16, ReadPackedUInt16 },
            { TypeCode.SByte, ReadPackedSByte },

            { TypeCode.Int64, ReadPackedInt64 },
            { TypeCode.Int32, ReadPackedInt32 },
            { TypeCode.Int16, ReadPackedInt16 },
            { TypeCode.Byte, ReadPackedByte },

            { TypeCode.String, ReadPackedString },
        };

        public static Dictionary<TypeCode, MethodInfo> Readers = new Dictionary<TypeCode, MethodInfo>()
        {
            { TypeCode.UInt64, ReadUInt64 },
            { TypeCode.UInt32, ReadUInt32 },
            { TypeCode.UInt16, ReadUInt16 },
            { TypeCode.SByte, ReadSByte },

            { TypeCode.Int64, ReadInt64 },
            { TypeCode.Int32, ReadInt32 },
            { TypeCode.Int16, ReadInt16 },
            { TypeCode.Byte, ReadByte },

            { TypeCode.String, ReadString },
            { TypeCode.Single, ReadSingle },
        };
    }

    /// <summary>
    /// This class acts as a thing wrapper around the record data for a row. It can read either packed or unpacked elements.
    /// </summary>
    internal unsafe class RecordReader : IDisposable
    {
        private byte[] _recordData;
        private Memory<byte> _recordMemory;
        //private GCHandle _dataHandle;
        //private IntPtr _dataPointer;

        protected int _byteCursor = 0;
    
        public long ReadInt64() => Read<long>(_byteCursor, 64, true);
        public int ReadInt32() => Read<int>(_byteCursor, 32, true);
        public short ReadInt16() => Read<short>(_byteCursor, 16, true);
        public byte ReadByte() => Read<byte>(_byteCursor, 8, true);
        
        public ulong ReadUInt64() => Read<ulong>(_byteCursor, 64, true);
        public uint ReadUInt32() => Read<uint>(_byteCursor, 32, true);
        public ushort ReadUInt16() => Read<ushort>(_byteCursor, 16, true);
        public sbyte ReadSByte() => Read<sbyte>(_byteCursor, 8, true);

        public float ReadSingle() => Read<float>(_byteCursor, 32, true);

        protected FileReader _fileReader;
        protected readonly bool _usesStringTable;

        public int StartOffset { get; }

        public RecordReader(FileReader fileReader, bool usesStringTable, int recordSize)
        {
            StartOffset = (int)fileReader.BaseStream.Position;

            _usesStringTable = usesStringTable;
            _fileReader = fileReader;

            _recordData = fileReader.ReadBytes(recordSize);
            _recordMemory = new Memory<byte>(_recordData);


            //_dataHandle = GCHandle.Alloc(_recordData, GCHandleType.Pinned);
            //_dataPointer = _dataHandle.AddrOfPinnedObject();
        }

        public void Dispose()
        {
            // _dataHandle.Free();

            _fileReader = null;
            _recordData = null;
        }

        public long ReadInt64(int bitOffset, int bitCount)
        {
            if (bitCount <= 32)
                return ReadInt32(bitOffset, bitCount);

            _byteCursor = bitOffset + bitCount;

            var longValue = Read<long>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 64)
                longValue &= (1L << bitCount) - 1;

            return longValue;
        }

        public ulong ReadUInt64(int bitOffset, int bitCount)
        {
            if (bitCount <= 32)
                return ReadUInt32(bitOffset, bitCount);

            _byteCursor = bitOffset + bitCount;

            var longValue = Read<ulong>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 64)
                longValue &= (1uL << bitCount) - 1;

            return longValue;
        }

        public int ReadInt32(int bitOffset, int bitCount)
        {
            if (bitCount <= 16)
                return ReadInt16(bitOffset, bitCount);

            _byteCursor = bitOffset + bitCount;

            var intValue = Read<int>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 32)
                intValue &= (1 << bitCount) - 1;

            return intValue;
        }

        public uint ReadUInt32(int bitOffset, int bitCount)
        {
            if (bitCount <= 16)
                return ReadUInt16(bitOffset, bitCount);

            _byteCursor = bitOffset + bitCount;

            var intValue = Read<uint>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 32)
                intValue &= (1u << bitCount) - 1;

            return intValue;
        }

        public short ReadInt16(int bitOffset, int bitCount)
        {
            if (bitCount <= 8)
                return ReadSByte(bitOffset, bitCount);

            _byteCursor = bitOffset + bitCount;

            var shortValue = Read<short>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 16)
                shortValue &= (1 << bitCount) - 1;

            return (short)shortValue;
        }

        public ushort ReadUInt16(int bitOffset, int bitCount)
        {
            if (bitCount <= 8)
                return ReadByte(bitOffset, bitCount);

            _byteCursor = bitOffset + bitCount;

            var shortValue = Read<short>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 16)
                shortValue &= (1 << bitCount) - 1;

            return (ushort)shortValue;
        }

        public byte ReadByte(int bitOffset, int bitCount)
        {
            _byteCursor = bitOffset + bitCount;

            var byteValue = Read<byte>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 8)
                byteValue &= (1 << bitCount) - 1;

            return (byte)byteValue;
        }

        public sbyte ReadSByte(int bitOffset, int bitCount)
        {
            _byteCursor = bitOffset + bitCount;

            var byteValue = Read<sbyte>(bitOffset, bitCount) >> (bitOffset & 7);
            if (bitCount != 8)
                byteValue &= (1 << bitCount) - 1;

            return (sbyte)byteValue;
        }

        public float ReadSingle(int bitOffset)
        {
            _byteCursor += 32;
            return Read<float>(bitOffset, 32);
        }

        /// <remarks>
        /// While this may look fine, it will return a value that will be unaccurate unless properly shifted to the right by <code><paramref name="bitOffset"/> & 7</code>, as this cannot be typically done by this method.
        /// </remarks>
        private T Read<T>(int bitOffset, int bitCount, bool advanceCursor = false) where T : struct
        {
            //! TODO: This needs to be more robust: Ideally speaking, the size check condition needs to go.
            //! TODO: Unfortunately, some types make be less bytes wide than SizeCache<T>.Size but still
            //! TODO: larger than the next smaller primitive type. We would need int24, int40, int48, int56...

            if (advanceCursor)
                _byteCursor += SizeCache<T>.Size * 8;

            var spanSlice = _recordMemory.Slice(bitOffset / 8, (bitCount + (bitOffset & 7) + 7) / 8);
            if (SizeCache<T>.Size > spanSlice.Length)
            {
                using (var sliceHandle = spanSlice.Pin())
                    return FastStructure.PtrToStructure<T>(new IntPtr(sliceHandle.Pointer));
            }

            var typeMemory = MemoryMarshal.Cast<byte, T>(spanSlice.Span);
            return typeMemory[0];

            //T v = FastStructure.PtrToStructure<T>(IntPtr.Add(_dataPointer, bitOffset / 8));

            //return v;
        }

        /// <summary>
        /// Reads a string from the record.
        /// </summary>
        /// <returns></returns>
        public virtual string ReadString()
        {
            if (_usesStringTable)
                return _fileReader.FindStringByOffset(ReadInt32());

            return _fileReader.ReadString();
        }

        /// <summary>
        /// Reads a string from the record, given the provided bit offset and length.
        /// </summary>
        /// <exception cref="InvalidOperationException">This exception is thrown when the caller tries to read a packed string offset in a file which does not have a string table.</exception>
        /// <param name="bitOffset">The absolute offset in the structure, in bits, at which the string is located.</param>
        /// <param name="bitCount">The amount of bits in which the offset to the string is contained.</param>
        /// <returns></returns>
        public virtual string ReadString(int bitOffset, int bitCount)
        {
            if (_usesStringTable)
                return _fileReader.FindStringByOffset(ReadInt32(bitOffset, bitCount));

            if ((bitOffset & 7) == 0)
                return _fileReader.ReadString();

            throw new InvalidOperationException("Packed strings must be in the string block!");
        }
        
        public long ReadBits(int bitOffset, int bitCount)
        {
            var byteOffset = bitOffset / 8;
            var byteCount = (bitCount + (bitOffset & 7) + 7) / 8;
            
            var value = 0L;
            for (var i = 0; i < byteCount; ++i)
                value |= (long)(_recordData[i + byteOffset] << (8 * i));

            value = (value >> (bitOffset & 7));

            // Prevent possible masking overflows from clamping the actual result.
            if (bitCount != 64)
                value &= ((1L << bitCount) - 1);

            return value;
        }

        public T[] ReadArray<T>(int arraySize, int bitOffset, int bitCount) where T : struct
        {
            var arr = new T[arraySize];
            for (var i = 0; i < arraySize; ++i)
                arr[i] = Read<T>(bitOffset + i * SizeCache<T>.Size, bitCount);
            return arr;
        }

        public T[] ReadArray<T>(int arraySize) where T : struct
        {
            var arr = new T[arraySize];
            for (var i = 0; i < arraySize; ++i)
                arr[i] = Read<T>(_byteCursor, SizeCache<T>.Size, true);
            // _byteCursor += SizeCache<T>.Size * 8 * arraySize;
            return arr;
        }

        public string[] ReadStrings(int arraySize, int bitOffset, int bitCount)
        {
            var arr = new string[arraySize];
            for (var i = 0; i < arraySize; ++i)
                arr[i] = ReadString(bitOffset + i * 4, bitCount);
            return arr;
        }

        public string[] ReadStrings(int arraySize)
        {
            var arr = new string[arraySize];
            for (var i = 0; i < arraySize; ++i)
                arr[i] = ReadString();
            return arr;
        }

    }
}
