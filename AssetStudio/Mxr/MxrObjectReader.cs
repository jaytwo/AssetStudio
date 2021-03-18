using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetStudio.Mxr
{
    static class MxrObjectReader
    {
        public static Dictionary<T, int> Read<T>(NamedObject namedObject,
            ClassIDType type,
            Func<ObjectReader, Dictionary<T, int>, T, bool> readField,
            int discardInitialBytes = 1,
            bool withHeader = true) where T : Enum
        {
            namedObject.type = type;

            var objectReader = namedObject.reader;
            objectReader.Reset();

            if (withHeader)
            {
                if (objectReader.ReadByte() != 0 ||
                    objectReader.ReadByte() != 0 ||
                    objectReader.ReadByte() != 0 ||
                    objectReader.ReadByte() != 0 ||
                    objectReader.ReadByte() != 0 ||
                    objectReader.ReadByte() != 1)
                {
                    throw new InvalidDataException();
                }

                namedObject.m_Name = ReadString(objectReader);

                if (objectReader.ReadByte() != 2)
                    throw new InvalidDataException();

                var unknown = objectReader.ReadBytes(7);

                if (objectReader.ReadByte() != 64)
                    throw new InvalidDataException();

                var sourceChars = objectReader.ReadBytes(objectReader.ReadByte() + objectReader.ReadByte());
                var source = Encoding.GetEncoding(932).GetString(sourceChars).TrimStart('.', '\\', '\0');

                if (objectReader.ReadByte() != 255)
                    throw new InvalidDataException();
            }

            var fieldValues = new Dictionary<T, int>();
            objectReader.ReadBytes(discardInitialBytes);

            while (true)
            {
                // Check for end of file
                if (objectReader.Position == objectReader.BaseStream.Length)
                    return fieldValues;

                var fieldByte = objectReader.ReadByte();
                var field = (T)Enum.ToObject(typeof(T), fieldByte);
                
                if (!readField(objectReader, fieldValues, field))
                {
                    fieldValues[field] = objectReader.ReadByte();
                    return fieldValues;
                }
            }
        }

        public static string ReadString(ObjectReader objectReader) =>
            Encoding.GetEncoding(932).GetString(objectReader.ReadBytes(objectReader.ReadInt32()));
    }
}