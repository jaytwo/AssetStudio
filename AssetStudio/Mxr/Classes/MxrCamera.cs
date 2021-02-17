using System.Collections.Generic;

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

    class MxrCamera : NamedObject
    {
        public MxrCamera(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<CameraField>(this, ClassIDType.Camera, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<CameraField, int> fieldValues, CameraField field)
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