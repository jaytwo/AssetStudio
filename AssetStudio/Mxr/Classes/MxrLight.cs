﻿using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    class MxrLight : NamedObject, IMxrPropertyInfo
    {
        public string InfoText { get; private set; }

        public MxrLight(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<LightField>(this, ClassIDType.Light, ReadField);
        }

        private void ReadField(ObjectReader objectReader, Dictionary<LightField, int> fieldValues, LightField field)
        {
            switch (field)
            {
                case LightField.LightType:
                    InfoText += $"{field}: {(LightType)objectReader.ReadByte()}\n";
                    break;

                case LightField.CutOffAngle:
                case LightField.CutOffAnglePhi:
                    InfoText += $"{field}: {Math.Round(objectReader.ReadSingle() * 180.0 / Math.PI)}\n";
                    break;

                case LightField.Colour:
                    InfoText += $"{field}: {string.Join(", ", objectReader.ReadByte(), objectReader.ReadByte(), objectReader.ReadByte())}\n";
                    break;

                default:
                    InfoText += $"{field}: {objectReader.ReadSingle()}\n";
                    break;
            }
        }
    }
}