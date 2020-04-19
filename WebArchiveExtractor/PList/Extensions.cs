﻿//-----------------------------------------------------------------------
// <copyright file="Extensions.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Linq;

// ReSharper disable CompareOfFloatsByEqualityOperator

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Extensions and helpers for plist serialization.
    /// </summary>
    internal static class Extensions
    {
        #region GetConcreteTypeIfNullable
        /// <summary>
        ///     Gets the specified type's concrete type of it is an instance of <see cref="Nullable{T}" />.
        ///     If the type is not null-able, it is returned as-is.
        /// </summary>
        /// <param name="type">The type to get the concrete type of.</param>
        /// <returns>The type's concrete type.</returns>
        public static Type GetConcreteTypeIfNullable(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type), "type cannot be null.");

            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
                return type.GetGenericArguments()[0];

            return type;
        }
        #endregion

        #region IsAscii
        /// <summary>
        ///     Gets a value indicating whether the given string is all ASCII.
        /// </summary>
        /// <param name="value">The string to check.</param>
        /// <returns>True if the string contains only ASCII characters, false otherwise.</returns>
        public static bool IsAscii(this string value)
        {
            return string.IsNullOrEmpty(value) || value.All(c => c <= 127);
        }
        #endregion

        #region IsCollection
        /// <summary>
        ///     Gets a value indicating whether the specified type is a collection type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is a collection type, false otherwise.</returns>
        public static bool IsCollection(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type), "type cannot be null.");

            return (typeof(Array).IsAssignableFrom(type)
                    || typeof(IEnumerable).IsAssignableFrom(type))
                   && !typeof(string).IsAssignableFrom(type)
                   && !typeof(byte[]).IsAssignableFrom(type);
        }
        #endregion

        #region IsDefaultValue
        /// <summary>
        ///     Gets a value indicating whether the given value is the default value for the specified type.
        /// </summary>
        /// <param name="type">The type to check the value against.</param>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is the default value, false otherwise.</returns>
        public static bool IsDefaultValue(this Type type, object value)
        {
            if (type == null) throw new ArgumentNullException(nameof(type), "type cannot be null.");

            var typeCode = Type.GetTypeCode(type);

            if (typeCode != TypeCode.Empty && typeCode != TypeCode.Object && value == null)
                throw new ArgumentException("Cannot pass a null value when the specified type is non-nullable.",
                    nameof(value));

            if (!type.IsInstanceOfType(value))
                throw new ArgumentException("The specified object value is not assignable to the specified type.",
                    nameof(value));

            switch (Type.GetTypeCode(type))
            {
                case TypeCode.Boolean:
                    return (bool) value == false;
                case TypeCode.Byte:
                case TypeCode.Char:
                case TypeCode.Int16:
                case TypeCode.Int32:
                case TypeCode.Int64:
                case TypeCode.SByte:
                case TypeCode.UInt16:
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return (long) value == 0;
                case TypeCode.DateTime:
                    return (DateTime) value == DateTime.MinValue;
                case TypeCode.DBNull:
                    return true;
                case TypeCode.Decimal:
                case TypeCode.Double:
                case TypeCode.Single:
                    return (double) value == 0;
                default:
                    return value == null;
            }
        }
        #endregion

        #region IsPrimitiveOrEnum
        /// <summary>
        ///     Gets a value indicating whether the specified type is an enum or primitive or semi-primitive (e.g., string) type.
        /// </summary>
        /// <param name="type">The type to check.</param>
        /// <returns>True if the type is an enum or primitive type, false otherwise.</returns>
        public static bool IsPrimitiveOrEnum(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type), "type cannot be null.");

            if (type.IsEnum || Type.GetTypeCode(type) != TypeCode.Object || typeof(Guid).IsAssignableFrom(type) ||
                typeof(TimeSpan).IsAssignableFrom(type) || typeof(byte[]).IsAssignableFrom(type) ||
                typeof(Uri).IsAssignableFrom(type)) return true;
            var concrete = type.GetConcreteTypeIfNullable();

            var result = concrete != type && IsPrimitiveOrEnum(concrete);

            return result;
        }
        #endregion

        #region ToBinaryString
        /// <summary>
        ///     Converts the given value into its binary representation as a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The value's binary representation as a string.</returns>
        public static string ToBinaryString(this byte value)
        {
            return Convert.ToString(value, 2);
        }

        /// <summary>
        ///     Converts the given value into its binary representation as a string.
        /// </summary>
        /// <param name="value">The value to convert.</param>
        /// <returns>The value's binary representation as a string.</returns>
        public static string ToBinaryString(this int value)
        {
            return Convert.ToString(value, 2);
        }
        #endregion
    }
}