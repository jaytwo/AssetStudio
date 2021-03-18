using AssetStudio.Mxr.Fields;
using System.Collections.Generic;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    class MxrText : TextAsset
    {
        private string _infoText;

        public MxrText(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<TextField>(this, ClassIDType.TextAsset, ReadField);
            m_Script = Encoding.UTF8.GetBytes(_infoText);
        }

        private bool ReadField(ObjectReader objectReader, Dictionary<TextField, int> fieldValues, TextField field)
        {
            switch (field)
            {
                case TextField.End:
                    return false;

                case TextField.Colour:
                case TextField.BackColour:
                    _infoText += $"{field}: {string.Join(", ", objectReader.ReadByte(), objectReader.ReadByte(), objectReader.ReadByte())}\n";
                    objectReader.ReadByte();
                    return true;

                case TextField.Font:
                    _infoText += $"{field}: {MxrObjectReader.ReadString(objectReader)}\n";
                    return true;

                case TextField.Byte22:
                    _infoText += $"{field}: {objectReader.ReadByte()}\n";
                    return true;

                case TextField.String:
                    var count = objectReader.ReadInt32();
                    for (int i = 0; i < count; i++)
                        _infoText += $"{field}[{i}]: {MxrObjectReader.ReadString(objectReader)}\n";
                    return true;

                default:
                    _infoText += $"{field}: {objectReader.ReadInt32()}\n";
                    return true;
            }
        }
    }
}