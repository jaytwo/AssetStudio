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

                        var animationData = MxrAnimation.Read(objectReader, fieldValues[TrackField.Frames], out value);
                        value += $" ({animationData.Length})";
                    }
                    break;

                case TrackField.Start:
                    if (objectReader.ReadByte() != 1)
                        throw new InvalidDataException();
                    return true;

                case TrackField.UnknownArray35:
                    var ints = ReadArray(objectReader, fieldValues, field, s => objectReader.ReadInt32());
                    if (!ints.EndsWith(" "))
                        value = ints.Length.ToString();
                    break;

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
                case TrackField.UnknownArray37:
                case TrackField.UnknownArray38:
                    var section = field;
                    var counts = new List<int>();
                    var txt = string.Empty;

                    while (true)
                    {
                        counts.Add(0);
                        txt += ReadArray(objectReader, fieldValues, section, s =>
                        {
                            counts[counts.Count - 1]++;
                            return (section == TrackField.UnknownArray34 || section == TrackField.UnknownArray35) ?
                                objectReader.ReadInt32() : objectReader.ReadInt64();
                        });

                        txt += Environment.NewLine;
                        section = (TrackField)objectReader.ReadByte();

                        if (section != TrackField.UnknownArray33 &&
                            section != TrackField.UnknownArray34 &&
                            section != TrackField.UnknownArray35 &&
                            section != TrackField.UnknownArray37 &&
                            section != TrackField.UnknownArray38 &&
                            section != TrackField.UnknownArray42)
                        {
                            objectReader.BaseStream.Position--;
                            break;
                        }
                    }

                    value = string.Join(" + ", counts) + $" ({txt.Length})";
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

        private string ReadArray(BinaryReader objectReader, Dictionary<TrackField, int> fieldValues, TrackField field, Func<BinaryReader, object> read)
        {
            var header = objectReader.ReadByte();
            if (header != 0 && header != 255)
                throw new InvalidDataException();

            fieldValues[field] = objectReader.ReadInt32();
            if (header == 255)
                fieldValues[field] = 0;

            var items = Enumerable.Range(0, fieldValues[field])
                .Select(i => read(objectReader))
                .ToArray();

            return $"Section {(int)field}" + Environment.NewLine +
                "    Objects[" + items.Length + "] = " + string.Join(", ", items);
        }
    }
}