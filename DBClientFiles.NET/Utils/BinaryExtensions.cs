﻿using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace DBClientFiles.NET.Utils
{
    [SuppressUnmanagedCodeSecurity]
    internal static unsafe class BinaryReaderExtensions
    {
        /// <summary>
        ///     Reads a generic structure from the current stream.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="br"></param>
        /// <returns></returns>
        public static T ReadStruct<T>(this BinaryReader br) where T : struct
        {
            if (SizeCache<T>.TypeRequiresMarshal)
            {
                var ptr = Marshal.AllocHGlobal(SizeCache<T>.Size);
                Marshal.Copy(br.ReadBytes(SizeCache<T>.Size), 0, ptr, SizeCache<T>.Size);
                var mret = Marshal.PtrToStructure<T>(ptr);
                Marshal.FreeHGlobal(ptr);
                return mret;
            }

            // OPTIMIZATION!
            var dataBytes = br.ReadBytes(SizeCache<T>.Size);
            var dataSpan = new Span<byte>(dataBytes);
            var structSpan = MemoryMarshal.Cast<byte, T>(dataSpan);
            return structSpan[0];
        }

        public static T[] ReadStructs<T>(this BinaryReader br, int count)
        {
            if (SizeCache<T>.TypeRequiresMarshal)
            {
                var ptr = Marshal.AllocHGlobal(SizeCache<T>.Size * count);
                Marshal.Copy(br.ReadBytes(SizeCache<T>.Size * count), 0, ptr, SizeCache<T>.Size * count);
                var arr = new T[count];
                // Unfortunate part of the marshaler, is that each instance needs to be pulled in separately.
                // Can't just do a bulk memcpy.
                for (var i = 0; i < count; i++)
                {
                    arr[i] = Marshal.PtrToStructure<T>(ptr + (SizeCache<T>.Size * i));
                }
                Marshal.FreeHGlobal(ptr);
                return arr;
            }
 
            if (count == 0)
            {
                return new T[0];
            }
 
            var ret = new T[count];
            fixed (byte* pB = br.ReadBytes(SizeCache<T>.Size * count))
            {
                var genericPtr = (byte*)SizeCache<T>.GetUnsafePtr(ref ret[0]);
                UnsafeNativeMethods.MoveMemory(genericPtr, pB, SizeCache<T>.Size * count);
            }
            return ret;
        }
    }
}
