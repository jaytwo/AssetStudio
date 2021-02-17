using System;
using System.Collections;
using System.IO;
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

    class MxrTexture : MxrNamedObject<TextureField>
    {
        private byte[] _colours = new byte[0];
        private byte[] _transparent = new byte[0];
        private BitArray _alphaMap;
        private MemoryStream _pixels;

        public MxrTexture(ObjectReader objectReader)
            : base(objectReader, ClassIDType.Texture2D) { }

        protected override void Read(ObjectReader objectReader, TextureField field)
        {
            switch (field)
            {
                case TextureField.JpegData:
                    _pixels = new MemoryStream(objectReader.ReadBytes(objectReader.ReadInt32()));
                    break;

                case TextureField.BmpColourTable:
                    _colours = objectReader.ReadBytes(_fieldValues[TextureField.Colours] * 4);
                    break;

                case TextureField.Tranparent:
                    _transparent = objectReader.ReadBytes(4);
                    break;

                case TextureField.BmpPixelData:
                    _pixels = new MemoryStream();
                    using (var memoryWriter = new BinaryWriter(_pixels, Encoding.Default, true))
                    {
                        // BMP header
                        var rowBits = _fieldValues[TextureField.Width] * _fieldValues[TextureField.BitsPerPixel];
                        if (rowBits % 32 != 0)
                            rowBits = 32 + (32 * (rowBits / 32));

                        var pixelsLength = rowBits * _fieldValues[TextureField.Height] / 8;
                        memoryWriter.Write((byte)0x42);
                        memoryWriter.Write((byte)0x4D);
                        memoryWriter.Write(14 + 40 + _colours.Length + pixelsLength);
                        memoryWriter.Write(0);
                        memoryWriter.Write(14 + 40 + _colours.Length);

                        // DIB header
                        memoryWriter.Write(40);
                        memoryWriter.Write(_fieldValues[TextureField.Width]);
                        memoryWriter.Write(_fieldValues[TextureField.Height]);
                        memoryWriter.Write((short)1);
                        memoryWriter.Write((short)_fieldValues[TextureField.BitsPerPixel]);
                        memoryWriter.Write(new byte[16]);
                        memoryWriter.Write(_fieldValues[TextureField.Colours]);
                        memoryWriter.Write(0);

                        // Pixels
                        memoryWriter.Write(_colours);
                        memoryWriter.Write(objectReader.ReadBytes(pixelsLength));
                    }

                    _pixels.Position = 0;
                    break;

                case TextureField.BitsPerPixel:
                case TextureField.UnknownShort31:
                    _fieldValues.Add(field, objectReader.ReadUInt16());
                    break;

                case TextureField.UnknownByte22:
                case TextureField.UnknownByte27:
                case TextureField.UnknownByte128:
                case TextureField.UnknownByte129:
                    _fieldValues.Add(field, objectReader.ReadByte());
                    break;

                case TextureField.UnknownArray99:
                    _fieldValues.Add(field, objectReader.ReadInt32());
                    objectReader.ReadBytes(12);
                    break;

                case TextureField.AlphaMap:
                    _fieldValues.Add(field, objectReader.ReadInt32());
                    _alphaMap = new BitArray(objectReader.ReadBytes(_fieldValues[field]));
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
                    _fieldValues.Add(field, objectReader.ReadInt32());
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}
