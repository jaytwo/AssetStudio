using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    enum EarField
    {
        Distance = 16,
        Rolloff = 17,
        Doppler = 18
    }

    class MxrEar : NamedObject
    {
        public MxrEar(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<EarField>(this, ClassIDType.AudioListener, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<EarField, int> fieldValues, EarField field)
        {
            var value = objectReader.ReadSingle();
        }
    }
}