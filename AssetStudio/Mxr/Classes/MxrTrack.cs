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
        private int _depth;
        private string _infoText;

        public MxrTrack(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<TrackField>(this, ClassIDType.MonoScript, ReadField, 0, endCondition: EndCondition, headerLevel: 1);
        }

        private bool EndCondition(byte fieldByte)
        {
            if (fieldByte == 255 && --_depth < 0)
            {
                m_Script = Encoding.UTF8.GetBytes(_infoText);
                return true;
            }

            return false;
        }

        private void ReadField(ObjectReader objectReader, Dictionary<TrackField, int> fieldValues, TrackField field)
        {
            string value = null;

            switch (field)
            {
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
                    if (!fieldValues.Any())
                    {
                        // Start of track
                        if (objectReader.ReadByte() != 16)
                            throw new InvalidDataException();

                        fieldValues[field] = objectReader.ReadInt32();
                        _depth++;
                        break;
                    }
                    if (objectReader.ReadByte() != 1)
                        throw new InvalidDataException();
                    return;

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
                    if (_depth > 0)
                        goto case TrackField.UnknownArray37;

                    value = ReadArray(objectReader, fieldValues, field, s => objectReader.ReadByte()) + Environment.NewLine;
                    break;

                case TrackField.UnknownArray34:
                    if (_depth > 0)
                        goto case TrackField.UnknownArray37;

                    var strings = new List<object>();
                    for (int i = 0; objectReader.ReadUInt32() != UInt32.MaxValue; i++)
                    {
                        objectReader.BaseStream.Position -= 4;
                        strings.Add($"    {objectReader.ReadInt32()}, {MxrObjectReader.ReadString(objectReader)}, {objectReader.ReadInt32()}");
                    }
                    value = strings.Count + " {\r\n" + string.Join(Environment.NewLine, strings) + Environment.NewLine + "}";
                    break;

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

                case TrackField.End:
                    fieldValues.Clear();
                    break;

                default:
                    fieldValues[field] = objectReader.ReadInt32();
                    break;
            }

            if (field == TrackField.Start && fieldValues.Count == 1)
                _infoText += $"-- TRACK ({fieldValues[field]}) --\n";
            else if (field == TrackField.End)
                _infoText += "\n";
            else
                _infoText += $"{field} = {value ?? fieldValues[field].ToString()}\n";
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

        protected bool OnFinish(BinaryReader source, BinaryWriter destination)
        {
            return --_depth < 0;
        }
    }
}