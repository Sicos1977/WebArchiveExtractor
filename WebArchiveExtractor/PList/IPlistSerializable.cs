//-----------------------------------------------------------------------
// <copyright file="IPlistSerializable.cs" company="Tasty Codes">
//     Copyright (c) 2011 Chad Burggraf.
// </copyright>
//-----------------------------------------------------------------------

using System.Collections;

namespace WebArchiveExtractor.PList
{
    /// <summary>
    /// Defines the interface for proxy serialization with <see cref="BinaryPlistReader"/> and <see cref="BinaryPlistWriter"/>.
    /// </summary>
    public interface IPlistSerializable
    {
        /// <summary>
        /// Populates this instance from the given plist <see cref="IDictionary"/> representation.
        /// Note that nested <see cref="IPlistSerializable"/> objects found in the graph during
        /// <see cref="ToPlistDictionary()"/> are represented as nested <see cref="IDictionary"/> instances here.
        /// </summary>
        /// <param name="plist">The plist <see cref="IDictionary"/> representation of this instance.</param>
        void FromPlistDictionary(IDictionary plist);

        /// <summary>
        /// Gets a plist friendly <see cref="IDictionary"/> representation of this instance.
        /// The returned dictionary may contain nested implementations of <see cref="IPlistSerializable"/>.
        /// </summary>
        /// <returns>A plist friendly <see cref="IDictionary"/> representation of this instance.</returns>
        IDictionary ToPlistDictionary();
    }
}
