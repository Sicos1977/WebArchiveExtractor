//-----------------------------------------------------------------------
// <copyright file="UniqueValueCache.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Provides a cache of unique primitive values when writing a binary plist.
    /// </summary>
    internal sealed class UniqueValueCache
    {
        #region Fields
        private readonly Dictionary<bool, int> _booleans = new Dictionary<bool, int>();
        private readonly Dictionary<long, int> _integers = new Dictionary<long, int>();
        private readonly Dictionary<float, int> _floats = new Dictionary<float, int>();
        private readonly Dictionary<double, int> _doubles = new Dictionary<double, int>();
        private readonly Dictionary<DateTime, int> _dates = new Dictionary<DateTime, int>();
        private readonly Dictionary<string, int> _strings = new Dictionary<string, int>();
        #endregion

        #region Contains
        /// <summary>
        ///     Gets a value indicating whether the cache contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>True if the cache contains the value, false otherwise.</returns>
        public bool Contains(bool value)
        {
            return _booleans.ContainsKey(value);
        }

        /// <summary>
        ///     Gets a value indicating whether the cache contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>True if the cache contains the value, false otherwise.</returns>
        public bool Contains(long value)
        {
            return _integers.ContainsKey(value);
        }

        /// <summary>
        ///     Gets a value indicating whether the cache contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>True if the cache contains the value, false otherwise.</returns>
        public bool Contains(float value)
        {
            return _floats.ContainsKey(value);
        }

        /// <summary>
        ///     Gets a value indicating whether the cache contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>True if the cache contains the value, false otherwise.</returns>
        public bool Contains(double value)
        {
            return _doubles.ContainsKey(value);
        }

        /// <summary>
        ///     Gets a value indicating whether the cache contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>True if the cache contains the value, false otherwise.</returns>
        public bool Contains(DateTime value)
        {
            return _dates.ContainsKey(value);
        }

        /// <summary>
        ///     Gets a value indicating whether the cache contains the given value.
        /// </summary>
        /// <param name="value">The value to check for.</param>
        /// <returns>True if the cache contains the value, false otherwise.</returns>
        public bool Contains(string value)
        {
            return _strings.ContainsKey(value);
        }
        #endregion

        #region GetIndex
        /// <summary>
        ///     Gets the index in the object table for the given value, assuming it has already been added to the cache.
        /// </summary>
        /// <param name="value">The value to get the index of.</param>
        /// <returns>The index of the value.</returns>
        public int GetIndex(bool value)
        {
            return _booleans[value];
        }

        /// <summary>
        ///     Gets the index in the object table for the given value, assuming it has already been added to the cache.
        /// </summary>
        /// <param name="value">The value to get the index of.</param>
        /// <returns>The index of the value.</returns>
        public int GetIndex(long value)
        {
            return _integers[value];
        }

        /// <summary>
        ///     Gets the index in the object table for the given value, assuming it has already been added to the cache.
        /// </summary>
        /// <param name="value">The value to get the index of.</param>
        /// <returns>The index of the value.</returns>
        public int GetIndex(float value)
        {
            return _floats[value];
        }

        /// <summary>
        ///     Gets the index in the object table for the given value, assuming it has already been added to the cache.
        /// </summary>
        /// <param name="value">The value to get the index of.</param>
        /// <returns>The index of the value.</returns>
        public int GetIndex(double value)
        {
            return _doubles[value];
        }

        /// <summary>
        ///     Gets the index in the object table for the given value, assuming it has already been added to the cache.
        /// </summary>
        /// <param name="value">The value to get the index of.</param>
        /// <returns>The index of the value.</returns>
        public int GetIndex(DateTime value)
        {
            return _dates[value];
        }

        /// <summary>
        ///     Gets the index in the object table for the given value, assuming it has already been added to the cache.
        /// </summary>
        /// <param name="value">The value to get the index of.</param>
        /// <returns>The index of the value.</returns>
        public int GetIndex(string value)
        {
            return _strings[value];
        }
        #endregion

        #region SetIndex
        /// <summary>
        ///     Sets the index in the object table for the given value.
        /// </summary>
        /// <param name="value">The value to set the index for.</param>
        /// <param name="index">The index to set.</param>
        public void SetIndex(bool value, int index)
        {
            _booleans[value] = index;
        }

        /// <summary>
        ///     Sets the index in the object table for the given value.
        /// </summary>
        /// <param name="value">The value to set the index for.</param>
        /// <param name="index">The index to set.</param>
        public void SetIndex(long value, int index)
        {
            _integers[value] = index;
        }

        /// <summary>
        ///     Sets the index in the object table for the given value.
        /// </summary>
        /// <param name="value">The value to set the index for.</param>
        /// <param name="index">The index to set.</param>
        public void SetIndex(float value, int index)
        {
            _floats[value] = index;
        }

        /// <summary>
        ///     Sets the index in the object table for the given value.
        /// </summary>
        /// <param name="value">The value to set the index for.</param>
        /// <param name="index">The index to set.</param>
        public void SetIndex(double value, int index)
        {
            _doubles[value] = index;
        }

        /// <summary>
        ///     Sets the index in the object table for the given value.
        /// </summary>
        /// <param name="value">The value to set the index for.</param>
        /// <param name="index">The index to set.</param>
        public void SetIndex(string value, int index)
        {
            _strings[value] = index;
        }

        /// <summary>
        ///     Sets the index in the object table for the given value.
        /// </summary>
        /// <param name="value">The value to set the index for.</param>
        /// <param name="index">The index to set.</param>
        public void SetIndex(DateTime value, int index)
        {
            _dates[value] = index;
        }
        #endregion
    }
}