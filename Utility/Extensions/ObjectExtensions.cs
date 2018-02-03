namespace Bee.Eee.Utility.Extensions.ObjectExtensions
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Serialization;
using System.Xml.Linq;
    using System.Runtime.Serialization;

    public static class ObjectExtensions
    {
        public static string ToXml(this object thing)
        {
            var serializer = new XmlSerializer(thing.GetType());
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, thing);
                return writer.ToString();
            }
        }

        public static string ToXml(this object thing, XmlAttributeOverrides overrides)
        {
            var serializer = new XmlSerializer(thing.GetType(), overrides);
            using (var writer = new StringWriter())
            {
                serializer.Serialize(writer, thing);
                return writer.ToString();
            }
        }

        public static T FromXml<T>(string xml)
        {
            var serializer = new XmlSerializer(typeof(T));
            using( var reader = new StringReader(xml))
            {
                object o = serializer.Deserialize(reader);
                return (T)o;
            }
        }

        public static T FromXml<T>(Type type, string xml)
        {
            var serializer = new XmlSerializer(type);
            using (var reader = new StringReader(xml))
            {
                object o = serializer.Deserialize(reader);
                return (T)o;
            }
        }

        public static T FromXml<T>(string xml, XmlAttributeOverrides overrides)
        {
            var serializer = new XmlSerializer(typeof(T), overrides);
            using (var reader = new StringReader(xml))
            {
                object o = serializer.Deserialize(reader);
                return (T)o;
            }
        }       

        public static string ToDataContractXml(this object o)
        {
            if (null == o)
                throw new ArgumentNullException("o");

            var serializer = new DataContractSerializer(o.GetType());
            using (var writer = new StringWriter())
            using (var xmlWriter = XmlWriter.Create(writer))
            {
                serializer.WriteObject(xmlWriter, o);
                xmlWriter.Flush();
                return writer.ToString();
            }
        }

        public static object FromDataContractXml(Type type, string xml)
        {
            var serializer = new DataContractSerializer(type);
            using (var reader = new StringReader(xml))
            using (var xmlReader = XmlReader.Create(reader))
            {
                return serializer.ReadObject(xmlReader);
            }
        }

        public static T FromDataContractXml<T>(Type type, string xml)
        {
            return (T)FromDataContractXml(type, xml);
        }

        /// <summary>
        /// Converts a SOAP string to an object
        /// </summary>
        /// <typeparam name="T">Object type</typeparam>
        /// <param name="soap">SOAP string</param>
        /// <returns>The object of the specified type</returns>
        public static T ToObject<T>(string soap)
        {
            if (string.IsNullOrEmpty(soap))
            {
                throw new ArgumentException("SOAP can not be null/empty");
            }

            StringReader stringReader = null;
            try
            {
                stringReader = new StringReader(soap);
                using (var reader = new NamespaceIgnorantXmlTextReader(stringReader))
                {
                    stringReader = null;
                    var formatter = new XmlSerializer(typeof(T));
                    return (T)formatter.Deserialize(reader);
                }
            }
            finally
            {
                if (stringReader != null)
                    stringReader.Dispose();
            }
        }

        /// <summary>
        /// helper class to ignore namespaces when de-serializing
        /// </summary>
        internal class NamespaceIgnorantXmlTextReader : XmlTextReader
        {
            public NamespaceIgnorantXmlTextReader(TextReader reader)
                : base(reader) { }

            public override string NamespaceURI { get { return ""; } }
        }

        private static readonly char[] NullChars = new[] { '\0' };

        /// <summary>
        /// Package the passed in object into a byte array
        /// </summary>
        /// <param name="o">The object to pass</param>
        /// <param name="attribute">A custom attribute for the object</param>
        /// <returns>A binary representation of the passed in object</returns>
        public static byte[] ToBinary(this object o, object attribute)
        {
            Type t = o.GetType();
            if (t == typeof(int))
                return BitConverter.GetBytes((int)o);
            else if (t == typeof(uint))
                return BitConverter.GetBytes((uint)o);
            else if (t == typeof(double))
                return BitConverter.GetBytes((double)o);
            else if (t == typeof(byte[]))
                return (byte[])o;
            else if (t == typeof(string))
            {
                byte[] stringBytes = Encoding.UTF8.GetBytes(o as string);
                var attr = attribute as StringAttr;
                if (null == attr)
                {
                    throw new Exception("Must have a StringAttr custom attribute for use with this function");
                }
                else
                {
                    var stringTrimmedValue = new byte[attr.MaxStringSize];
                    int copyAmount = (attr.NullTerminated) ? attr.MaxStringSize - 1 : attr.MaxStringSize;
                    stringBytes.CopyTo(stringTrimmedValue, 0);
                    return stringTrimmedValue;
                }

            }
            else if (t == typeof(DateTime))
            {                                
                long val = ((DateTime)o).ToFileTime();
                return BitConverter.GetBytes(val);
            }

            return null;
        }

        public static object FromData(byte[] data, ref int index, Type type, object attribute, bool dwordAlign)
        {
            object retVal = null;
            if (type == typeof(int))
            {
                retVal = BitConverter.ToInt32(data, index);
                index += sizeof(int);
            }
            else if (type == typeof(uint))
            {
                retVal = BitConverter.ToUInt32(data, index);
                index += sizeof(uint);
            }
            else if (type == typeof(byte[]))
            {
                var attr = attribute as ByteArrayAttr;
                if (null == attr)
                    throw new Exception("Must have a ByteAttryAttr custom attribute for use with this function");

                var copy = new byte[attr.Size];
                int missalignment = data.Length - index - attr.Size;
                if (missalignment < 0)
                {
                    throw new ArgumentException(
                        string.Format("Unable to parse ByteArray because {0} bytes were missing from the source array.", -missalignment));
                }
                
                Array.Copy(data, index, copy, 0, attr.Size);
                retVal = copy;
                index += attr.Size;
            }
            else if (type == typeof(string))
            {
                var attr = attribute as StringAttr;
                if (null == attr)
                {
                    throw new Exception("Must have a StringAttr custom attribute for use with this function");
                }
                else
                {
                    retVal = Encoding.UTF8.GetString(data, index, attr.MaxStringSize).Trim(NullChars);
                    index += attr.MaxStringSize;
                }
            }
            else if (type == typeof(DateTime))
            {
                long val = BitConverter.ToInt64(data, index);
                index += sizeof(long);
                retVal = DateTime.FromFileTime(val);
            }
            else if (type == typeof(double))
            {
                retVal = BitConverter.ToDouble(data, index);
                index += sizeof(double);
            }
            else
            {
                throw new Exception(string.Format("Field of type {0} not handled.", type.ToString()));
            }

            if (dwordAlign)
                index = index + (index & 1);

            return retVal;
        }

        public static string TypeName(this object p)
        {
            return p == null ? string.Empty : p.GetType().Name;
        }

        public static bool SameType(this object baseObject, Type comparisonType)
        {
            if (baseObject == null)
                return false;

            if (comparisonType == null)
                throw new ArgumentNullException("comparisonType");

            return baseObject.GetType().FullName.Equals(comparisonType.FullName, StringComparison.InvariantCulture);
        }

        public static bool SameType(this object aObject, object bObject)
        {
            if (aObject == null)
                return false;

            if (bObject == null)
                return false;

            string aObjectType = aObject.GetType().FullName;
            string bObjectType = bObject.GetType().FullName;
            return aObjectType.Equals(bObjectType, StringComparison.InvariantCulture);
        }
    }

    /// <summary>
    /// Used by the ToBinary extension to see how to handle a string
    /// </summary>
    public class StringAttr : Attribute
    {
        public int MaxStringSize { get; set; }
        public bool NullTerminated { get; set; }

        public StringAttr(int maxStringSize)
        {
            this.MaxStringSize = maxStringSize;
            this.NullTerminated = false;
        }

        public StringAttr(int maxStringSize, bool nullTerminated)
        {
            this.MaxStringSize = maxStringSize;
            this.NullTerminated = nullTerminated;
        }

    }

    /// <summary>
    /// Used by the ToBinary extension to see how to handle a string
    /// </summary>
    public class ByteArrayAttr : Attribute
    {
        public int Size { get; set; }

        public ByteArrayAttr(int size)
        {
            this.Size = size;
        }

        public static byte[] MakeByteField(object o, string fieldName)
        {
            Type t = o.GetType();
            var fi = t.GetField(fieldName);
            if (fi == null)
                throw new Exception(string.Format("Unable to find field {0} in object.", fieldName));

            var attr = fi.GetCustomAttributes(typeof(ByteArrayAttr), false).FirstOrDefault() as ByteArrayAttr;
            if (null == attr)
                throw new Exception(string.Format("Field {0} doesn't have the ByteArrayAttr custom attribute can't make byte field.", fi.Name));

            return new byte[attr.Size];

        }
    }

    public static class MemoryStreamExtensions
    {
        public static void Write(this MemoryStream ms, byte[] ar)
        {
            ms.Write(ar, 0, ar.Length);
        }

        public static void WriteAligned(this MemoryStream ms, byte[] ar)
        {
            ms.Write(ar, 0, ar.Length);
            long pos = ms.Position;
            // dword align
            ms.Position = pos + (pos & 1);
        }
    }

    public static class XElementExtensions
    {
        public static string ExtractElementValue(this XElement root, params string[] names)
        {
            XElement currentElement = root;
            for (int i = 0; i < names.Length; i++)
            {
                currentElement = currentElement.Element(names[i]);
                if (null == currentElement)
                    return null;
            }

            return currentElement.Value;
        }
    }
}
