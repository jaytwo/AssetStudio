using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetStudio.Mxr.Classes
{
    class MxrMovie : NamedObject, IMxrPropertyInfo
    {
        public string InfoText { get; private set; }

        public MxrMovie(ObjectReader objectReader)
            : base(objectReader)
        {
            MxrObjectReader.Read<MovieField>(this, ClassIDType.MovieTexture, ReadField, 0);
        }

        private bool ReadField(ObjectReader objectReader, Dictionary<MovieField, int> fieldValues, MovieField field)
        {
            switch (field)
            {
                case MovieField.End:
                    return false;

                case MovieField.UnknownByte19:
                case MovieField.UnknownByte20:
                case MovieField.UnknownByte21:
                case MovieField.UnknownByte22:
                case MovieField.UnknownByte23:
                case MovieField.UnknownByte24:
                    InfoText += $"{field}: {objectReader.ReadByte()}\n";
                    return true;

                case MovieField.UnknownInt16:
                case MovieField.UnknownInt17:
                case MovieField.UnknownInt18:
                case MovieField.Width:
                case MovieField.Height:
                case MovieField.UnknownInt52:
                    InfoText += $"{field}: {objectReader.ReadInt32()}\n";
                    return true;

                case MovieField.UnknownArray32:
                    InfoText += $"{field}: [{string.Join(", ", Enumerable.Range(0, objectReader.ReadInt32()).Select(i => objectReader.ReadByte()))}]\n";
                    return true;

                default:
                    throw new NotImplementedException();
            }
        }
    }
}