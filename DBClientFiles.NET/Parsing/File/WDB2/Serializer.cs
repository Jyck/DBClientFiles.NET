﻿using DBClientFiles.NET.Collections;
using DBClientFiles.NET.Parsing.Binding;
using DBClientFiles.NET.Parsing.File.Records;
using DBClientFiles.NET.Parsing.Reflection;
using DBClientFiles.NET.Parsing.Serialization;
using System.Linq.Expressions;

namespace DBClientFiles.NET.Parsing.File.WDB2
{
    internal sealed class Serializer<T> : BaseSerializer<T>
    {
        public Serializer(StorageOptions options, TypeInfo typeInfo) : base(options, typeInfo)
        {
        }

        /// <summary>
        /// WDB2 deserilization is trivial. There is no packing and everything is aligned to 4-byte boundaries.
        /// </summary>
        /// <param name="memberAccess"></param>
        /// <param name="memberInfo"></param>
        /// <param name="recordReader"></param>
        /// <returns></returns>
        /// <remarks>
        /// <para>
        /// For array types:
        /// <list type="bullet">
        ///     <item>For arrays of primitives or strings</item>
        ///     <description>
        ///     We chain the call to <see cref="IRecordReader.ReadArray{T}"/> or <see cref="IRecordReader.ReadStringArray(int)"/>
        ///     if the type is either primitive, or a string.
        ///     </description>
        ///
        ///     <item>For arrays of value or reference types.</item>
        ///     <description>
        ///     We just create the array itself, and move on (<c>new T[...]</c>).
        ///     </description>
        /// </list>
        /// </para>
        /// <para>
        /// For regular types, this is all of the above, except value or references are a no-op, and methods become
        /// <see cref="IRecordReader.Read{T}"/> and <see cref="IRecordReader.ReadString"/>, respectively.
        /// </para>
        /// </remarks>
        public override Expression VisitNode(Expression memberAccess, Member memberInfo, Expression recordReader)
        {
            if (memberInfo.Type.Type.IsArray)
            {
                var elementType = memberInfo.Type.Type.GetElementType();
                if (elementType.IsPrimitive)
                {
                    // ReadArray<T>(...);
                    return Expression.Call(recordReader,
                        _IRecordReader.ReadArray.MakeGenericMethod(elementType),
                        Expression.Constant(memberInfo.Cardinality));
                }
                else if (elementType == typeof(string))
                {
                    // ReadStringArray(...)
                    return Expression.Call(recordReader,
                        _IRecordReader.ReadStringArray,
                        Expression.Constant(memberInfo.Cardinality));
                }

                return null;
            }

            if (memberInfo.Type.Type.IsPrimitive)
            {
                // Read<T>();
                return Expression.Call(recordReader, _IRecordReader.Read.MakeGenericMethod(memberInfo.Type.Type));
            }
            else if (memberInfo.Type.Type == typeof(string))
            {
                // ReadString();
                return Expression.Call(recordReader, _IRecordReader.ReadString);
            }

            return null;
        }
    }
}
