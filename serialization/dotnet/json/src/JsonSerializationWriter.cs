// ------------------------------------------------------------------------------
//  Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
// ------------------------------------------------------------------------------

using System;
using System.Linq;
using System.IO;
using System.Text.Json;
using Microsoft.Kiota.Abstractions.Serialization;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Kiota.Abstractions.Extensions;

namespace Microsoft.Kiota.Serialization.Json
{
    /// <summary>
    /// The <see cref="ISerializationWriter"/> implementation for json content types.
    /// </summary>
    public class JsonSerializationWriter : ISerializationWriter, IDisposable
    {
        private readonly MemoryStream _stream = new MemoryStream();

        /// <summary>
        /// The <see cref="Utf8JsonWriter"/> instance for writing json content
        /// </summary>
        public readonly Utf8JsonWriter writer;

        /// <summary>
        /// The <see cref="JsonSerializationWriter"/> constructor
        /// </summary>
        public JsonSerializationWriter()
        {
            writer = new Utf8JsonWriter(_stream);
        }

        /// <summary>
        /// The action to perform before object serialization
        /// </summary>
        public Action<IParsable> OnBeforeObjectSerialization { get; set; }

        /// <summary>
        /// The action to perform after object serialization
        /// </summary>
        public Action<IParsable> OnAfterObjectSerialization { get; set; }

        /// <summary>
        /// The action to perform on the start of object serialization
        /// </summary>
        public Action<IParsable, ISerializationWriter> OnStartObjectSerialization { get; set; }

        /// <summary>
        /// Get the stream of the serialized content
        /// </summary>
        /// <returns>The <see cref="Stream"/> of the serialized content</returns>
        public Stream GetSerializedContent() {
            writer.Flush();
            _stream.Position = 0;
            return _stream;
        }

        /// <summary>
        /// Write the string value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The string value</param>
        public void WriteStringValue(string key, string value)
        {
            if(value != null)
            { // we want to keep empty string because they are meaningfull
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStringValue(value);
            }
        }

        /// <summary>
        /// Write the boolean value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The boolean value</param>
        public void WriteBoolValue(string key, bool? value)
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteBooleanValue(value.Value);
        }

        /// <summary>
        /// Write the int value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The int value</param>
        public void WriteIntValue(string key, int? value)
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteNumberValue(value.Value);
        }

        /// <summary>
        /// Write the float value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The float value</param>
        public void WriteFloatValue(string key, float? value)
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteNumberValue(value.Value);
        }

        /// <summary>
        /// Write the double value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The double value</param>
        public void WriteDoubleValue(string key, double? value)
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteNumberValue(value.Value);
        }

        /// <summary>
        /// Write the Guid value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The Guid value</param>
        public void WriteGuidValue(string key, Guid? value)
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteStringValue(value.Value);
        }

        /// <summary>
        /// Write the DateTimeOffset value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The DateTimeOffset value</param>
        public void WriteDateTimeOffsetValue(string key, DateTimeOffset? value)
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue) writer.WriteStringValue(value.Value);
        }

        /// <summary>
        /// Write the null value
        /// </summary>
        /// <param name="key">The key of the json node</param>
        public void WriteNullValue(string key)
        {
            if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
            writer.WriteNullValue();
        }

        /// <summary>
        /// Write the enumeration value of type  <typeparam name="T"/>
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The enumeration value</param>
        public void WriteEnumValue<T>(string key, T? value) where T : struct, Enum
        {
            if(!string.IsNullOrEmpty(key) && value.HasValue) writer.WritePropertyName(key);
            if(value.HasValue)
            {
                if(typeof(T).GetCustomAttributes<FlagsAttribute>().Any())
                    writer.WriteStringValue(Enum.GetValues<T>()
                                            .Where(x => value.Value.HasFlag(x))
                                            .Select(x => Enum.GetName<T>(x))
                                            .Select(x => x.ToFirstCharacterLowerCase())
                                            .Aggregate((x, y) => $"{x},{y}"));
                else writer.WriteStringValue(value.Value.ToString().ToFirstCharacterLowerCase());
            }
        }

        /// <summary>
        /// Write the collection of primitives of type  <typeparam name="T"/>
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="values">The primitive collection</param>
        public void WriteCollectionOfPrimitiveValues<T>(string key, IEnumerable<T> values)
        {
            if(values != null)
            { //empty array is meaningful
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStartArray();
                foreach(var collectionValue in values)
                    WriteAnyValue(null, collectionValue);
                writer.WriteEndArray();
            }
        }

        /// <summary>
        /// Write the collection of objects of type  <typeparam name="T"/>
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="values">The object collection</param>
        public void WriteCollectionOfObjectValues<T>(string key, IEnumerable<T> values) where T : IParsable
        {
            if(values != null)
            { //empty array is meaningful
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                writer.WriteStartArray();
                foreach(var item in values)
                    WriteObjectValue<T>(null, item);
                writer.WriteEndArray();
            }
        }

        /// <summary>
        /// Write the object of type <typeparam name="T"/>
        /// </summary>
        /// <param name="key">The key of the json node</param>
        /// <param name="value">The object instance to write</param>
        public void WriteObjectValue<T>(string key, T value) where T : IParsable
        {
            if(value != null)
            {
                if(!string.IsNullOrEmpty(key)) writer.WritePropertyName(key);
                OnBeforeObjectSerialization?.Invoke(value);
                writer.WriteStartObject();
                OnStartObjectSerialization?.Invoke(value, this);
                value.Serialize(this);
                writer.WriteEndObject();
                OnAfterObjectSerialization?.Invoke(value);
            }
        }

        /// <summary>
        /// Write the additional data property bag
        /// </summary>
        /// <param name="value">The additional data dictionary</param>
        public void WriteAdditionalData(IDictionary<string, object> value)
        {
            if(value == null) return;

            foreach(var dataValue in value)
                WriteAnyValue(dataValue.Key, dataValue.Value);
        }

        private void WriteNonParsableObjectValue<T>(string key, T value)
        {
            if(!string.IsNullOrEmpty(key))
                writer.WritePropertyName(key);
            writer.WriteStartObject();
            if(value == null) writer.WriteNullValue();
            else
                foreach(var oProp in value.GetType().GetProperties())
                    WriteAnyValue(oProp.Name, oProp.GetValue(value));
            writer.WriteEndObject();
        }
        private void WriteAnyValue<T>(string key, T value)
        {
            if(value == null)
            {
                if(!string.IsNullOrEmpty(key))
                    this.writer.WritePropertyName(key);
                this.writer.WriteNullValue();
            }
            switch(value)
            {
                case string s:
                    WriteStringValue(key, s);
                    break;
                case bool b:
                    WriteBoolValue(key, b);
                    break;
                case int i:
                    WriteIntValue(key, i);
                    break;
                case float f:
                    WriteFloatValue(key, f);
                    break;
                case double d:
                    WriteDoubleValue(key, d);
                    break;
                case Guid g:
                    WriteGuidValue(key, g);
                    break;
                case DateTimeOffset dto:
                    WriteDateTimeOffsetValue(key, dto);
                    break;
                case IEnumerable<object> coll:
                    WriteCollectionOfPrimitiveValues(key, coll); // should we support collections of parsables here too?
                    break;
                case object o:
                    WriteNonParsableObjectValue(key, o); // should we support parsables here too?
                    break;
                case null:
                    WriteNullValue(key);
                    break;
                default:
                    throw new InvalidOperationException($"error serialization additional data value with key {key}, unknown type {value?.GetType()}");
            }
        }

        /// <summary>
        /// Cleanup/dispose the writer
        /// </summary>
        public void Dispose()
        {
            writer.Dispose();
        }
    }
}
