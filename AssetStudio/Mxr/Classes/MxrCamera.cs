namespace AssetStudio.Mxr.Classes
{
    enum CameraField
    {
        Height = 16,
        ZoomFactor = 17,
        ClipFore = 18,
        ClipBack = 19,
        FogEnabled = 32,
        FogColour = 33,
        FogFore = 34,
        FogBack = 35,
        Tag = 36,
        Unknown37 = 37,
        Angle = 38
    }

    class MxrCamera : MxrNamedObject<CameraField>
    {
        public MxrCamera(ObjectReader objectReader)
            : base(objectReader, ClassIDType.Camera) { }

        protected override void Read(ObjectReader objectReader, CameraField field)
        {
            object value;
            
            switch (field)
            {
                case CameraField.FogColour:
                    value = string.Join(", ", objectReader.ReadByte(), objectReader.ReadByte(), objectReader.ReadByte());
                    objectReader.ReadByte();
                    break;

                case CameraField.FogEnabled:
                case CameraField.Unknown37:
                    value = objectReader.ReadInt32();
                    break;

                default:
                    value = objectReader.ReadSingle();
                    break;
            }
        }
    }
}