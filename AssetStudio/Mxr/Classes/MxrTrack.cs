using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    class MxrTrack : TextAsset
    {
        private string _infoText;

        public MxrTrack(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<TrackField>(this, ClassIDType.MonoScript, ReadField, 0, withHeader: false);
        }

        private bool ReadField(ObjectReader objectReader, Dictionary<TrackField, int> fieldValues, TrackField field)
        {
            string value = null;

            switch (field)
            {
                case TrackField.End:
                    m_Script = Encoding.UTF8.GetBytes(_infoText);
                    objectReader.Position--;
                    return false;

                case TrackField.Frames:
                    fieldValues[field] = objectReader.ReadInt32();
                    if (fieldValues[field] == 0)
                    {
                        if (objectReader.ReadByte() != 255)
                            objectReader.BaseStream.Position--;
                    }
                    else if (!fieldValues.ContainsKey(TrackField.UnknownByte41))
                    {
                        if (objectReader.ReadByte() != 32)
                            throw new InvalidDataException();

                        value = MxrAnimation.Read(objectReader, fieldValues[TrackField.Frames]);
                    }
                    break;

                case TrackField.Start:
                    if (objectReader.ReadByte() != 1)
                        throw new InvalidDataException();
                    return true;

                case TrackField.UnknownByte41:
                    fieldValues[field] = objectReader.ReadByte();
                    break;

                case TrackField.UnknownByte48:
                case TrackField.UnknownByte49:
                    string unknown48 = string.Empty;
                    if (fieldValues.ContainsKey(TrackField.UnknownByte41))
                    {
                        fieldValues.TryGetValue(TrackField.Type, out var resourceType);
                        while (true)
                        {
                            var objectType = (int)objectReader.ReadByte();
                            if (objectType == 255)
                                break;

                            if (objectType == 128)
                                objectType = objectReader.ReadInt32();

                            var objectCount = objectReader.ReadByte();
                            unknown48 += $"Unknown{objectType}[{objectCount}] = " + string.Join(", ",
                                Enumerable.Range(0, resourceType * objectCount).Select(e => objectReader.ReadSingle())) + Environment.NewLine;
                        }

                        value = unknown48.Length.ToString();
                        fieldValues[field] = unknown48.Length;
                    }
                    else fieldValues[field] = objectReader.ReadByte();

                    break;

                case TrackField.Name:
                    value = MxrObjectReader.ReadString(objectReader);
                    break;

                case TrackField.UnknownArray33:
                case TrackField.UnknownArray34:
                case TrackField.UnknownArray35:
                case TrackField.UnknownArray37:
                case TrackField.UnknownArray38:
                case TrackField.UnknownArray42:
                    if (objectReader.ReadByte() != 0)
                        throw new InvalidDataException();
                    var ints = Enumerable.Range(0, objectReader.ReadInt32())
                        .Select(i => (field == TrackField.UnknownArray34 || field == TrackField.UnknownArray35) ?
                            objectReader.ReadInt32() : objectReader.ReadInt64())
                        .ToList();
                    value = ints.Any() ? $"{ints.Count} {{\n    {string.Join(", ", ints)}\n}}" : "{ }";
                    break;

                case TrackField.Type:
                    fieldValues[field] = objectReader.ReadInt32();
                    value = ((MxrClassIDType)fieldValues[field]).ToString();
                    if (fieldValues.ContainsKey(TrackField.UnknownByte41) && objectReader.ReadByte() != 255)
                        objectReader.BaseStream.Position--;
                    break;

                default:
                    fieldValues[field] = objectReader.ReadInt32();
                    break;
            }

            _infoText += $"{field} = {value ?? fieldValues[field].ToString()}\n";
            return true;
        }
    }
}