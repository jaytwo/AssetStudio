﻿using System;
using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    enum MidiField
    {
        MidiData = 32
    }

    class MxrMidi : MxrNamedObject
    {
        public MxrMidi(ObjectReader objectReader)
            : base(objectReader, ClassIDType.AudioImporter) { }

        protected override void Read(ObjectReader objectReader) =>
            Read<MidiField>(objectReader, ReadField);

        private void ReadField(ObjectReader objectReader, Dictionary<MidiField, int> fieldValues, MidiField field)
        {
            switch (field)
            {
                default:
                    fieldValues[field] = objectReader.ReadInt32();
                    break;

                case MidiField.MidiData:
                    ushort chunks = 2;
                    for (int i = 0; i <= chunks; i++)
                    {
                        var header = objectReader.ReadBytes(8);

                        Array.Reverse(header);
                        var dataLength = (int)BitConverter.ToUInt32(header, 0);
                        var data = objectReader.ReadBytes(dataLength);

                        if (i == 0)
                        {
                            Array.Reverse(data);
                            chunks = BitConverter.ToUInt16(data, 2);
                        }
                    }
                    break;
            }
        }
    }
}