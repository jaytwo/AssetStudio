using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    class MxrScore : TextAsset
    {
        private string _infoText;

        public MxrScore(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<ScoreField>(this, ClassIDType.MonoScript, ReadField, 0, withHeader: false);
        }

        public bool ReadField(ObjectReader objectReader, Dictionary<ScoreField, int> fieldValues, ScoreField field)
        {
            switch (field)
            {
                case ScoreField.End:
                    m_Script = Encoding.UTF8.GetBytes(_infoText);
                    return false;

                case ScoreField.Index:
                    _infoText += $"{field}: {objectReader.ReadInt32()}\n";
                    return true;

                case ScoreField.Name:
                    m_Name = MxrObjectReader.ReadString(objectReader);
                    objectReader.Position--;
                    return false;

                case ScoreField.UnknownArray33:
                    if (objectReader.ReadByte() != 0)
                        throw new InvalidDataException();

                    var bytes = Enumerable.Range(0, objectReader.ReadInt32()).Select(i => objectReader.ReadByte()).ToList();
                    _infoText += $"{field}: {bytes.Count} {{\n    {string.Join(",", bytes)}\n}}\n";
                    return true;

                case ScoreField.UnknownArray34:
                    var strings = new List<object>();
                    for (int i = 0; objectReader.ReadUInt32() != UInt32.MaxValue; i++)
                    {
                        objectReader.BaseStream.Position -= 4;
                        strings.Add($"    {objectReader.ReadInt32()}, {MxrObjectReader.ReadString(objectReader)}, {objectReader.ReadInt32()}");
                    }
                    _infoText += $"{field}: {strings.Count} {{\n{string.Join(Environment.NewLine, strings)}\n}}\n";
                    return true;

                default:
                    throw new InvalidDataException();
            }
        }
    }
}