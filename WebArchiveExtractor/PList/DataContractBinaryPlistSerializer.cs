//-----------------------------------------------------------------------
// <copyright file="DataContractBinaryPlistSerializer.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

// ReSharper disable MemberCanBePrivate.Global

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Serializes data contracts to and from the binary plist format.
    /// </summary>
    public sealed class DataContractBinaryPlistSerializer
    {
        #region Fields
        private readonly Type _rootType;
        private readonly Dictionary<Type, TypeCacheItem> _typeCache;
        #endregion

        #region Constructor
        /// <summary>
        ///     Initializes a new instance of the DataContractBinaryPlistSerializer class.
        /// </summary>
        /// <param name="type">The type of the instances that are serialized or de-serialized.</param>
        public DataContractBinaryPlistSerializer(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type), "type cannot be null.");

            var isDictionary = typeof(IDictionary).IsAssignableFrom(type);

            if (!isDictionary && type.IsCollection())
                throw new ArgumentException(
                    "root type cannot be a collection unless it is an IDictionary implementation.", nameof(type));

            if (type.IsPrimitiveOrEnum())
                throw new ArgumentException("type must be an implementation of IDictionary or a complex object type.",
                    nameof(type));

            _rootType = type;
            _typeCache = new Dictionary<Type, TypeCacheItem>();
        }
        #endregion

        #region ReadObject
        /// <summary>
        ///     Reads an object from the specified stream.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <returns>The de-serialized object.</returns>
        public object ReadObject(Stream stream)
        {
            return GetReadablePlistObject(_rootType, new BinaryPlistReader().ReadObject(stream));
        }
        #endregion

        #region WriteObject
        /// <summary>
        ///     Writes the complete contents of the given object to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="graph">The object to write.</param>
        public void WriteObject(Stream stream, object graph)
        {
            WriteObject(stream, graph, true);
        }
        #endregion

        #region WriteObject
        /// <summary>
        ///     Writes the complete contents of the given object to the specified stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="graph">The object to write.</param>
        /// <param name="closeStream">A value indicating whether to close the stream after the write operation completes.</param>
        public void WriteObject(Stream stream, object graph, bool closeStream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream), "stream cannot be null.");

            if (graph == null) throw new ArgumentNullException(nameof(graph), "graph cannot be null.");

            var type = graph.GetType();

            if (!_rootType.IsAssignableFrom(type))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture,
                    "The object specified is of type {0}, which is not assignable to this instance's root type, {1}.",
                    type, _rootType));

            var dict = GetWritablePlistObject(type, graph) as IDictionary;

            if (dict == null)
                throw new ArgumentException(
                    "The root object must be assignable to IDictionary, a complex type with an explicit or implied data contract, or assignable to IPlistSerializable.",
                    nameof(graph));

            new BinaryPlistWriter().WriteObject(stream, dict, closeStream);
        }
        #endregion

        #region GetReadablePlistObject
        /// <summary>
        ///     Gets the readable plist value of the given object identified by the specified type.
        /// </summary>
        /// <param name="type">The type the object is expected to have after being de-serialized.</param>
        /// <param name="obj">The raw plist object value.</param>
        /// <returns>A readable plist object value.</returns>
        private object GetReadablePlistObject(Type type, object obj)
        {
            object result = null;
            var plistDict = obj as IDictionary;

            if (obj == null) return null;
            if (typeof(IPlistSerializable).IsAssignableFrom(type))
            {
                if (plistDict == null) return null;
                var serResult = (IPlistSerializable) Activator.CreateInstance(type);
                serResult.FromPlistDictionary(plistDict);
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                if (plistDict == null) return null;
                Type keyType = typeof(object), valueType = typeof(object);

                if (type.IsGenericType)
                {
                    var args = type.GetGenericArguments();
                    keyType = args[0];
                    valueType = args[1];
                }

                var dictResult = (IDictionary) (type.IsInterface
                    ? Activator.CreateInstance(typeof(Dictionary<,>).MakeGenericType(keyType, valueType))
                    : Activator.CreateInstance(type));

                foreach (var key in plistDict.Keys)
                {
                    if (!type.IsGenericType)
                    {
                        keyType = key.GetType();
                        valueType = plistDict[key] != null ? plistDict[key].GetType() : typeof(object);
                    }

                    dictResult[GetReadablePlistObject(keyType, key)] =
                        GetReadablePlistObject(valueType, plistDict[key]);
                }

                result = dictResult;
            }
            else if (type.IsCollection())
            {
                if (!(obj is IEnumerable plistColl)) return null;
                var valueType = typeof(object);
                var isArray = false;
                IList listResult;

                if (type.IsGenericType)
                {
                    valueType = type.GetGenericArguments()[0];
                }
                else if (typeof(Array).IsAssignableFrom(type))
                {
                    valueType = type.GetElementType();
                    isArray = true;
                }

                if (isArray)
                    listResult = new ArrayList();
                else
                    // TODO: The default DataContractSerializer uses an informal protocal requiring a method named "Add()"
                    // rather than requiring concrete collection types to implement IList.
                    listResult = (IList) (type.IsInterface
                        ? Activator.CreateInstance(typeof(List<>).MakeGenericType(valueType))
                        : Activator.CreateInstance(type));

                foreach (var value in plistColl) listResult.Add(GetReadablePlistObject(valueType, value));

                result = isArray ? ((ArrayList) listResult).ToArray() : listResult;
            }
            else if (type.IsPrimitiveOrEnum())
            {
                result = obj;
            }
            else
            {
                if (plistDict == null) return null;
                if (!_typeCache.ContainsKey(type)) _typeCache[type] = new TypeCacheItem(type);

                var cache = _typeCache[type];
                result = Activator.CreateInstance(type);

                for (var i = 0; i < cache.Fields.Count; i++)
                {
                    var field = cache.Fields[i];
                    var member = cache.FieldMembers[i];

                    if (plistDict.Contains(member.Name))
                        field.SetValue(result, GetReadablePlistObject(field.FieldType, plistDict[member.Name]));
                }

                for (var i = 0; i < cache.Properties.Count; i++)
                {
                    var property = cache.Properties[i];
                    var member = cache.PropertyMembers[i];

                    if (plistDict.Contains(member.Name))
                        property.SetValue(result,
                            GetReadablePlistObject(property.PropertyType, plistDict[member.Name]), null);
                }
            }

            return result;
        }
        #endregion

        #region GetWritablePlistObject
        /// <summary>
        ///     Gets the writable plist value of the given object identified by the specified type.
        /// </summary>
        /// <param name="type">The of the object.</param>
        /// <param name="obj">The object to get the plist value of.</param>
        /// <returns>The plist value of the given object.</returns>
        private object GetWritablePlistObject(Type type, object obj)
        {
            object result;

            if (obj == null) return null;

            if (typeof(IPlistSerializable).IsAssignableFrom(type))
            {
                result = ((IPlistSerializable) obj).ToPlistDictionary();
            }
            else if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var dict = obj as IDictionary;
                var resultDict = new Dictionary<object, object>();

                if (dict != null)
                    foreach (var key in dict.Keys)
                    {
                        var value = dict[key];
                        resultDict[GetWritablePlistObject(key.GetType(), key)] =
                            GetWritablePlistObject(value.GetType(), value);
                    }

                result = resultDict;
            }
            else if (type.IsCollection())
            {
                var coll = obj as IEnumerable;
                var resultColl = new List<object>();

                if (coll != null)
                    resultColl.AddRange(from object value in coll
                        select GetWritablePlistObject(value.GetType(), value));

                result = resultColl;
            }
            else if (type.IsPrimitiveOrEnum())
            {
                result = obj;
            }
            else
            {
                if (!_typeCache.ContainsKey(type)) _typeCache[type] = new TypeCacheItem(type);

                var cache = _typeCache[type];
                var resultDict = new Dictionary<string, object>();

                for (var i = 0; i < cache.Fields.Count; i++)
                {
                    var field = cache.Fields[i];
                    var member = cache.FieldMembers[i];
                    var fieldValue = field.GetValue(obj);

                    if (member.EmitDefaultValue || !field.FieldType.IsDefaultValue(fieldValue))
                        resultDict[member.Name] = GetWritablePlistObject(field.FieldType, fieldValue);
                }

                for (var i = 0; i < cache.Properties.Count; i++)
                {
                    var property = cache.Properties[i];
                    var member = cache.PropertyMembers[i];
                    var propertyValue = property.GetValue(obj, null);

                    if (member.EmitDefaultValue || !property.PropertyType.IsDefaultValue(propertyValue))
                        resultDict[member.Name] = GetWritablePlistObject(property.PropertyType, propertyValue);
                }

                result = resultDict;
            }

            return result;
        }
    }
    #endregion
}