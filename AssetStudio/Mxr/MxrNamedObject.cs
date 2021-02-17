using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetStudio.Mxr
{
    public abstract class MxrNamedObject : NamedObject
    {
        public byte[] m_Flags;
        public string m_Source;

        public MxrNamedObject(ObjectReader objectReader, ClassIDType type)
            : base(objectReader)
        {
            this.type = type;
            objectReader.Reset();

            if (objectReader.ReadByte() != 0 ||
                objectReader.ReadByte() != 0 ||
                objectReader.ReadByte() != 0 ||
                objectReader.ReadByte() != 0 ||
                objectReader.ReadByte() != 0 ||
                objectReader.ReadByte() != 1)
            {
                throw new InvalidDataException();
            }

            m_Name = ReadString(objectReader);

            if (objectReader.ReadByte() != 2)
                throw new InvalidDataException();

            m_Flags = objectReader.ReadBytes(7);

            if (objectReader.ReadByte() != 64)
                throw new InvalidDataException();

            var sourceChars = objectReader.ReadBytes(objectReader.ReadByte() + objectReader.ReadByte());
            m_Source = Encoding.GetEncoding(932).GetString(sourceChars).TrimStart('.', '\\', '\0');

            if (objectReader.ReadByte() != 255)
                throw new InvalidDataException();

            Read(objectReader);
            byteSize = (uint)(objectReader.Position - objectReader.byteStart);
        }

        protected abstract void Read(ObjectReader objectReader);

        protected static string ReadString(ObjectReader objectReader) =>
            Encoding.GetEncoding(932).GetString(objectReader.ReadBytes(objectReader.ReadInt32()));

        protected static Dictionary<T, int> Read<T>(ObjectReader objectReader,
            Action<ObjectReader, Dictionary<T, int>, T> readField,
            int discardInitialBytes = 1,
            byte endByte = 255) where T : Enum
        {
            var fieldValues = new Dictionary<T, int>();
            objectReader.ReadBytes(discardInitialBytes);

            while (true)
            {
                var eof = objectReader.Position == objectReader.BaseStream.Length;
                var fieldByte = eof ? endByte : objectReader.ReadByte();
                var field = (T)Enum.ToObject(typeof(T), fieldByte);

                if (fieldByte == endByte)
                {
                    if (!eof)
                        fieldValues[field] = objectReader.ReadByte();

                    return fieldValues;
                }
                else readField(objectReader, fieldValues, field);
            }
        }
    }
}