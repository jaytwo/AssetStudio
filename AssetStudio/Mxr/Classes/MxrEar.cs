using AssetStudio.Mxr.Fields;
using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    class MxrEar : NamedObject, IMxrPropertyInfo
    {
        public string InfoText { get; private set; }

        public MxrEar(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<EarField>(this, ClassIDType.AudioListener, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<EarField, int> fieldValues, EarField field)
        {
            InfoText += $"{field}: {objectReader.ReadSingle()}\n";
        }
    }
}