// SilverlightSerializer by Mike Talbot
//                          http://whydoidoit.com
//                          email:   mike.talbot@alterian.com
//                          twitter: mike_talbot
//
// This code is free to use, no warranty is offered or implied.
// If you redistribute, please retain this header.

/*
 * Joel: this class has been modified slightly from the original source so that it works with C# 3.0
 * Primarily, removed the "optional parameters" and just passed in nulls
 * */
#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

#endregion


namespace FlatRedBall.IO
{
    /// <summary>
    ///   Indicates that a property or field should not be serialized
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DoNotSerialize : Attribute
    {

    }
    /// <summary>
    /// Used in checksum mode to flag a property as not being part
    /// of the "meaning" of an object - i.e. two objects with the
    /// same checksum "mean" the same thing, even if some of the
    /// properties are different, those properties would not be
    /// relevant to the purpose of the object
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class DoNotChecksum : Attribute
    {
    }
    /// <summary>
    /// Attribute used to flag IDs this can be useful for check object
    /// consistence when the serializer is in a mode that does not 
    /// serialize identifiers
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class SerializerId : Attribute
    {
    }

    public interface ISerializeObject
    {
        object[] Serialize(object target);
        object Deserialize(object[] data);
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class SerializerAttribute : Attribute
    {
        internal Type SerializesType;
        public SerializerAttribute(Type serializesType)
        {
            SerializesType = serializesType;
        }
    }

    /// <summary>
    ///   Silverlight/.NET compatible binary serializer with suppression support
    ///   produces compact representations, suitable for further compression.  This uses reflection so it may be slow.
    /// </summary>
    public static class SilverlightSerializer
    {
        private static readonly Dictionary<Type, IEnumerable<FieldInfo>> FieldLists = new Dictionary<Type, IEnumerable<FieldInfo>>();
        private static readonly Dictionary<string, IEnumerable<PropertyInfo>> PropertyLists = new Dictionary<string, IEnumerable<PropertyInfo>>();
        private static readonly Dictionary<string, IEnumerable<PropertyInfo>> ChecksumLists = new Dictionary<string, IEnumerable<PropertyInfo>>();
        [ThreadStatic]
        private static List<Type> _knownTypes;
        [ThreadStatic]
        private static Dictionary<object, int> _seenObjects;
        [ThreadStatic]
        private static List<object> _loadedObjects;
        [ThreadStatic]
        private static List<string> _propertyIds;

        [ThreadStatic]
        private static Stack<List<object>> _loStack;
        [ThreadStatic]
        private static Stack<Dictionary<object, int>> _soStack;
        [ThreadStatic]
        private static Stack<List<Type>> _ktStack;
        [ThreadStatic]
        private static Stack<List<string>> _piStack;
        [ThreadStatic]
        private static bool _isChecksum;
        [ThreadStatic]
        public static bool IgnoreIds;

        /// <summary>
        /// Arguments for a missing type event
        /// </summary>
        public class TypeMappingEventArgs : EventArgs
        {
            /// <summary>
            /// The missing types name
            /// </summary>
            public string TypeName = string.Empty;
            /// <summary>
            /// Supply a type to use instead
            /// </summary>
            public Type UseType = null;
        }

        /// <summary>
        /// Event that is fired if a particular type cannot be found
        /// </summary>
        public static event EventHandler<TypeMappingEventArgs> MapMissingType;


        private static void InvokeMapMissingType(TypeMappingEventArgs e)
        {
            EventHandler<TypeMappingEventArgs> handler = MapMissingType;
            if (handler != null)
                handler(null, e);
        }

        /// <summary>
        /// Put the serializer into Checksum mode
        /// </summary>
        public static bool IsChecksum
        {
            get
            {
                return _isChecksum;
            }
            set
            {
                _isChecksum = value;
            }
        }

        /// <summary>
        /// Deserialize to a type
        /// </summary>
        /// <param name="array"></param>
        /// <returns></returns>
        public static T Deserialize<T>(byte[] array) where T : class
        {
            return Deserialize(array) as T;

        }

        /// <summary>
        /// Deserialize from a stream to a type
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T Deserialize<T>(Stream stream) where T : class
        {
            return Deserialize(stream, null) as T;
        }

        /// <summary>
        /// Get a checksum for an item.  Checksums "should" be different 
        /// for every object that has a different "meaning".  You can
        /// flag properties as DoNotChecksum if that helps to keep decorative
        /// properties away from the checksum whilst including meaningful ones
        /// </summary>
        /// <param name="item">The object to checksum</param>
        /// <returns>A checksum string, this includes no illegal characters and can be used as a file name</returns>
        public static string GetChecksum(object item)
        {
            if (item == null)
                return "";
            byte[] checksum = new byte[] { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
            var isChecksum = SilverlightSerializer.IsChecksum;
            SilverlightSerializer.IsChecksum = true;
            var toBytes = SilverlightSerializer.Serialize(item);
            SilverlightSerializer.IsChecksum = isChecksum;

            for (var i = 0; i < toBytes.Length; i++)
            {
                checksum[i & 15] ^= toBytes[i];
            }
            return toBytes.Count().ToString() + Encode(checksum);
        }

        private static string Encode(byte[] checksum)
        {
            var s = Convert.ToBase64String(checksum);
            return s.Aggregate("", (current, c) => current + (Char.IsLetterOrDigit(c)
                                                                  ? c
                                                                  : Char.GetNumericValue(c)));
        }


        //Holds a reference to the custom serializers
        private static readonly Dictionary<Type, ISerializeObject> Serializers = new Dictionary<Type, ISerializeObject>();
        //Dictionary to ensure we only scan an assembly once
        private static readonly Dictionary<Assembly, bool> Assemblies = new Dictionary<Assembly, bool>();

        /// <summary>
        /// Register all of the custom serializers in an assembly
        /// </summary>
        /// <param name="assembly">Leave blank to register the assembly that the method is called from, or pass an assembly</param>
        public static void RegisterSerializationAssembly(Assembly assembly)
        {
            if (assembly == null)
                assembly = Assembly.GetCallingAssembly();
            if (Assemblies.ContainsKey(assembly))
                return;
            Assemblies[assembly] = true;
            ScanAllTypesForAttribute((tp, attr) =>
            {
                Serializers[((SerializerAttribute)attr).SerializesType] = Activator.CreateInstance(tp) as ISerializeObject;
            }, assembly, typeof(SerializerAttribute));
        }

        //Function to be called when scanning types
        internal delegate void ScanTypeFunction(Type type, Attribute attribute);

        /// <summary>
        /// Scan all of the types in an assembly for a particular attribute
        /// </summary>
        /// <param name="function">The function to call</param>
        /// <param name="assembly">The assembly to scan</param>
        /// <param name="attribute">The attribute to look for</param>
        internal static void ScanAllTypesForAttribute(ScanTypeFunction function, Assembly assembly, Type attribute)
        {
            try
            {
                foreach (var tp in assembly.GetTypes())
                {
                    if (attribute != null)
                    {
                        var attrs = Attribute.GetCustomAttributes(tp, attribute, false);
                        if (attrs != null)
                        {
                            foreach (var attr in attrs)
                                function(tp, attr);
                        }
                    }
                    else
                        function(tp, null);
                }
            }
            catch (Exception)
            {


            }
        }
        /// <summary>
        /// Dictionary of all the used objects to check if properties are different
        /// to those set during construction
        /// </summary>
        private static readonly Dictionary<Type, object> Vanilla = new Dictionary<Type, object>();
        /// <summary>
        /// Write persistence debugging information to the debug output window
        /// often used with Verbose
        /// </summary>
        public static bool IsLoud;
        /// <summary>
        /// Write all types, even if they are known, often used with Loud mode
        /// </summary>
        public static bool Verbose;

        /// <summary>
        ///   Caches and returns property info for a type
        /// </summary>
        /// <param name = "itm">The type that should have its property info returned</param>
        /// <returns>An enumeration of PropertyInfo objects</returns>
        /// <remarks>
        ///   It should be noted that the implementation converts the enumeration returned from reflection to an array as this more than double the speed of subsequent reads
        /// </remarks>
        private static IEnumerable<PropertyInfo> GetPropertyInfo(Type itm)
        {
            lock (PropertyLists)
            {
                IEnumerable<PropertyInfo> ret = null;
                Debug.Assert(itm.AssemblyQualifiedName != null);
                if (!IsChecksum)
                {
                    if (!PropertyLists.TryGetValue(itm.AssemblyQualifiedName, out ret))
                    {
                        ret = itm.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetCustomAttributes(typeof(DoNotSerialize), false).Count() == 0 && !(p.GetIndexParameters().Count() > 0) && (p.GetSetMethod() != null)).ToArray();
                        PropertyLists[itm.AssemblyQualifiedName] = ret;
                    }
                }
                else
                {
                    if (!ChecksumLists.TryGetValue(itm.AssemblyQualifiedName, out ret))
                    {
                        ret = itm.GetProperties(BindingFlags.Instance | BindingFlags.Public).Where(p => p.GetCustomAttributes(typeof(DoNotSerialize), false).Count() == 0 && p.GetCustomAttributes(typeof(DoNotChecksum), true).Count() == 0 && !(p.GetIndexParameters().Count() > 0) && (p.GetSetMethod() != null)).ToArray();
                        ChecksumLists[itm.AssemblyQualifiedName] = ret;
                    }
                }
                return IgnoreIds && ret != null
                           ? ret.Where(p => p.GetCustomAttributes(typeof(SerializerId), true).Count() == 0)
                           : ret;
            }
        }

        /// <summary>
        ///   Caches and returns field info for a type
        /// </summary>
        /// <param name = "itm">The type that should have its field info returned</param>
        /// <returns>An enumeration of FieldInfo objects</returns>
        /// <remarks>
        ///   It should be noted that the implementation converts the enumeration returned from reflection to an array as this more than double the speed of subsequent reads
        /// </remarks>
        private static IEnumerable<FieldInfo> GetFieldInfo(Type itm)
        {
            lock (FieldLists)
            {
                IEnumerable<FieldInfo> ret = null;
                if (FieldLists.ContainsKey(itm))
                    ret = FieldLists[itm];
                else
                {
                    ret = FieldLists[itm] = itm.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetField).Where(p => p.GetCustomAttributes(typeof(DoNotSerialize), false).Count() == 0).ToArray();
                }

                return IsChecksum ? ret.Where(p => p.GetCustomAttributes(typeof(DoNotChecksum), true).Count() == 0) : ret;

            }
        }

        /// <summary>
        ///   Returns a token that represents the name of the property
        /// </summary>
        /// <param name = "name">The name for which to return a token</param>
        /// <returns>A 2 byte token representing the name</returns>
        private static ushort GetPropertyDefinitionId(string name)
        {
            lock (_propertyIds)
            {
                var ret = _propertyIds.IndexOf(name);
                if (ret >= 0)
                    return (ushort)ret;
                _propertyIds.Add(name);
                return (ushort)(_propertyIds.Count - 1);
            }
        }

        public static object Deserialize(Stream inputStream)
        {
            return Deserialize(inputStream, null);
        }

        /// <summary>
        /// Deserializes from a stream, potentially into an existing instance
        /// </summary>
        /// <param name="inputStream">Stream to deserialize from</param>
        /// <param name="instance">Instance to use</param>
        /// <returns></returns>
        public static object Deserialize(Stream inputStream, object instance)
        {
            var v = Verbose;
            CreateStacks();
            try
            {
                _ktStack.Push(_knownTypes);
                _piStack.Push(_propertyIds);
                _loStack.Push(_loadedObjects);

                var rw = new BinaryReader(inputStream);
                var version = rw.ReadString();
                var count = rw.ReadInt32();
                if (version == "SerV3")
                    Verbose = rw.ReadBoolean();
                _propertyIds = new List<string>();
                _knownTypes = new List<Type>();
                _loadedObjects = new List<object>();
                for (var i = 0; i < count; i++)
                {
                    var typeName = rw.ReadString();
                    var tp = Type.GetType(typeName);
                    if (tp == null)
                    {
                        var map = new TypeMappingEventArgs
                        {
                            TypeName = typeName
                        };
                        InvokeMapMissingType(map);
                        tp = map.UseType;
                    }
                    if (!Verbose)
                        if (tp == null)
                            throw new ArgumentException(string.Format("Cannot reference type {0} in this context", typeName));
                    _knownTypes.Add(tp);
                }
                count = rw.ReadInt32();
                for (var i = 0; i < count; i++)
                {
                    _propertyIds.Add(rw.ReadString());
                }

                return DeserializeObject(rw, null, instance);
            }
            finally
            {
                _knownTypes = _ktStack.Pop();
                _propertyIds = _piStack.Pop();
                _loadedObjects = _loStack.Pop();
                Verbose = v;
            }
        }

        /// <summary>
        ///   Convert a previously serialized object from a byte array 
        ///   back into a .NET object
        /// </summary>
        /// <param name = "bytes">The data stream for the object</param>
        /// <returns>The rehydrated object represented by the data supplied</returns>
        public static object Deserialize(byte[] bytes)
        {
            using (MemoryStream inputStream = new MemoryStream(bytes))
            {
                return Deserialize(inputStream, null);
            }
        }

        /// <summary>
        ///   Convert a previously serialized object from a byte array 
        ///   back into a .NET object
        /// </summary>
        /// <param name = "bytes">The data stream for the object</param>
        /// <returns>The rehydrated object represented by the data supplied</returns>
        public static void DeserializeInto(byte[] bytes, object instance)
        {
            using (MemoryStream inputStream = new MemoryStream(bytes))
            {
                Deserialize(inputStream, instance);
            }
        }


        /// <summary>
        ///   Creates a set of stacks on the current thread
        /// </summary>
        private static void CreateStacks()
        {
            if (_piStack == null)
                _piStack = new Stack<List<string>>();
            if (_ktStack == null)
                _ktStack = new Stack<List<Type>>();
            if (_loStack == null)
                _loStack = new Stack<List<object>>();
            if (_soStack == null)
                _soStack = new Stack<Dictionary<object, int>>();
        }

        /// <summary>
        ///   Deserializes an object or primitive from the stream
        /// </summary>
        /// <param name = "reader">The reader of the binary file</param>
        /// <param name = "itemType">The expected type of the item being read (supports compact format)</param>
        /// <returns>The value read from the file</returns>
        /// <remarks>
        ///   The function is supplied with the type of the property that the object was stored in (if known) this enables
        ///   a compact format where types only have to be specified if they differ from the expected one
        /// </remarks>
        private static object DeserializeObject(BinaryReader reader, Type itemType, object instance)
        {
            var tpId = (ushort)reader.ReadUInt16();
            if (tpId == 0xFFFE)
                return null;

            //Lookup the value type if necessary
            if (tpId != 0xffff || itemType == null)
                itemType = _knownTypes[tpId];

            object obj = null;
            if (itemType != null)
            {
                //Check for custom serialization
                if (Serializers.ContainsKey(itemType))
                {
                    //Read the serializer and its data
                    var serializer = Serializers[itemType];
                    object[] data = DeserializeObject(reader, typeof(object[]), null) as object[];
                    return serializer.Deserialize(data);
                }

                //Check if this is a simple value and read it if so
                if (IsSimpleType(itemType))
                {
                    if (itemType.IsEnum)
                    {
                        return Enum.Parse(itemType, ReadValue(reader, typeof(int)).ToString(), true);
                    }
                    return ReadValue(reader, itemType);
                }
            }
            //See if we should lookup this object or create a new one
            var found = reader.ReadChar();
            if (found == 'S') //S is for Seen
                return _loadedObjects[reader.ReadInt32()];
            if (itemType != null)
            {
                //Otherwise create the object
                if (itemType.IsArray)
                {
                    int baseCount = reader.ReadInt32();

                    if (baseCount == -1)
                    {
                        return DeserializeMultiDimensionArray(itemType, reader, baseCount);
                    }
                    else
                    {
                        return DeserializeArray(itemType, reader, baseCount);
                    }
                }

                obj = instance ?? CreateObject(itemType);
                _loadedObjects.Add(obj);
            }
            //Check for collection types)
            if (obj is IDictionary)
                return DeserializeDictionary(obj as IDictionary, itemType, reader);
            if (obj is IList)
                return DeserializeList(obj as IList, itemType, reader);


            //Otherwise we are serializing an object
            return DeserializeObjectAndProperties(obj, itemType, reader);

        }

        /// <summary>
        ///   Deserializes an array of values
        /// </summary>
        /// <param name = "itemType">The type of the array</param>
        /// <param name = "reader">The reader of the stream</param>
        /// <returns>The deserialized array</returns>
        /// <remarks>
        ///   This routine optimizes for arrays of primitives and bytes
        /// </remarks>
        private static object DeserializeArray(Type itemType, BinaryReader reader, int count)
        {
            // If the count is -1 at this point, then it is being called from the
            // deserialization of a multi-dimensional array - so we need
            // to read the size of the array
            if (count == -1)
            {
                count = reader.ReadInt32();
            }

            //Get the expected element type
            var elementType = itemType.GetElementType();
            //Optimize for byte arrays
            if (elementType == typeof(byte))
            {
                var ret = reader.ReadBytes(count);
                _loadedObjects.Add(ret);
                return ret;
            }

            //Create an array of the correct type
            var array = Array.CreateInstance(elementType, count);
            _loadedObjects.Add(array);
            //Check whether the array contains primitives, if it does we don't
            //need to store the type of each member
            if (IsSimpleType(elementType))
                for (var l = 0; l < count; l++)
                {
                    array.SetValue(ReadValue(reader, elementType), l);
                }
            else
                for (var l = 0; l < count; l++)
                {
                    array.SetValue(DeserializeObject(reader, elementType, null), l);
                }
            return array;
        }

        /// <summary>
        ///   Deserializes a multi-dimensional array of values
        /// </summary>
        /// <param name = "itemType">The type of the array</param>
        /// <param name = "reader">The reader of the stream</param>
        /// <param name="count">The base size of the multi-dimensional array</param>
        /// <returns>The deserialized array</returns>
        /// <remarks>
        ///   This routine deserializes values serialized on a 'row by row' basis, and
        ///   calls into DeserializeArray to do this
        /// </remarks>
        private static object DeserializeMultiDimensionArray(Type itemType, BinaryReader reader, int count)
        {
            //Read the number of dimensions the array has
            var dimensions = reader.ReadInt32();
            var totalLength = reader.ReadInt32();

            int rowLength = 0;

            // Establish the length of each array element
            // and get the total 'row size'
            int[] lengths = new int[dimensions];
            int[] indices = new int[dimensions];

            for (int item = 0; item < dimensions; item++)
            {
                lengths[item] = reader.ReadInt32();
                rowLength += lengths[item];
                indices[item] = 0;
            }

            int cols = lengths[lengths.Length - 1];
            //int cols = dimensions == 1 ? 1 : lengths[lengths.Length - 1];

            //Get the expected element type
            var elementType = itemType.GetElementType();



            Array sourceArrays = Array.CreateInstance(elementType, lengths);
            DeserializeArrayPart(sourceArrays, 0, indices, itemType, reader);
            return sourceArrays;
        }

        private static void DeserializeArrayPart(Array sourceArrays, int i, int[] indices, Type itemType, BinaryReader binaryReader)
        {
            int length = sourceArrays.GetLength(i);
            for (var l = 0; l < length; l++)
            {
                indices[i] = l;
                if (i != sourceArrays.Rank - 2)
                    DeserializeArrayPart(sourceArrays, i + 1, indices, itemType, binaryReader);
                else
                {
                    Array sourceArray = (Array)DeserializeArray(itemType, binaryReader, -1);
                    int cols = sourceArrays.GetLength(i + 1);
                    for (int arrayStartIndex = 0; arrayStartIndex < cols; arrayStartIndex++)
                    {
                        indices[i + 1] = arrayStartIndex;
                        sourceArrays.SetValue(sourceArray.GetValue(arrayStartIndex), indices);
                    }
                }
            }
        }

        /// <summary>
        ///   Deserializes a dictionary from storage, handles generic types with storage optimization
        /// </summary>
        /// <param name = "o">The newly created dictionary</param>
        /// <param name = "itemType">The type of the dictionary</param>
        /// <param name = "reader">The binary reader for the current bytes</param>
        /// <returns>The dictionary object updated with the values from storage</returns>
        private static object DeserializeDictionary(IDictionary o, Type itemType, BinaryReader reader)
        {
            Type keyType = null;
            Type valueType = null;
            if (itemType.IsGenericType)
            {
                var types = itemType.GetGenericArguments();
                keyType = types[0];
                valueType = types[1];
            }

            var count = reader.ReadInt32();
            var list = new List<object>();
            for (var i = 0; i < count; i++)
            {
                list.Add(DeserializeObject(reader, keyType, null));
            }
            for (var i = 0; i < count; i++)
            {
                o[list[i]] = DeserializeObject(reader, valueType, null);
            }
            return o;
        }

        /// <summary>
        ///   Deserialize a list from the data stream
        /// </summary>
        /// <param name = "o">The newly created list</param>
        /// <param name = "itemType">The type of the list</param>
        /// <param name = "reader">The reader for the current bytes</param>
        /// <returns>The list updated with values from the stream</returns>
        private static object DeserializeList(IList o, Type itemType, BinaryReader reader)
        {
            Type valueType = null;
            if (itemType.IsGenericType)
            {
                var types = itemType.GetGenericArguments();
                valueType = types[0];
            }

            var count = reader.ReadInt32();
            var list = new List<object>();
            for (var i = 0; i < count; i++)
            {
                o.Add(DeserializeObject(reader, valueType, null));
            }
            return o;
        }

        /// <summary>
        ///   Deserializes a class based object that is not a collection, looks for both public properties and fields
        /// </summary>
        /// <param name = "o">The object being deserialized</param>
        /// <param name = "itemType">The type of the object</param>
        /// <param name = "reader">The reader for the current stream of bytes</param>
        /// <returns>The object updated with values from the stream</returns>
        private static object DeserializeObjectAndProperties(object o, Type itemType, BinaryReader reader)
        {
            DeserializeProperties(reader, itemType, o);
            DeserializeFields(reader, itemType, o);
            return o;
        }


        /// <summary>
        ///   Deserializes the properties of an object from the stream
        /// </summary>
        /// <param name = "reader">The reader of the bytes in the stream</param>
        /// <param name = "itemType">The type of the object</param>
        /// <param name = "o">The object to deserialize</param>
        private static void DeserializeProperties(BinaryReader reader, Type itemType, object o)
        {
            //Get the number of properties
            var propCount = reader.ReadByte();
            int length = 0;
            if (Verbose)
                length = reader.ReadInt32();
            if (o == null)
            {
                reader.BaseStream.Seek(length, SeekOrigin.Current);
                return;
            }
            for (var i = 0; i < propCount; i++)
            {
                //Get a property name identifier
                var propId = reader.ReadUInt16();
                //Lookup the name
                var propName = _propertyIds[propId];
                //Use the name to find the type
                var propType = itemType.GetProperty(propName);
                //Deserialize the value
                var value = DeserializeObject(reader, propType != null ? propType.PropertyType : null, null);
                if (propType != null && value != null)
                {
                    try
                    {
                        propType.SetValue(o, value, null);
                    }
                    catch (Exception)
                    {
                        //Suppress cases where the old value is no longer compatible with the new property type


                    }

                }
            }
        }

        /// <summary>
        ///   Deserializes the fields of an object from the stream
        /// </summary>
        /// <param name = "reader">The reader of the bytes in the stream</param>
        /// <param name = "itemType">The type of the object</param>
        /// <param name = "o">The object to deserialize</param>
        private static void DeserializeFields(BinaryReader reader, Type itemType, object o)
        {
            var fieldCount = reader.ReadByte();
            int length = 0;
            if (Verbose)
                length = reader.ReadInt32();
            if (o == null)
            {
                reader.BaseStream.Seek(length, SeekOrigin.Current);
                return;
            }
            for (var i = 0; i < fieldCount; i++)
            {
                var fieldId = reader.ReadUInt16();
                var fieldName = _propertyIds[fieldId];
                var fieldType = itemType.GetField(fieldName);
                var value = DeserializeObject(reader, fieldType != null ? fieldType.FieldType : null, null);
                if (fieldType != null && value != null)
                {
                    try
                    {
                        fieldType.SetValue(o, value);
                    }
                    catch (Exception)
                    {
                        //Suppress cases where the old value is no longer compatible with the new property type
                    }

                }
            }
        }


        public static void Serialize(object item, Stream outputStream)
        {
            CreateStacks();


            try
            {
                _ktStack.Push(_knownTypes);
                _piStack.Push(_propertyIds);
                _soStack.Push(_seenObjects);

                _propertyIds = new List<string>();
                _knownTypes = new List<Type>();
                _seenObjects = new Dictionary<object, int>();
                var strm = new MemoryStream();
                var wr = new BinaryWriter(strm);
                SerializeObject(item, wr, null);
                var outputWr = new BinaryWriter(outputStream);
                outputWr.Write("SerV3");
                outputWr.Write(_knownTypes.Count);
                //New, store the verbose property
                outputWr.Write(Verbose);
                foreach (var kt in _knownTypes)
                {
                    outputWr.Write(kt.AssemblyQualifiedName);
                }
                outputWr.Write(_propertyIds.Count);
                foreach (var pi in _propertyIds)
                {
                    outputWr.Write(pi);
                }
                strm.WriteTo(outputStream);
            }
            finally
            {
                _knownTypes = _ktStack.Pop();
                _propertyIds = _piStack.Pop();
                _seenObjects = _soStack.Pop();
            }

        }

        /// <summary>
        ///   Serialize an object into an array of bytes
        /// </summary>
        /// <param name = "item">The object to serialize</param>
        /// <returns>A byte array representation of the item</returns>
        public static byte[] Serialize(object item)
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                Serialize(item, outputStream);
                //Reset the verbose mode
                return outputStream.ToArray();
            }
        }

        /// <summary>
        ///   Serialize an object into an array of bytes
        /// </summary>
        /// <param name = "item">The object to serialize</param>
        /// <param name="makeVerbose">Whether the object should be serialized for forwards compatibility</param>
        /// <returns>A byte array representation of the item</returns>
        public static byte[] Serialize(object item, bool makeVerbose)
        {
            using (MemoryStream outputStream = new MemoryStream())
            {
                var v = Verbose;
                Verbose = makeVerbose;
                Serialize(item, outputStream);
                Verbose = v;
                //Reset the verbose mode
                return outputStream.ToArray();
            }
        }
        private static void SerializeObject(object item, BinaryWriter writer, Type propertyType)
        {
            if (item == null)
            {
                writer.Write((ushort)0xFFFE);
                return;
            }

            var itemType = item.GetType();
            Debug.Assert(itemType != null);

            //If this isn't a simple type, then this might be a subclass so we need to
            //store the type
            if (propertyType != itemType || Verbose)
            {
                //Write the type identifier
                var tpId = GetTypeId(itemType);
                writer.Write(tpId);
            }
            else
                //Write a dummy identifier
                writer.Write((ushort)0xFFFF);


            //Check for custom serialization
            if (Serializers.ContainsKey(itemType))
            {
                //If we have a custom serializer then use it!
                var serializer = Serializers[itemType];
                var data = serializer.Serialize(item);
                SerializeObject(data, writer, typeof(object[]));
                return;
            }


            //Check for simple types again
            if (IsSimpleType(itemType))
            {
                if (itemType.IsEnum)
                    WriteValue(writer, (int)item);
                else
                    WriteValue(writer, item);
                return;
            }

            //Check whether this object has been seen
            if (_seenObjects.ContainsKey(item))
            {
                writer.Write('S');
                writer.Write(_seenObjects[item]);
                return;
            }

            //We are going to serialize an object
            writer.Write('O');
            _seenObjects[item] = _seenObjects.Count;

            //Check for collection types)
            if (item is Array)
            {
                if (((Array)item).Rank == 1)
                {
                    SerializeArray(item as Array, itemType, writer);
                }
                else
                {
                    SerializeMultiDimensionArray(item as Array, itemType, writer);
                }
                return;
            }
            if (item is IDictionary)
            {
                SerializeDictionary(item as IDictionary, itemType, writer);
                return;
            }
            if (item is IList)
            {
                SerializeList(item as IList, itemType, writer);
                return;
            }


            //Otherwise we are serializing an object
            SerializeObjectAndProperties(item, itemType, writer);
        }

        private static void SerializeList(IList item, Type tp, BinaryWriter writer)
        {
            Type valueType = null;
            //Try to optimize the storage of types based on the type of list
            if (tp.IsGenericType)
            {
                var types = tp.GetGenericArguments();
                valueType = types[0];
            }

            writer.Write(item.Count);
            foreach (var val in item)
            {
                SerializeObject(val, writer, valueType);
            }
        }

        private static void SerializeDictionary(IDictionary item, Type tp, BinaryWriter writer)
        {
            Type keyType = null;
            Type valueType = null;
            //Try to optimise storage based on the type of dictionary
            if (tp.IsGenericType)
            {
                var types = tp.GetGenericArguments();
                keyType = types[0];
                valueType = types[1];
            }

            //Write out the size
            writer.Write(item.Count);
            //Serialize the pairs
            foreach (var key in item.Keys)
            {
                SerializeObject(key, writer, keyType);
            }
            foreach (var val in item.Values)
            {
                SerializeObject(val, writer, valueType);
            }
        }

        private static void SerializeArray(Array item, Type tp, BinaryWriter writer)
        {
            var length = item.Length;

            writer.Write(length);

            var propertyType = tp.GetElementType();
            //Special optimization for arrays of byte
            if (propertyType == typeof(byte))
                writer.Write((byte[])item, 0, length);
            //Special optimization for arrays of simple types
            //which don't need to have the entry type stored
            //for each item
            else if (IsSimpleType(propertyType))
                for (var l = 0; l < length; l++)
                {
                    WriteValue(writer, item.GetValue(l));
                }
            else
                for (var l = 0; l < length; l++)
                {
                    SerializeObject(item.GetValue(l), writer, propertyType);
                }
        }

        private static void SerializeMultiDimensionArray(Array item, Type tp, BinaryWriter writer)
        {

            // Multi-dimension serializer data is:
            // Int32: Ranks
            // Int32 (x number of ranks): length of array dimension 

            int dimensions = item.Rank;

            var length = item.GetLength(0);

            // Determine the number of cols being populated
            var cols = item.GetLength(item.Rank - 1);

            // Explicitly write this value, to denote that this is a multi-dimensional array
            // so it doesn't break the deserializer when reading values for existing arrays
            writer.Write((int)-1);
            writer.Write(dimensions);
            writer.Write(item.Length);

            var propertyType = tp.GetElementType();
            var indicies = new int[dimensions];

            // Write out the length of each array, if we are dealing with the first array
            for (int arrayStartIndex = 0; arrayStartIndex < dimensions; arrayStartIndex++)
            {
                indicies[arrayStartIndex] = 0;
                writer.Write(item.GetLength(arrayStartIndex));
            }

            SerializeArrayPart(item, 0, indicies, writer);
        }

        private static void SerializeArrayPart(Array item, int i, int[] indices, BinaryWriter writer)
        {
            var length = item.GetLength(i);
            for (var l = 0; l < length; l++)
            {
                indices[i] = l;
                if (i != item.Rank - 2)
                    SerializeArrayPart(item, i + 1, indices, writer);
                else
                {
                    Type arrayType = item.GetType().GetElementType();
                    var cols = item.GetLength(i + 1);

                    var baseArray = Array.CreateInstance(arrayType, cols);

                    // Convert the whole multi-dimensional array to be 'row' based
                    // and serialize using the existing code
                    for (int arrayStartIndex = 0; arrayStartIndex < cols; arrayStartIndex++)
                    {
                        indices[i + 1] = arrayStartIndex;
                        baseArray.SetValue(item.GetValue(indices), arrayStartIndex);
                    }

                    SerializeArray(baseArray, baseArray.GetType(), writer);
                }
            }


        }


        /// <summary>
        ///   Return whether the type specified is a simple type that can be serialized fast
        /// </summary>
        /// <param name = "tp">The type to check</param>
        /// <returns>True if the type is a simple one and can be serialized directly</returns>
        private static bool IsSimpleType(Type tp)
        {
            return tp.IsPrimitive || tp == typeof(DateTime) || tp == typeof(TimeSpan) || tp == typeof(string) || tp.IsEnum || tp == typeof(Guid) || tp == typeof(decimal);
        }

        private static void SerializeObjectAndProperties(object item, Type itemType, BinaryWriter writer)
        {
            lock (Vanilla)
            {
                if (Vanilla.ContainsKey(itemType) == false)
                {

                    Vanilla[itemType] = CreateObject(itemType);
                }
            }


            WriteProperties(itemType, item, writer);
            WriteFields(itemType, item, writer);
        }

        private static object CreateObject(Type itemType)
        {
            try
            {
                return Activator.CreateInstance(itemType);
            }
            catch (Exception)
            {
                return itemType.GetConstructor(new Type[] { }).Invoke(new object[] { });

            }

        }

        private static void WriteProperties(Type itemType, object item, BinaryWriter writer)
        {
            var propertyStream = new MemoryStream();
            var pw = new BinaryWriter(propertyStream);
            byte propCount = 0;

            //Get the properties of the object
            var properties = GetPropertyInfo(itemType);
            foreach (var property in properties)
            {
                if (IsChecksum && IsLoud)
                {
                    Debug.WriteLine(string.Format(" ---->     {0}  on {1}", property.Name, item.ToString()));
                }
                var value = property.GetValue(item, null);
                //Don't store null values
                if (value == null)
                    continue;
                //Don't store empty collections
                if (value is ICollection)
                    if ((value as ICollection).Count == 0)
                        continue;
                //Don't store empty arrays
                if (value is Array)
                    if ((value as Array).Length == 0)
                        continue;
                //Check whether the value differs from the default
                lock (Vanilla)
                {
                    if (value.Equals(property.GetValue(Vanilla[itemType], null)))
                        continue;
                }
                //If we get here then we need to store the property
                propCount++;
                pw.Write(GetPropertyDefinitionId(property.Name));
                SerializeObject(value, pw, property.PropertyType);
            }
            writer.Write(propCount);
            if (Verbose)
                writer.Write((int)propertyStream.Length);
            propertyStream.WriteTo(writer.BaseStream);
        }

        private static void WriteFields(Type itemType, object item, BinaryWriter writer)
        {
            var fieldStream = new MemoryStream();
            var fw = new BinaryWriter(fieldStream);
            byte fieldCount = 0;

            //Get the public fields of the object
            var fields = GetFieldInfo(itemType);
            foreach (var field in fields)
            {
                var value = field.GetValue(item);
                //Don't store null values
                if (value == null)
                    continue;
                //Don't store empty collections
                if (value is ICollection)
                    if ((value as ICollection).Count == 0)
                        continue;
                //Don't store empty arrays
                if (value is Array)
                    if ((value as Array).Length == 0)
                        continue;
                //Check whether the value differs from the default
                lock (Vanilla)
                {
                    if (value.Equals(field.GetValue(Vanilla[itemType])))
                        continue;
                }
                //if we get here then we need to store the field
                fieldCount++;
                fw.Write(GetPropertyDefinitionId(field.Name));
                SerializeObject(value, fw, field.FieldType);
            }
            writer.Write(fieldCount);
            if (Verbose)
                writer.Write((int)fieldStream.Length);
            fieldStream.WriteTo(writer.BaseStream);
        }

        /// <summary>
        ///   Write a basic untyped value
        /// </summary>
        /// <param name = "writer">The writer to commit byte to</param>
        /// <param name = "value">The value to write</param>
        private static void WriteValue(BinaryWriter writer, object value)
        {
            if (value is string)
                writer.Write((string)value);
            else if (value == null)
                writer.Write("~~NULL~~");
            else if (value is decimal)
            {
                int[] array = Decimal.GetBits((Decimal)value);
                SerializeObject(array, writer, typeof(int[]));
            }
            else if (value is float)
                writer.Write((float)value);
            else if (value is bool)
                writer.Write((bool)value
                                 ? 'Y'
                                 : 'N');
            else if (value is Guid)
                writer.Write(value.ToString());
            else if (value is DateTime)
                writer.Write(((DateTime)value).Ticks);
            else if (value is TimeSpan)
                writer.Write(((TimeSpan)value).Ticks);
            else if (value is char)
                writer.Write((char)value);
            else if (value is ushort)
                writer.Write((ushort)value);
            else if (value is double)
                writer.Write((double)value);
            else if (value is ulong)
                writer.Write((ulong)value);
            else if (value is int)
                writer.Write((int)value);
            else if (value is uint)
                writer.Write((uint)value);
            else if (value is byte)
                writer.Write((byte)value);
            else if (value is long)
                writer.Write((long)value);
            else if (value is short)
                writer.Write((short)value);
            else if (value is sbyte)
                writer.Write((sbyte)value);
            else
                writer.Write((int)value);
        }

        /// <summary>
        ///   Read a basic value from the stream
        /// </summary>
        /// <param name = "reader">The reader with the stream</param>
        /// <param name = "tp">The type to read</param>
        /// <returns>The hydrated value</returns>
        private static object ReadValue(BinaryReader reader, Type tp)
        {

            if (tp == typeof(string))
            {
                var retString = reader.ReadString();

                return retString == "~~NULL~~"
                           ? null
                           : retString;
            }
            if (tp == typeof(bool))
                return reader.ReadChar() == 'Y';
            if (tp == typeof(decimal))
            {
                var array = DeserializeObject(reader, typeof(int[]), null) as int[];
                return new Decimal(array);
            }
            if (tp == typeof(DateTime))
                return new DateTime(reader.ReadInt64());
            if (tp == typeof(TimeSpan))
                return new TimeSpan(reader.ReadInt64());
            if (tp == typeof(float))
                return reader.ReadSingle();
            if (tp == typeof(char))
                return reader.ReadChar();
            if (tp == typeof(ushort))
                return reader.ReadUInt16();
            if (tp == typeof(double))
                return reader.ReadDouble();
            if (tp == typeof(ulong))
                return reader.ReadUInt64();
            if (tp == typeof(int))
                return reader.ReadInt32();
            if (tp == typeof(uint))
                return reader.ReadUInt32();
            if (tp == typeof(byte))
                return reader.ReadByte();
            if (tp == typeof(long))
                return reader.ReadInt64();
            if (tp == typeof(short))
                return reader.ReadInt16();
            if (tp == typeof(sbyte))
                return reader.ReadSByte();
            if (tp == typeof(Guid))
                return new Guid(reader.ReadString());
            return reader.ReadInt32();
        }

        /// <summary>
        ///   Logs a type and returns a unique token for it
        /// </summary>
        /// <param name = "tp">The type to retrieve a token for</param>
        /// <returns>A 2 byte token representing the type</returns>
        private static ushort GetTypeId(Type tp)
        {
            var tpId = _knownTypes.IndexOf(tp);

            if (tpId < 0)
            {
                tpId = _knownTypes.Count;
                _knownTypes.Add(tp);
            }
            return (ushort)tpId;
        }
    }
}
