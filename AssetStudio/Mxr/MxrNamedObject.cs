using System.IO;
using System.Text;

namespace AssetStudio.Mxr
{
    public class MxrNamedObject : NamedObject
    {
        public byte[] m_Flags;
        public string m_Source;

        public MxrNamedObject(ObjectReader objectReader)
            : base(objectReader)
        {
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

        protected virtual void Read(ObjectReader objectReader) { }

        protected static string ReadString(ObjectReader objectReader) =>
            Encoding.GetEncoding(932).GetString(objectReader.ReadBytes(objectReader.ReadInt32()));
    }
}
