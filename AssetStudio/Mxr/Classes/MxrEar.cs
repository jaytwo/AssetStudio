namespace AssetStudio.Mxr.Classes
{
    enum EarField
    {
        Distance = 16,
        Rolloff = 17,
        Doppler = 18
    }

    class MxrEar : MxrNamedObject<EarField>
    {
        public MxrEar(ObjectReader objectReader)
            : base(objectReader, ClassIDType.AudioListener) { }

        protected override void Read(ObjectReader objectReader, EarField field)
        {
            var value = objectReader.ReadSingle();
        }
    }
}
