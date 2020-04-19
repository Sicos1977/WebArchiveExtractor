﻿//-----------------------------------------------------------------------
// <copyright file="BinaryPlistWriter.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable IntroduceOptionalParameters.Global

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Performs serialization of objects into binary plist format.
    /// </summary>
    public sealed class BinaryPlistWriter
    {
        #region Fields
        /// <summary>
        ///     Gets the magic number value used in a binary plist header.
        /// </summary>
        internal const uint HeaderMagicNumber = 0x62706c69;

        /// <summary>
        ///     Gets the version number value used in a binary plist header.
        /// </summary>
        internal const uint HeaderVersionNumber = 0x73743030;

        /// <summary>
        ///     Gets Apple's reference date value.
        /// </summary>
        internal static readonly DateTime ReferenceDate = new DateTime(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private List<BinaryPlistItem> _objectTable;
        private List<long> _offsetTable;
        private UniqueValueCache _uniques;
        private int _objectTableSize, _objectRefCount, _objectRefSize, _topLevelObjectOffset;
        private long _maxObjectRefValue;
        #endregion

        #region WriteObject
        /// <summary>
        ///     Writes the specified <see cref="IPlistSerializable" /> object to the given file path as a binary plist.
        /// </summary>
        /// <param name="path">The file path to write to.</param>
        /// <param name="obj">The <see cref="IPlistSerializable" /> object to write.</param>
        public void WriteObject(string path, IPlistSerializable obj)
        {
            using (var stream = File.Create(path))
            {
                WriteObject(stream, obj);
            }
        }

        /// <summary>
        ///     Writes the specified <see cref="IPlistSerializable" /> object to the given stream as a binary plist.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="obj">The <see cref="IPlistSerializable" /> object to write.</param>
        public void WriteObject(Stream stream, IPlistSerializable obj)
        {
            WriteObject(stream, obj, true);
        }

        /// <summary>
        ///     Writes the specified <see cref="IPlistSerializable" /> object to the given stream as a binary plist.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="obj">The <see cref="IPlistSerializable" /> object to write.</param>
        /// <param name="closeStream">A value indicating whether to close the stream after the write operation completes.</param>
        public void WriteObject(Stream stream, IPlistSerializable obj, bool closeStream)
        {
            if (obj == null) throw new ArgumentNullException(nameof(obj), "obj cannot be null.");

            WriteObject(stream, obj.ToPlistDictionary(), closeStream);
        }

        /// <summary>
        ///     Writes the specified <see cref="IDictionary" /> object to the given file path as a binary plist.
        /// </summary>
        /// <param name="path">The file path to write to.</param>
        /// <param name="dictionary">The <see cref="IDictionary" /> object to write.</param>
        public void WriteObject(string path, IDictionary dictionary)
        {
            using (var stream = File.Create(path))
            {
                WriteObject(stream, dictionary);
            }
        }

        /// <summary>
        ///     Writes the specified <see cref="IDictionary" /> object to the given stream as a binary plist.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="dictionary">The <see cref="IDictionary" /> object to write.</param>
        public void WriteObject(Stream stream, IDictionary dictionary)
        {
            WriteObject(stream, dictionary, true);
        }

        /// <summary>
        ///     Writes the specified <see cref="IDictionary" /> object to the given stream as a binary plist.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="dictionary">The <see cref="IDictionary" /> object to write.</param>
        /// <param name="closeStream">A value indicating whether to close the stream after the write operation completes.</param>
        public void WriteObject(Stream stream, IDictionary dictionary, bool closeStream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream), "stream cannot be null.");

            if (!stream.CanWrite) throw new ArgumentException("The stream must be writable.", nameof(stream));

            if (dictionary == null) throw new ArgumentNullException(nameof(dictionary), "dictionary cannot be null.");

            // Reset the state and then build the object table.
            Reset();
            AddDictionary(dictionary);

            _topLevelObjectOffset = 8;
            CalculateObjectRefSize();

            var writer = new BinaryWriter(stream);

            try
            {
                // Write the header.
                writer.Write(HeaderMagicNumber.ToBigEndianConditional());
                writer.Write(HeaderVersionNumber.ToBigEndianConditional());

                // Write the object table.
                long offsetTableOffset = _topLevelObjectOffset + WriteObjectTable(writer);

                // Write the offset table.
                foreach (var l in _offsetTable)
                {
                    var offset = (int) l;
                    WriteReferenceInteger(writer, offset, _objectRefSize);
                }

                // Write the trailer.
                writer.Write(new byte[6], 0, 6);
                writer.Write((byte) _objectRefSize);
                writer.Write((byte) _objectRefSize);
                writer.Write(((long) _objectTable.Count).ToBigEndianConditional());
                writer.Write((long) 0);
                writer.Write(offsetTableOffset.ToBigEndianConditional());
            }
            finally
            {
                writer.Flush();

                if (closeStream) writer.Close();
            }
        }
        #endregion

        #region AddIntegerCount
        /// <summary>
        ///     Adds an integer count to the given buffer.
        /// </summary>
        /// <param name="buffer">The buffer to add the integer count to.</param>
        /// <param name="count">A count value to write.</param>
        private static void AddIntegerCount(ICollection<byte> buffer, int count)
        {
            var countBuffer = GetIntegerBytes(count);

            // According to my inspection of the output of Property List Editor's .plist files,
            // it is marking the most significant bit for some unknown reason. So we're marking it too.
            buffer.Add((byte) ((byte) Math.Log(countBuffer.Length, 2) | 0x10));

            foreach (var countByte in countBuffer) buffer.Add(countByte);
        }
        #endregion

        #region GetIntegerBytes
        /// <summary>
        ///     Gets a big-endian byte array that corresponds to the given integer value.
        /// </summary>
        /// <param name="value">The integer value to get bytes for.</param>
        /// <returns>A big-endian byte array.</returns>
        private static byte[] GetIntegerBytes(long value)
        {
            // See AddIntegerCount() for why this is restricting use
            // of the most significant bit.
            if (value >= 0 && value < 128)
                return new[] {(byte) value};

            if (value >= short.MinValue && value <= short.MaxValue)
                return BitConverter.GetBytes(((short) value).ToBigEndianConditional());

            if (value >= int.MinValue && value <= int.MaxValue)
                return BitConverter.GetBytes(((int) value).ToBigEndianConditional());

            return BitConverter.GetBytes(value.ToBigEndianConditional());
        }
        #endregion

        #region WriteReferenceInteger
        /// <summary>
        ///     Writes the given value using the number of bytes indicated by the specified size
        ///     to the given <see cref="BinaryWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="value">The value to write.</param>
        /// <param name="size">The size of the integer to write.</param>
        /// <returns>The number of bytes written.</returns>
        private static int WriteReferenceInteger(BinaryWriter writer, long value, int size)
        {
            byte[] buffer;

            switch (size)
            {
                case 1:
                    buffer = new[] {(byte) value};
                    break;
                case 2:
                    buffer = BitConverter.GetBytes(((short) value).ToBigEndianConditional());
                    break;
                case 4:
                    buffer = BitConverter.GetBytes(((int) value).ToBigEndianConditional());
                    break;
                case 8:
                    buffer = BitConverter.GetBytes(value.ToBigEndianConditional());
                    break;
                default:
                    throw new ArgumentException(
                        "The reference size must be one of 1, 2, 4 or 8. The specified reference size was: " + size,
                        nameof(size));
            }

            writer.Write(buffer, 0, buffer.Length);
            return buffer.Length;
        }
        #endregion

        #region AddArray
        /// <summary>
        ///     Adds an array to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddArray(IEnumerable value)
        {
            var index = _objectTable.Count;

            var array = new BinaryPlistArray(_objectTable);
            var item = new BinaryPlistItem(array) {IsArray = true};
            _objectTable.Add(item);

            foreach (var obj in value)
            {
                array.ObjectReference.Add(AddObject(obj));
                _objectRefCount++;
            }

            if (array.ObjectReference.Count < 15)
            {
                item.Marker.Add((byte) (0xA0 | (byte) array.ObjectReference.Count));
            }
            else
            {
                item.Marker.Add(0xAF);
                AddIntegerCount(item.Marker, array.ObjectReference.Count);
            }

            _objectTableSize += item.Size;
            return index;
        }
        #endregion

        #region AddData
        /// <summary>
        ///     Adds arbitrary data to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddData(object value)
        {
            var index = _objectTable.Count;
            var bufferIndex = 0;

            if (!(value is byte[] buffer))
                using (var stream = new MemoryStream())
                {
                    var formatter = new BinaryFormatter();
                    formatter.Serialize(stream, value);

                    stream.Position = 0;
                    buffer = new byte[stream.Length];

                    int count;
                    while (0 < (count = stream.Read(buffer, 0, buffer.Length - bufferIndex))) bufferIndex += count;
                }

            var item = new BinaryPlistItem(value);
            item.SetByteValue(buffer);

            if (buffer.Length < 15)
            {
                item.Marker.Add((byte) (0x40 | (byte) buffer.Length));
            }
            else
            {
                item.Marker.Add(0x4F);
                AddIntegerCount(item.Marker, buffer.Length);
            }

            _objectTable.Add(item);
            _objectTableSize += item.Size;

            return index;
        }
        #endregion

        #region AddDate
        /// <summary>
        ///     Adds a date to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddDate(DateTime value)
        {
            if (!_uniques.Contains(value))
            {
                var index = _objectTable.Count;
                var buffer = BitConverter.GetBytes(value.ToUniversalTime().Subtract(ReferenceDate).TotalSeconds);

                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);

                var item = new BinaryPlistItem(value);
                item.Marker.Add(0x33);
                item.SetByteValue(buffer);

                _objectTable.Add(item);
                _objectTableSize += item.Size;

                _uniques.SetIndex(value, index);
                return index;
            }

            return _uniques.GetIndex(value);
        }
        #endregion

        #region AddDictionary
        /// <summary>
        ///     Adds a dictionary to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddDictionary(IDictionary value)
        {
            var index = _objectTable.Count;

            var dict = new BinaryPlistDictionary(_objectTable, value.Count);
            var item = new BinaryPlistItem(dict) {IsDictionary = true};
            _objectTable.Add(item);

            foreach (var key in value.Keys)
            {
                dict.KeyReference.Add(AddObject(key));
                dict.ObjectReference.Add(AddObject(value[key]));

                _objectRefCount += 2;
            }

            if (dict.KeyReference.Count < 15)
            {
                item.Marker.Add((byte) (0xD0 | (byte) dict.KeyReference.Count));
            }
            else
            {
                item.Marker.Add(0xDF);
                AddIntegerCount(item.Marker, dict.KeyReference.Count);
            }

            _objectTableSize += item.Size;
            return index;
        }
        #endregion

        #region AddDouble
        /// <summary>
        ///     Adds a double to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddDouble(double value)
        {
            if (!_uniques.Contains(value))
            {
                var index = _objectTable.Count;
                var buffer = BitConverter.GetBytes(value);

                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);

                var item = new BinaryPlistItem(value);
                item.Marker.Add((byte) (0x20 | (byte) Math.Log(buffer.Length, 2)));
                item.SetByteValue(buffer);

                _objectTable.Add(item);
                _objectTableSize += item.Size;

                _uniques.SetIndex(value, index);
                return index;
            }

            return _uniques.GetIndex(value);
        }
        #endregion

        #region AddFloat
        /// <summary>
        ///     Adds a float to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddFloat(float value)
        {
            if (!_uniques.Contains(value))
            {
                var index = _objectTable.Count;
                var buffer = BitConverter.GetBytes(value);

                if (BitConverter.IsLittleEndian) Array.Reverse(buffer);

                var item = new BinaryPlistItem(value);
                item.Marker.Add((byte) (0x20 | (byte) Math.Log(buffer.Length, 2)));
                item.SetByteValue(buffer);

                _objectTable.Add(item);
                _objectTableSize += item.Size;

                _uniques.SetIndex(value, index);
                return index;
            }

            return _uniques.GetIndex(value);
        }
        #endregion

        #region AddInteger
        /// <summary>
        ///     Adds an integer to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddInteger(long value)
        {
            if (!_uniques.Contains(value))
            {
                var index = _objectTable.Count;

                var item = new BinaryPlistItem(value);
                item.SetByteValue(GetIntegerBytes(value));
                item.Marker.Add((byte) (0x10 | (byte) Math.Log(item.ByteValue.Count, 2)));

                _objectTable.Add(item);
                _objectTableSize += item.Size;

                _uniques.SetIndex(value, index);
                return index;
            }

            return _uniques.GetIndex(value);
        }
        #endregion

        #region AddObject
        /// <summary>
        ///     Adds an object to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddObject(object value)
        {
            int index;
            Type type;
            TypeCode typeCode;

            if (value != null)
            {
                type = value.GetType().GetConcreteTypeIfNullable();
                typeCode = Type.GetTypeCode(type);
            }
            else
                throw new ArgumentNullException(nameof(value));

            switch (typeCode)
            {
                case TypeCode.Boolean:
                    index = AddPrimitive((bool) value);
                    break;
                case TypeCode.Byte:
                    index = AddInteger((byte) value);
                    break;
                case TypeCode.Char:
                    index = AddInteger((char) value);
                    break;
                case TypeCode.DateTime:
                    index = AddDate((DateTime) value);
                    break;
                case TypeCode.DBNull:
                    index = AddPrimitive(null);
                    break;
                case TypeCode.Decimal:
                    index = AddDouble((double) (decimal) value);
                    break;
                case TypeCode.Double:
                    index = AddDouble((double) value);
                    break;
                case TypeCode.Empty:
                    index = AddPrimitive(null);
                    break;
                case TypeCode.Int16:
                    index = AddInteger((short) value);
                    break;
                case TypeCode.Int32:
                    index = AddInteger((int) value);
                    break;
                case TypeCode.Int64:
                    index = AddInteger((long) value);
                    break;
                case TypeCode.SByte:
                    index = AddInteger((sbyte) value);
                    break;
                case TypeCode.Single:
                    index = AddFloat((float) value);
                    break;
                case TypeCode.String:
                    index = AddString((string) value);
                    break;
                case TypeCode.UInt16:
                    index = AddInteger((ushort) value);
                    break;
                case TypeCode.UInt32:
                    index = AddInteger((uint) value);
                    break;
                case TypeCode.UInt64:
                    throw new InvalidOperationException(
                        "UInt64 cannot be written to a binary plist. Please use Int64 instead. If your value cannot fit into an Int64, consider separating it into two UInt32 values.");
                default:
                    if (type.IsEnum)
                        index = AddInteger((int) value);
                    else if (typeof(IPlistSerializable).IsAssignableFrom(type))
                        index = AddDictionary(((IPlistSerializable) value).ToPlistDictionary());
                    else if (typeof(IDictionary).IsAssignableFrom(type))
                        index = AddDictionary(value as IDictionary);
                    else if ((typeof(Array).IsAssignableFrom(type)
                              || typeof(IEnumerable).IsAssignableFrom(type))
                             && !typeof(string).IsAssignableFrom(type)
                             && !typeof(byte[]).IsAssignableFrom(type))
                        index = AddArray(value as IEnumerable);
                    else if (typeof(byte[]).IsAssignableFrom(type)
                             || typeof(ISerializable).IsAssignableFrom(type)
                             || type.IsSerializable)
                        index = AddData(value);
                    else
                        throw new InvalidOperationException(
                            "A type was found in the object table that is not serializable. Types that are natively serializable to a binary plist include: null, " +
                            "booleans, integers, floats, dates, strings, arrays and dictionaries. Any other types must be marked with a SerializableAttribute or " +
                            "implement ISerializable. The type that caused this exception to be thrown is: " +
                            type.FullName);

                    break;
            }

            return index;
        }
        #endregion

        #region AddPrimitive
        /// <summary>
        ///     Adds a primitive to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddPrimitive(bool? value)
        {
            if (!value.HasValue || !_uniques.Contains(value.Value))
            {
                var index = _objectTable.Count;

                var item = new BinaryPlistItem(value);
                item.Marker.Add(value.HasValue ? value.Value ? (byte) 0x9 : (byte) 0x8 : (byte) 0);

                _objectTable.Add(item);
                _objectTableSize += item.Size;

                if (value.HasValue) _uniques.SetIndex(value.Value, index);

                return index;
            }

            return _uniques.GetIndex(value.Value);
        }
        #endregion

        #region AddString
        /// <summary>
        ///     Adds a string to the internal object table.
        /// </summary>
        /// <param name="value">The value to add.</param>
        /// <returns>The index of the added value.</returns>
        private int AddString(string value)
        {
            if (!_uniques.Contains(value))
            {
                var index = _objectTable.Count;
                var ascii = value.IsAscii();
                byte[] buffer;

                var item = new BinaryPlistItem(value);

                if (value.Length < 15)
                {
                    item.Marker.Add((byte) ((byte) (ascii ? 0x50 : 0x60) | (byte) value.Length));
                }
                else
                {
                    item.Marker.Add((byte) (ascii ? 0x5F : 0x6F));
                    AddIntegerCount(item.Marker, value.Length);
                }

                if (ascii)
                {
                    buffer = Encoding.ASCII.GetBytes(value);
                }
                else
                {
                    buffer = Encoding.Unicode.GetBytes(value);

                    if (BitConverter.IsLittleEndian)
                        for (var i = 0; i < buffer.Length; i++)
                        {
                            var l = buffer[i];
                            buffer[i] = buffer[++i];
                            buffer[i] = l;
                        }
                }

                item.SetByteValue(buffer);

                _objectTable.Add(item);
                _objectTableSize += item.Size;

                _uniques.SetIndex(value, index);
                return index;
            }

            return _uniques.GetIndex(value);
        }
        #endregion

        #region CalculateObjectRefSize
        /// <summary>
        ///     Calculates the object ref size to use for this instance's current state.
        /// </summary>
        private void CalculateObjectRefSize()
        {
            while (_objectTableSize + _topLevelObjectOffset + _objectRefCount * _objectRefSize > _maxObjectRefValue)
                switch (_objectRefSize)
                {
                    case 1:
                        _objectRefSize = 2;
                        _maxObjectRefValue = short.MaxValue;
                        break;
                    case 2:
                        _objectRefSize = 4;
                        _maxObjectRefValue = int.MaxValue;
                        break;
                    case 4:
                        _objectRefSize = 8;
                        _maxObjectRefValue = long.MaxValue;
                        break;
                    case 8:
                        break;
                    default:
                        throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture,
                            "Failed to calculate the required object reference size with an object table size of {0} and an object reference count of {1}.",
                            _objectTableSize, _objectRefCount));
                }
        }
        #endregion

        #region Reset
        /// <summary>
        ///     Resets this instance's state.
        /// </summary>
        private void Reset()
        {
            _objectTableSize =
                _objectRefCount =
                    _objectRefSize =
                        _topLevelObjectOffset = 0;

            _objectRefSize = 1;
            _maxObjectRefValue = 255;

            _objectTable = new List<BinaryPlistItem>();
            _offsetTable = new List<long>();
            _uniques = new UniqueValueCache();
        }
        #endregion

        #region WriteArray
        /// <summary>
        ///     Writes an array item to the given <see cref="BinaryWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="value">The array item to write.</param>
        /// <returns>The number of bytes written.</returns>
        private int WriteArray(BinaryWriter writer, BinaryPlistItem value)
        {
            var size = value.Marker.Count;
            var array = (BinaryPlistArray) value.Value;

            writer.Write(value.Marker.ToArray());

            size += array.ObjectReference.Sum(objectRef => WriteReferenceInteger(writer, objectRef, _objectRefSize));

            return size;
        }
        #endregion

        #region WriteDictionary
        /// <summary>
        ///     Writes a dictionary item to the given <see cref="BinaryWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <param name="value">The dictionary item to write.</param>
        /// <returns>The number of bytes written.</returns>
        private int WriteDictionary(BinaryWriter writer, BinaryPlistItem value)
        {
            var size = value.Marker.Count;
            var dict = (BinaryPlistDictionary) value.Value;

            writer.Write(value.Marker.ToArray());

            size += dict.KeyReference.Sum(keyRef => WriteReferenceInteger(writer, keyRef, _objectRefSize));
            size += dict.ObjectReference.Sum(objectRef => WriteReferenceInteger(writer, objectRef, _objectRefSize));

            return size;
        }
        #endregion

        #region WriteObjectTable
        /// <summary>
        ///     Write the object table to the given <see cref="BinaryWriter" />.
        /// </summary>
        /// <param name="writer">The <see cref="BinaryWriter" /> to write to.</param>
        /// <returns>The number of bytes written.</returns>
        private int WriteObjectTable(BinaryWriter writer)
        {
            var offset = _topLevelObjectOffset;

            foreach (var item in _objectTable)
            {
                _offsetTable.Add(offset);

                if (item.IsArray)
                {
                    offset += WriteArray(writer, item);
                }
                else if (item.IsDictionary)
                {
                    offset += WriteDictionary(writer, item);
                }
                else
                {
                    writer.Write(item.Marker.ToArray());

                    if (item.ByteValue.Count > 0) writer.Write(item.ByteValue.ToArray());

                    offset += item.Size;
                }
            }

            return offset - _topLevelObjectOffset;
        }
        #endregion
    }
}