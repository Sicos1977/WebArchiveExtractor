//-----------------------------------------------------------------------
// <copyright file="BinaryPlistDictionary.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
//     Inspired by BinaryPListParser.java, copyright (c) 2005 Werner Randelshofer
//          http://www.java2s.com/Open-Source/Java-Document/Swing-Library/jide-common/com/jidesoft/plaf/aqua/BinaryPListParser.java.htm
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;

// ReSharper disable MemberCanBePrivate.Global

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Represents a dictionary in a binary plist.
    /// </summary>
    internal class BinaryPlistDictionary
    {
        #region Properties
        /// <summary>
        ///     Gets the dictionary's key reference collection.
        /// </summary>
        public IList<int> KeyReference { get; }

        /// <summary>
        ///     Gets the dictionary's object reference collection.
        /// </summary>
        public IList<int> ObjectReference { get; }

        /// <summary>
        ///     Gets a reference to the binary plist's object table.
        /// </summary>
        public IList<BinaryPlistItem> ObjectTable { get; }
        #endregion

        #region Constructor
        /// <summary>
        ///     Initializes a new instance of the BinaryPlistDictionary class.
        /// </summary>
        /// <param name="objectTable">A reference to the binary plist's object table.</param>
        /// <param name="size">The size of the dictionary.</param>
        public BinaryPlistDictionary(IList<BinaryPlistItem> objectTable, int size)
        {
            KeyReference = new List<int>(size);
            ObjectReference = new List<int>(size);
            ObjectTable = objectTable;
        }
        #endregion

        #region ToDictionary
        /// <summary>
        ///     Converts this instance into a <see cref="Dictionary{Object, Object}" />.
        /// </summary>
        /// <returns>A <see cref="Dictionary{Object, Object}" /> representation this instance.</returns>
        public Dictionary<object, object> ToDictionary()
        {
            var dictionary = new Dictionary<object, object>();

            for (var i = 0; i < KeyReference.Count; i++)
            {
                var keyRef = KeyReference[i];
                var objectRef = ObjectReference[i];

                if (keyRef < 0 || keyRef >= ObjectTable.Count ||
                    (ObjectTable[keyRef] != null && ObjectTable[keyRef].Value == this) || objectRef < 0 ||
                    objectRef >= ObjectTable.Count ||
                    (ObjectTable[objectRef] != null && ObjectTable[objectRef].Value == this)) continue;
                var keyValue = ObjectTable[keyRef] == null ? null : ObjectTable[keyRef].Value;
                var objectValue = ObjectTable[objectRef] == null ? null : ObjectTable[objectRef].Value;

                switch (objectValue)
                {
                    case BinaryPlistDictionary innerDict:
                        objectValue = innerDict.ToDictionary();
                        break;
                    case BinaryPlistArray innerArray:
                        objectValue = innerArray.ToArray();
                        break;
                }

                dictionary[keyValue ?? throw new InvalidOperationException()] = objectValue;
            }

            return dictionary;
        }
        #endregion

        #region ToString
        /// <summary>
        ///     Returns the string representation of this instance.
        /// </summary>
        /// <returns>This instance's string representation.</returns>
        public override string ToString()
        {
            var sb = new StringBuilder("{");

            for (var i = 0; i < KeyReference.Count; i++)
            {
                if (i > 0) sb.Append(",");

                var keyRef = KeyReference[i];
                var objectRef = ObjectReference[i];

                if (keyRef < 0 || keyRef >= ObjectTable.Count)
                    sb.Append("#" + keyRef);
                else if (ObjectTable[keyRef] != null && ObjectTable[keyRef].Value == this)
                    sb.Append("*" + keyRef);
                else
                    sb.Append(ObjectTable[keyRef]);

                sb.Append(":");

                if (objectRef < 0 || objectRef >= ObjectTable.Count)
                    sb.Append("#" + objectRef);
                else if (ObjectTable[objectRef] != null && ObjectTable[objectRef].Value == this)
                    sb.Append("*" + objectRef);
                else
                    sb.Append(ObjectTable[objectRef]);
            }

            return sb + "}";
        }
        #endregion
    }
}