using System;
using System.Collections.Generic;

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

    class MxrText : NamedObject
    {
        public MxrText(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<TextField>(this, ClassIDType.TextAsset, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<TextField, int> fieldValues, TextField field)
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
                    value = MxrObjectReader.ReadString(objectReader);
                    break;

                case TextField.Byte22:
                    value = objectReader.ReadByte().ToString();
                    break;

                case TextField.Strings:
                    var count = objectReader.ReadInt32();
                    value = string.Empty;
                    for (int i = 0; i < count; i++)
                        value += Environment.NewLine + MxrObjectReader.ReadString(objectReader);
                    break;

                default:
                    value = objectReader.ReadInt32().ToString();
                    break;
            }
        }
    }
}