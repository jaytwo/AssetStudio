using AssetStudio.Mxr.Fields;
using System.Collections.Generic;
using System.IO;

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

        private bool ReadField(ObjectReader objectReader, Dictionary<ScoreField, int> fieldValues, ScoreField field)
        {
            switch (field)
            {
                case ScoreField.Index:
                    InfoText += $"{field}: {objectReader.ReadInt32()}\n";
                    return true;

                case ScoreField.Name:
                    m_Name = MxrObjectReader.ReadString(objectReader);
                    objectReader.Position--;
                    return false;

                default:
                    throw new InvalidDataException();
            }
        }
    }
}