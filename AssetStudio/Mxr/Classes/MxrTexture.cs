using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    enum TextureField
    {
        Width = 16,
        Height = 17,
        BitsPerPixel = 18,
        Colours = 19,
        Tranparent = 20,
        UnknownInt21 = 21,
        UnknownByte22 = 22,
        UnknownInt26 = 26,
        UnknownByte27 = 27,
        UnknownShort31 = 31,
        BmpColourTable = 32,
        BmpPixelData = 48,
        JpegData = 64,
        AlphaMap = 80,
        UnknownInt96 = 96,
        UnknownInt97 = 97,
        UnknownInt98 = 98,
        UnknownArray99 = 99,
        UnknownInt100 = 100,
        UnknownInt101 = 101,
        UnknownByte128 = 128,
        UnknownByte129 = 129,
    }

    class MxrTexture : Texture2D
    {
        private uint[] _colours = new uint[0];
        private uint _transparent;
        private BitArray _alphaMap;

        public MxrTexture(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<TextureField>(this, ClassIDType.Texture2D, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<TextureField, int> fieldValues, TextureField field)
        {
            switch (field)
            {
                case TextureField.JpegData:
                    using (var jpeg = new Bitmap(new MemoryStream(objectReader.ReadBytes(objectReader.ReadInt32()))))
                    {
                        m_Width = jpeg.Width;
                        m_Height = jpeg.Height;
                        m_TextureFormat = TextureFormat.RGB24;

                        var data = jpeg.LockBits(new Rectangle(0, 0, m_Width, m_Height), ImageLockMode.ReadOnly, jpeg.PixelFormat);
                        var strides = new byte[data.Stride * jpeg.Height];
                        Marshal.Copy(data.Scan0, strides, 0, strides.Length);
                        jpeg.UnlockBits(data);

                        var rgb = new byte[m_Width * m_Height * 3];
                        var index = 0;
                        for (int row = 0; row < m_Height; row++)
                            for (int column = 0; column < m_Width; column++)
                            {
                                var offset = ((m_Height - row - 1) * data.Stride) + (column * 3);
                                rgb[index++] = strides[offset + 2];
                                rgb[index++] = strides[offset + 1];
                                rgb[index++] = strides[offset + 0];
                            }

                        image_data = new ResourceReader(new BinaryReader(new MemoryStream(rgb)), 0, rgb.Length);
                    }
                    break;

                case TextureField.BmpColourTable:
                    _colours = objectReader.ReadUInt32Array(fieldValues[TextureField.Colours]);
                    break;

                case TextureField.Tranparent:
                    _transparent = objectReader.ReadUInt32() | 0xff000000;
                    break;

                case TextureField.BmpPixelData:
                    using (var memoryWriter = new BinaryWriter(new MemoryStream(), Encoding.Default, true))
                    {
                        m_Width = fieldValues[TextureField.Width];
                        m_Height = fieldValues[TextureField.Height];
                        m_TextureFormat = TextureFormat.BGRA32;

                        var bitsPerPixel = fieldValues[TextureField.BitsPerPixel];
                        var rowBytes = (int)Math.Ceiling((m_Width * bitsPerPixel) / 8.0) % 4;
                        
                        for (int row = 0; row < m_Height; row++)
                        {
                            for (int column = 0; column < m_Width; column++)
                            {
                                switch (bitsPerPixel)
                                {
                                    case 4:
                                        var b = objectReader.ReadByte();
                                        WriteColour(memoryWriter, _colours[b >> 4]);
                                        if (++column < m_Width)
                                            WriteColour(memoryWriter, _colours[b & 0x0f]);
                                        break;

                                    case 8:
                                        WriteColour(memoryWriter, _colours[objectReader.ReadByte()]);
                                        break;

                                    case 24:
                                        WriteColour(memoryWriter, objectReader.ReadUInt32());
                                        objectReader.Position--;
                                        break;

                                    default:
                                        throw new NotImplementedException();
                                }
                            }

                            // Align rows to multiples of 32 bits
                            if (rowBytes != 0)
                                objectReader.ReadBytes(4 - rowBytes);
                        }

                        image_data = new ResourceReader(new BinaryReader(memoryWriter.BaseStream), 0, (int)memoryWriter.BaseStream.Length);
                    }
                    break;

                case TextureField.BitsPerPixel:
                case TextureField.UnknownShort31:
                    fieldValues.Add(field, objectReader.ReadUInt16());
                    break;

                case TextureField.UnknownByte22:
                case TextureField.UnknownByte27:
                case TextureField.UnknownByte128:
                case TextureField.UnknownByte129:
                    fieldValues.Add(field, objectReader.ReadByte());
                    break;

                case TextureField.UnknownArray99:
                    fieldValues.Add(field, objectReader.ReadInt32());
                    objectReader.ReadBytes(12);
                    break;

                case TextureField.AlphaMap:
                    fieldValues.Add(field, objectReader.ReadInt32());
                    _alphaMap = new BitArray(objectReader.ReadBytes(fieldValues[field]));
                    break;

                case TextureField.Width:
                case TextureField.Height:
                case TextureField.Colours:
                case TextureField.UnknownInt21:
                case TextureField.UnknownInt26:
                case TextureField.UnknownInt96:
                case TextureField.UnknownInt97:
                case TextureField.UnknownInt98:
                case TextureField.UnknownInt100:
                case TextureField.UnknownInt101:
                    fieldValues.Add(field, objectReader.ReadInt32());
                    break;

                default:
                    throw new NotImplementedException();
            }
        }

        private void WriteColour(BinaryWriter writer, uint bgra)
        {
            bgra |= 0xff000000;
            writer.Write(bgra == _transparent ? 0 : bgra);
        }
    }
}