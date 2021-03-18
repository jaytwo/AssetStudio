using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AssetStudio.Mxr.Classes
{
    class MxrScore : NamedObject, IMxrPropertyInfo
    {
        public string InfoText { get; private set; }

        public MxrScore(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<ScoreField>(this, ClassIDType.MonoScript, ReadField, 0, withHeader: false);
        }

        public bool ReadField(ObjectReader objectReader, Dictionary<ScoreField, int> fieldValues, ScoreField field)
        {
            switch (field)
            {
                case ScoreField.End:
                    return false;

                case ScoreField.Index:
                    InfoText += $"{field}: {objectReader.ReadInt32()}\n";
                    return true;

                case ScoreField.Name:
                    m_Name = MxrObjectReader.ReadString(objectReader);
                    objectReader.Position--;
                    return false;

                case ScoreField.UnknownArray33:
                    InfoText += $"{field}: {ReadArray(objectReader, fieldValues, field, s => objectReader.ReadByte())}\n";
                    return true;

                case ScoreField.UnknownArray34:
                    var strings = new List<object>();
                    for (int i = 0; objectReader.ReadUInt32() != UInt32.MaxValue; i++)
                    {
                        objectReader.BaseStream.Position -= 4;
                        strings.Add($"    {objectReader.ReadInt32()}, {MxrObjectReader.ReadString(objectReader)}, {objectReader.ReadInt32()}");
                    }
                    InfoText += $"{field}: {strings.Count} {{\n{string.Join(Environment.NewLine, strings)}\n}}\n";
                    return true;

                default:
                    throw new InvalidDataException();
            }
        }

        private string ReadArray(BinaryReader objectReader, Dictionary<ScoreField, int> fieldValues, ScoreField field, Func<BinaryReader, object> read)
        {
            var header = objectReader.ReadByte();
            if (header != 0 && header != 255)
                throw new InvalidDataException();

            fieldValues[field] = objectReader.ReadInt32();
            if (header == 255)
                fieldValues[field] = 0;

            var items = Enumerable.Range(0, fieldValues[field])
                .Select(i => read(objectReader))
                .ToArray();

            return $"Section {(int)field}" + Environment.NewLine +
                "    Objects[" + items.Length + "] = " + string.Join(", ", items);
        }
    }
}