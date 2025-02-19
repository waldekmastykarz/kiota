using System;
using System.Collections.Generic;

namespace Microsoft.Kiota.Abstractions.Serialization {
    /// <summary>
    ///  This factory holds a list of all the registered factories for the various types of nodes.
    /// </summary>
    public class SerializationWriterFactoryRegistry : ISerializationWriterFactory {
        public string ValidContentType { get {
            throw new InvalidOperationException("The registry supports multiple content types. Get the registered factory instead.");
        }}
        /// <summary>
        /// Default singleton instance of the registry to be used when registring new factories that should be available by default.
        /// </summary>
        public static readonly SerializationWriterFactoryRegistry DefaultInstance = new();
        /// <summary>
        /// List of factories that are registered by content type.
        /// </summary>
        public Dictionary<string, ISerializationWriterFactory> ContentTypeAssociatedFactories { get; set; } = new Dictionary<string, ISerializationWriterFactory>();
        public ISerializationWriter GetSerializationWriter(string contentType) {
            if(string.IsNullOrEmpty(contentType))
                throw new ArgumentNullException(nameof(contentType));
            
            if(ContentTypeAssociatedFactories.ContainsKey(contentType))
                return ContentTypeAssociatedFactories[contentType].GetSerializationWriter(contentType);
            else
                throw new InvalidOperationException($"Content type {contentType} does not have a factory registered to be parsed");
        }

    }
}
