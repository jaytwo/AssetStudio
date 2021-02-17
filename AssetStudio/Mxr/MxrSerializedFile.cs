using AssetStudio.Mxr.Classes;
using Ionic.Zlib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio.Mxr
{
    class MxrSerializedFile : SerializedFile
    {
        public MxrSerializedFile(AssetsManager assetsManager, string fullName, EndianBinaryReader reader)
            : base(assetsManager, fullName, reader) { }

        protected override void Read()
        {
            using (var stream1 = Decompress(23, out var bytesRead))
            {
                header = new SerializedFileHeader();
                header.m_MetadataSize = (uint)bytesRead;
                header.m_FileSize = reader.BaseStream.Length;
                //header.m_Version = reader.ReadUInt32();
                header.m_DataOffset = 28 + bytesRead;
            }

            using (var stream2 = Decompress(header.m_DataOffset, out _))
            using (var reader2 = new EndianBinaryReader(stream2, EndianType.LittleEndian))
            {
                m_Types = new List<SerializedType>();
                m_Objects = new List<ObjectInfo>();
                Objects = new List<Object>();
                ObjectsDic = new Dictionary<long, Object>();

                while (true)
                {
                    var objectInfo = new ObjectInfo();
                    objectInfo.typeID = reader2.ReadByte();
                    objectInfo.classID = reader2.ReadInt32();

                    switch (objectInfo.typeID)
                    {
                        // Header
                        case 224:
                            if (reader2.ReadByte() != 225)
                                throw new InvalidDataException();

                            var length = reader2.ReadInt32();
                            objectInfo.byteStart = reader2.Position;

                            reader2.Position += (length - 1) * 5 + 1;
                            break;

                        // Resource
                        case 16:
                            if (reader2.ReadByte() != 17)
                                throw new InvalidDataException();

                            objectInfo.m_PathID = reader2.ReadInt32();

                            if (reader2.ReadByte() != 32 || reader2.ReadByte() != 16)
                                throw new InvalidDataException();

                            var classId = (MxrClassIDType)objectInfo.classID;
                            if (classId != MxrClassIDType.Model)
                                return;

                            objectInfo.byteStart = reader2.Position;
                            AddObject(ReadAsset(new ObjectReader(reader2, this, objectInfo), classId));
                            break;

                        default:
                            throw new InvalidDataException();
                    }

                    objectInfo.byteSize = (uint)(reader2.Position - objectInfo.byteStart);
                    m_Objects.Add(objectInfo);
                }
            }
        }

        private Object ReadAsset(ObjectReader objectReader, MxrClassIDType classId)
        {
            switch (classId)
            {
                case MxrClassIDType.Model:
                    return new MxrModel(objectReader);
                default:
                    return new MxrNamedObject(objectReader);
            }
        }

        private MemoryStream Decompress(long start, out long bytesRead)
        {
            reader.BaseStream.Position = start;

            var headerType = reader.ReadByte();
            var header = reader.ReadBytes(headerType == 240 ? 4 : 0);

            var lengthBytes = reader.ReadBytes(4).Select(b => (uint)b).ToArray();
            var length = lengthBytes[1] | (lengthBytes[2] << 8) | (lengthBytes[3] << 16) | ((lengthBytes[0] & 0x7F) << 24);
            
            var decompressed = new MemoryStream();

            using (var deflate = new ZlibStream(reader.BaseStream, CompressionMode.Decompress, true))
            using (var source = new EndianBinaryReader(deflate))
                while (true)
                {
                    var bytes = source.ReadBytes(1024);
                    if (bytes.Length == 0)
                    {
                        bytesRead = deflate.TotalIn;
                        decompressed.Position = 0;
                        return decompressed;
                    }

                    decompressed.Write(bytes, 0, bytes.Length);
                }
        }
    }
}