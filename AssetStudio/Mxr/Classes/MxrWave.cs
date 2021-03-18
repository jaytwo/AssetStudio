using AssetStudio.Mxr.Fields;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    class MxrWave : AudioClip
    {
        private const int WAVE_FORMAT_PCM = 0x0001;
        private const int WAVE_FORMAT_DVI_ADPCM = 0x0011;

        public MxrWave(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<WaveField>(this, ClassIDType.AudioClip, ReadField);
        }

        private bool ReadField(ObjectReader objectReader, Dictionary<WaveField, int> fieldValues, WaveField field)
        {
            switch (field)
            {
                case WaveField.End:
                    return false;

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

                    var audioData = new MemoryStream();
                    using (var destination = new BinaryWriter(audioData, Encoding.ASCII, true))
                    {
                        destination.Write(Encoding.ASCII.GetBytes("RIFF"));
                        destination.Write(dataLength + 20 + fmtLength);
                        destination.Write(Encoding.ASCII.GetBytes("WAVEfmt "));
                        destination.Write(fmtLength);
                        destination.Write(format);
                        destination.Write(channels);
                        destination.Write(samplesPerSec);
                        destination.Write(avgBytesPerSec);
                        destination.Write(blockAlign);
                        destination.Write(bitsPerSample);

                        if (format == WAVE_FORMAT_DVI_ADPCM)
                        {
                            // See https://icculus.org/SDL_sound/downloads/external_documentation/wavecomp.htm
                            var samplesPerBlock = objectReader.ReadUInt16();
                            destination.Write(extraSize);
                            destination.Write(samplesPerBlock);
                            destination.Write(Encoding.ASCII.GetBytes("fact"));
                            destination.Write(4);
                            destination.Write(samplesPerBlock * dataLength / blockAlign);
                        }

                        destination.Write(Encoding.ASCII.GetBytes("data"));
                        destination.Write(dataLength);
                        destination.Write(objectReader.ReadBytes(dataLength));
                    }

                    m_Type = AudioType.WAV;
                    m_Size = audioData.Length;
                    m_AudioData = new ResourceReader(new BinaryReader(audioData), 0, (int)audioData.Length);
                    return true;

                case WaveField.UnknownByte:
                    fieldValues.Add(field, objectReader.ReadByte());
                    return true;

                default:
                    fieldValues.Add(field, objectReader.ReadInt32());
                    return true;
            }
        }
    }
}