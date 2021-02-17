using System;
using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    enum LightField
    {
        LightType = 16,
        Colour = 17,
        Channels = 21,
        CutOffAngle = 22,
        Distance = 23,
        Tag = 24,
        CutOffAnglePhi = 25,
        ChannelList = 26,
        DropOffRate = 27,
    }

    enum LightType
    {
        Parallel = 0,
        Point = 1,
        Spot = 2,
        Ambient = 3,
    }

    class MxrLight : MxrNamedObject
    {
        public MxrLight(ObjectReader objectReader)
            : base(objectReader, ClassIDType.Light) { }

        protected override void Read(ObjectReader objectReader) =>
            Read<LightField>(objectReader, ReadField);

        private void ReadField(ObjectReader objectReader, Dictionary<LightField, int> fieldValues, LightField field)
        {
            object value;

            switch (field)
            {
                case LightField.LightType:
                    value = (LightType)objectReader.ReadByte();
                    break;

                case LightField.CutOffAngle:
                case LightField.CutOffAnglePhi:
                    value = Math.Round(objectReader.ReadSingle() * 180.0 / Math.PI);
                    break;

                case LightField.Colour:
                    value = string.Join(", ", objectReader.ReadByte(), objectReader.ReadByte(), objectReader.ReadByte());
                    break;

                default:
                    value = objectReader.ReadSingle();
                    break;
            }
        }
    }
}