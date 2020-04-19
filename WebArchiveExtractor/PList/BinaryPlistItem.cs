//-----------------------------------------------------------------------
// <copyright file="BinaryPlistItem.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections.Generic;

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Represents an item in a binary plist's object table.
    /// </summary>
    internal class BinaryPlistItem
    {
        #region Fields
        private readonly List<byte> _byteValue;
        private readonly List<byte> _marker;

        /// <summary>
        ///     Gets the item's byte value collection.
        /// </summary>
        public IList<byte> ByteValue => _byteValue;
        #endregion

        #region Properties
        /// <summary>
        ///     Gets or sets a value indicating whether this item represents an array.
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        ///     Gets or sets a value indicating whether this item represents a dictionary.
        /// </summary>
        public bool IsDictionary { get; set; }

        /// <summary>
        ///     Gets the item's marker value collection.
        /// </summary>
        public IList<byte> Marker => _marker;

        /// <summary>
        ///     Gets the item's size, which is a sum of the <see cref="Marker" /> and <see cref="ByteValue" /> lengths.
        /// </summary>
        public int Size => Marker.Count + ByteValue.Count;

        /// <summary>
        ///     Gets or sets the object value this item represents.
        /// </summary>
        public object Value { get; set; }
        #endregion

        #region Constructors
        /// <summary>
        ///     Initializes a new instance of the BinaryPlistItem class.
        /// </summary>
        public BinaryPlistItem()
        {
            _byteValue = new List<byte>();
            _marker = new List<byte>();
        }

        /// <summary>
        ///     Initializes a new instance of the BinaryPlistItem class.
        /// </summary>
        /// <param name="value">The value of the object the item represents.</param>
        public BinaryPlistItem(object value) : this()
        {
            Value = value;
        }
        #endregion

        #region SetByteValue
        /// <summary>
        ///     Sets the <see cref="ByteValue" /> to the given collection.
        /// </summary>
        /// <param name="buffer">The collection to set.</param>
        public void SetByteValue(IEnumerable<byte> buffer)
        {
            _byteValue.Clear();

            if (buffer != null) _byteValue.AddRange(buffer);
        }
        #endregion

        #region ToString
        /// <summary>
        ///     Gets the string representation of this instance.
        /// </summary>
        /// <returns>The string representation of this instance.</returns>
        public override string ToString()
        {
            return Value != null ? Value.ToString() : "null";
        }
        #endregion
    }
}