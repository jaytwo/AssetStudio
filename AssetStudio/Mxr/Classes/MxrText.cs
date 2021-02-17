using System;

namespace AssetStudio.Mxr.Classes
{
    enum TextField
    {
        Colour = 16,
        BackColour = 17,
        Height = 18,
        Size = 19,
        Font = 21,
        Byte22 = 22,
        Strings = 32
    }

    class MxrText : MxrNamedObject<TextField>
    {
        public MxrText(ObjectReader objectReader)
            : base(objectReader, ClassIDType.TextAsset) { }

        protected override void Read(ObjectReader objectReader, TextField field)
        {
            string value;

            switch (field)
            {
                case TextField.Colour:
                case TextField.BackColour:
                    value = string.Join(", ", objectReader.ReadByte(), objectReader.ReadByte(), objectReader.ReadByte());
                    objectReader.ReadByte();
                    break;

                case TextField.Font:
                    value = ReadString(objectReader);
                    break;

                case TextField.Byte22:
                    value = objectReader.ReadByte().ToString();
                    break;

                case TextField.Strings:
                    var count = objectReader.ReadInt32();
                    value = string.Empty;
                    for (int i = 0; i < count; i++)
                        value += Environment.NewLine + ReadString(objectReader);
                    break;

                default:
                    value = objectReader.ReadInt32().ToString();
                    break;
            }
        }
    }
}