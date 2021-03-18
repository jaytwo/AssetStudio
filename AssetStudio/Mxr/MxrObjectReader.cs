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
            Action<ObjectReader, Dictionary<T, int>, T> readField,
            int discardInitialBytes = 1,
            Func<byte, bool> endCondition = null,
            byte headerLevel = 2) where T : Enum
        {
            if (endCondition == null)
                endCondition = DefaultEndCondition;

            namedObject.type = type;

            var objectReader = namedObject.reader;
            objectReader.Reset();

            switch (headerLevel)
            {
                case 1:
                    namedObject.m_Name = ReadString(objectReader);
                    break;

                case 2:
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
                    break;
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

                if (endCondition(fieldByte))
                {
                    fieldValues[field] = objectReader.ReadByte();
                    return fieldValues;
                }
                
                readField(objectReader, fieldValues, field);
            }
        }

        private static bool DefaultEndCondition(byte fieldByte) => fieldByte == 255;

        public static string ReadString(ObjectReader objectReader) =>
            Encoding.GetEncoding(932).GetString(objectReader.ReadBytes(objectReader.ReadInt32()));
    }
}