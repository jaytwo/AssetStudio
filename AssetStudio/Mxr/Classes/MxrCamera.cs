using AssetStudio.Mxr.Fields;
using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    class MxrCamera : NamedObject, IMxrPropertyInfo
    {
        public string InfoText { get; private set; }

        public MxrCamera(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<CameraField>(this, ClassIDType.Camera, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<CameraField, int> fieldValues, CameraField field)
        {
            switch (field)
            {
                case CameraField.FogColour:
                    InfoText += $"{field}: {string.Join(", ", objectReader.ReadByte(), objectReader.ReadByte(), objectReader.ReadByte())}\n";
                    objectReader.ReadByte();
                    break;

                case CameraField.FogEnabled:
                case CameraField.Unknown37:
                    InfoText += $"{field}: {objectReader.ReadInt32()}\n";
                    break;

                default:
                    InfoText += $"{field}: {objectReader.ReadSingle()}\n";
                    break;
            }
        }
    }
}