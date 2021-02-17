using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    enum EarField
    {
        Distance = 16,
        Rolloff = 17,
        Doppler = 18
    }

    class MxrEar : MxrNamedObject
    {
        public MxrEar(ObjectReader objectReader)
            : base(objectReader, ClassIDType.AudioListener) { }

        protected override void Read(ObjectReader objectReader) =>
            Read<EarField>(objectReader, ReadField);

        private void ReadField(ObjectReader objectReader, Dictionary<EarField, int> fieldValues, EarField field)
        {
            var value = objectReader.ReadSingle();
        }
    }
}