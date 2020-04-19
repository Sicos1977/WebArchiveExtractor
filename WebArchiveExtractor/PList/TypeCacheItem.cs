//-----------------------------------------------------------------------
// <copyright file="TypeCacheItem.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace WebArchiveExtractor.PList
{
    /// <summary>
    ///     Represents a cached type used during serialization by a <see cref="DataContractBinaryPlistSerializer" />.
    /// </summary>
    internal sealed class TypeCacheItem
    {
        #region Fields
        private readonly Type _type;
        private readonly bool _hasCustomContract;
        #endregion

        #region Properties
        /// <summary>
        ///     Gets the collection of concrete or simulated <see cref="DataMemberAttribute" />s for the type's fields.
        /// </summary>
        public IList<DataMemberAttribute> FieldMembers { get; private set; }

        /// <summary>
        ///     Gets a collection of the type's fields.
        /// </summary>
        public IList<FieldInfo> Fields { get; private set; }

        /// <summary>
        ///     Gets a collection of the type's properties.
        /// </summary>
        public IList<PropertyInfo> Properties { get; private set; }

        /// <summary>
        ///     Gets a collection of concrete or simulated <see cref="DataMemberAttribute" />s for the type's properties.
        /// </summary>
        public IList<DataMemberAttribute> PropertyMembers { get; private set; }
        #endregion

        #region Constructor
        /// <summary>
        ///     Initializes a new instance of the TypeCacheItem class.
        /// </summary>
        /// <param name="type">The type to cache.</param>
        public TypeCacheItem(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type), "type cannot be null.");

            _type = type;
            _hasCustomContract = type.GetCustomAttributes(typeof(DataContractAttribute), false).Length > 0;
            InitializeFields();
            InitializeProperties();
        }
        #endregion

        #region InitializeFields
        /// <summary>
        ///     Initializes this instance's field-related properties.
        /// </summary>
        private void InitializeFields()
        {
            FieldMembers = new List<DataMemberAttribute>();
            Fields = new List<FieldInfo>();

            var fields = _hasCustomContract
                ? _type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : _type.GetFields(BindingFlags.Instance | BindingFlags.Public);

            var tuples = from f in fields
                let attr = f.GetCustomAttributes(false)
                let member = attr.OfType<DataMemberAttribute>().FirstOrDefault()
                where !f.IsLiteral && !attr.OfType<IgnoreDataMemberAttribute>().Any()
                select new
                {
                    Info = f,
                    Member = member
                };

            foreach (var tuple in tuples.Where(t => !_hasCustomContract || t.Member != null))
            {
                var member = tuple.Member ?? new DataMemberAttribute
                {
                    EmitDefaultValue = true,
                    IsRequired = false
                };

                member.Name = !string.IsNullOrEmpty(member.Name) ? member.Name : tuple.Info.Name;

                FieldMembers.Add(member);
                Fields.Add(tuple.Info);
            }
        }
        #endregion

        #region InitializeProperties
        /// <summary>
        ///     Initializes this instance's property-related properties.
        /// </summary>
        private void InitializeProperties()
        {
            Properties = new List<PropertyInfo>();
            PropertyMembers = new List<DataMemberAttribute>();

            var properties = _hasCustomContract
                ? _type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                : _type.GetProperties(BindingFlags.Instance | BindingFlags.Public);

            var tuples = from p in properties
                let attr = p.GetCustomAttributes(false)
                let member = attr.OfType<DataMemberAttribute>().FirstOrDefault()
                where p.CanRead && p.CanWrite && !attr.OfType<IgnoreDataMemberAttribute>().Any()
                select new
                {
                    Info = p,
                    Member = member
                };

            foreach (var tuple in tuples.Where(t => !_hasCustomContract || t.Member != null))
            {
                var member = tuple.Member ?? new DataMemberAttribute
                {
                    EmitDefaultValue = true,
                    IsRequired = false
                };

                member.Name = !string.IsNullOrEmpty(member.Name) ? member.Name : tuple.Info.Name;

                PropertyMembers.Add(member);
                Properties.Add(tuple.Info);
            }
        }
        #endregion
    }
}