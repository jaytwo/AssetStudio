﻿using System;

namespace AssetStudio.Mxr.Classes
{
    enum MovieField
    {
        UnknownInt16 = 16,
        UnknownInt17 = 17,
        UnknownInt18 = 18,
        UnknownByte19 = 19,
        UnknownByte20 = 20,
        UnknownByte21 = 21,
        UnknownByte22 = 22,
        UnknownByte23 = 23,
        UnknownByte24 = 24,
        UnknownArray32 = 32,
        Width = 48,
        Height = 49,
        UnknownInt52 = 52
    }

    class MxrMovie : MxrNamedObject<MovieField>
    {
        public MxrMovie(ObjectReader objectReader)
            : base(objectReader, ClassIDType.MovieTexture) { }

        protected override void OnStart(ref ObjectReader objectReader) { }

        protected override void Read(ObjectReader objectReader, MovieField field)
        {
            switch (field)
            {
                case MovieField.UnknownByte19:
                case MovieField.UnknownByte20:
                case MovieField.UnknownByte21:
                case MovieField.UnknownByte22:
                case MovieField.UnknownByte23:
                case MovieField.UnknownByte24:
                    _fieldValues.Add(field, objectReader.ReadByte());
                    break;

                case MovieField.UnknownInt16:
                case MovieField.UnknownInt17:
                case MovieField.UnknownInt18:
                case MovieField.Width:
                case MovieField.Height:
                case MovieField.UnknownInt52:
                    _fieldValues.Add(field, objectReader.ReadInt32());
                    break;

                case MovieField.UnknownArray32:
                    _fieldValues.Add(field, objectReader.ReadInt32());
                    objectReader.ReadBytes(_fieldValues[field]);
                    break;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}