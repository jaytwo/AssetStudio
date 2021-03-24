using AssetStudio.Mxr.Classes;
using Ionic.Zlib;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio.Mxr
{
    class MxrSerializedFile : SerializedFile
    {
        public MxrStringSubstituter StringSubstituter { get; private set; }

        public MxrSerializedFile(AssetsManager assetsManager, string fullName, EndianBinaryReader reader)
            : base(assetsManager, fullName, reader) { }

        protected override void Read()
        {
            StringSubstituter = new MxrStringSubstituter(fullName);

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

                if (reader2.ReadByte() == 224)
                    reader2.Position--;

                while (true)
                {
                    var objectInfo = new ObjectInfo();
                    objectInfo.typeID = reader2.ReadByte();

                    if (objectInfo.typeID != 255)
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

                            objectInfo.m_PathID = GetPathID(objectInfo.classID, reader2.ReadInt32());

                            if (reader2.ReadByte() != 32 || reader2.ReadByte() != 16)
                                throw new InvalidDataException();

                            var classId = (MxrClassIDType)objectInfo.classID;
                            objectInfo.byteStart = reader2.Position;

                            var namedObject = ReadAsset(new ObjectReader(reader2, this, objectInfo), classId);
                            namedObject.byteSize = (uint)(reader2.Position - objectInfo.byteStart);
                            AddObject(namedObject);
                            break;

                        case 255:
                            // Table
                            if (reader2.ReadByte() != 48)
                                throw new InvalidDataException();

                            var totalLength = reader2.ReadInt32();
                            var totalStart = reader2.BaseStream.Position;
                            var scriptBytes = reader2.ReadBytes(reader2.ReadInt32());
                            var pathId = -1;

                            objectInfo.classID = (int)MxrClassIDType.Script;
                            objectInfo.m_PathID = pathId--;
                            objectInfo.byteStart = reader2.Position;

                            var tableObject = new MxrTable(new ObjectReader(reader2, this, objectInfo));
                            tableObject.byteSize = objectInfo.byteSize = (uint)(reader2.Position - objectInfo.byteStart);
                            m_Objects.Add(objectInfo);
                            AddObject(tableObject);

                            if (reader2.ReadInt16() != 0)
                                throw new InvalidDataException();

                            // Events
                            objectInfo = new ObjectInfo();
                            objectInfo.classID = (int)MxrClassIDType.Script;
                            objectInfo.m_PathID = pathId--;
                            objectInfo.byteStart = reader2.Position;

                            var eventsObject = new MxrEvents(new ObjectReader(reader2, this, objectInfo));
                            eventsObject.Read(tableObject.Strings, totalStart + totalLength - 1);
                            eventsObject.byteSize = objectInfo.byteSize = (uint)(reader2.Position - objectInfo.byteStart);
                            m_Objects.Add(objectInfo);
                            AddObject(eventsObject);

                            while ((new byte[] { 32, 0 }).Contains(reader2.ReadByte())) { }
                            reader2.Position--;

                            // Scores and tracks
                            while (true)
                            {
                                objectInfo = new ObjectInfo();
                                objectInfo.classID = reader2.ReadByte();
                                objectInfo.m_PathID = pathId--;
                                objectInfo.byteStart = --reader2.Position;

                                if (objectInfo.classID != (int)MxrClassIDType.Score)
                                    return;

                                var scoreObject = new MxrScore(new ObjectReader(reader2, this, objectInfo));
                                scoreObject.byteSize = objectInfo.byteSize = (uint)(reader2.Position - objectInfo.byteStart);
                                m_Objects.Add(objectInfo);
                                AddObject(scoreObject);

                                while (reader2.ReadByte() == 32)
                                {
                                    if (reader2.ReadByte() != 16)
                                        throw new InvalidDataException();

                                    var trackIndex = reader2.ReadInt32();

                                    objectInfo = new ObjectInfo();
                                    objectInfo.classID = (int)MxrClassIDType.Track;
                                    objectInfo.m_PathID = pathId--;
                                    objectInfo.byteStart = reader2.Position;

                                    var trackObject = new MxrTrack(new ObjectReader(reader2, this, objectInfo), trackIndex);
                                    trackObject.byteSize = objectInfo.byteSize = (uint)(reader2.Position - objectInfo.byteStart);
                                    m_Objects.Add(objectInfo);
                                    AddObject(trackObject);
                                }

                                reader2.Position--;
                                MxrObjectReader.Read<Fields.ScoreField>(scoreObject.reader, scoreObject.ReadField);
                            }

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
                case MxrClassIDType.Texture:
                case MxrClassIDType.Bitmap:
                    return new MxrTexture(objectReader);
                case MxrClassIDType.Text:
                    return new MxrText(objectReader);
                case MxrClassIDType.Wave:
                case MxrClassIDType.Sound3d:
                    return new MxrWave(objectReader);
                case MxrClassIDType.Midi:
                    return new MxrMidi(objectReader);
                case MxrClassIDType.Camera:
                    return new MxrCamera(objectReader);
                case MxrClassIDType.Light:
                    return new MxrLight(objectReader);
                case MxrClassIDType.Movie:
                    return new MxrMovie(objectReader);
                case MxrClassIDType.Ear:
                    return new MxrEar(objectReader);
                default:
                    throw new InvalidDataException();
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

        public static long GetPathID(int classId, int index) => (classId + 1) * 1000 + index;
    }
}