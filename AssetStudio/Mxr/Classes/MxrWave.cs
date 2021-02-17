using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    enum WaveField
    {
        RiffData = 16,
        UnknownByte = 131
    }

    class MxrWave : NamedObject
    {
        private const int WAVE_FORMAT_PCM = 0x0001;
        private const int WAVE_FORMAT_DVI_ADPCM = 0x0011;

        public MxrWave(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<WaveField>(this, ClassIDType.AudioClip, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<WaveField, int> fieldValues, WaveField field)
        {
            switch (field)
            {
                case WaveField.RiffData:
                    var unknown1 = objectReader.ReadBytes(6);
                    var format = objectReader.ReadUInt16();
                    var channels = objectReader.ReadUInt16();
                    var samplesPerSec = objectReader.ReadUInt32();
                    var avgBytesPerSec = objectReader.ReadUInt32();
                    var blockAlign = objectReader.ReadUInt16();
                    var bitsPerSample = objectReader.ReadUInt16();
                    var extraSize = objectReader.ReadUInt16();

                    var unknown2 = objectReader.ReadBytes(6);
                    var fmtLength = format == WAVE_FORMAT_PCM ? 16 : (18 + extraSize);
                    var dataLength = (int)objectReader.ReadUInt32() - extraSize;

                    if (format == WAVE_FORMAT_DVI_ADPCM)
                    {
                        var samplesPerBlock = objectReader.ReadUInt16();
                    }

                    objectReader.ReadBytes(dataLength);
                    break;

                case WaveField.UnknownByte:
                    fieldValues.Add(field, objectReader.ReadByte());
                    break;

                default:
                    fieldValues.Add(field, objectReader.ReadInt32());
                    break;
            }
        }
    }
}