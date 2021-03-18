using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    class MxrTable : TextAsset
    {
        public string InfoText { get; set; }
        public Dictionary<int, string> Strings { get; } = new Dictionary<int, string>();

        public MxrTable(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<TableField>(this, ClassIDType.MonoScript, ReadField, withHeader: false);
            m_Name = "Table";
            m_Script = Encoding.UTF8.GetBytes(InfoText);
        }

        private bool ReadField(ObjectReader objectReader, Dictionary<TableField, int> fieldValues, TableField field)
        {
            var address = 0;
            var value = field.ToString() + " = ";

            switch (field)
            {
                case TableField.End:
                    return false;

                case TableField.Unknown128:
                    var header = objectReader.ReadBytes(2);
                    var dataLength = 0;

                    switch (header[0])
                    {
                        case 3:
                            if (header[1] == 32)
                            {
                                header = header.Concat(objectReader.ReadBytes(12)).ToArray();
                                dataLength = header[6] * 4;
                            }
                            else dataLength = 4;
                            break;
                        case 5:
                            dataLength = header[1] == 32 ? 24 : 8;
                            break;
                        case 8:
                            dataLength = 16;
                            break;
                        case 11:
                            if (header[1] == 32)
                            {
                                header = header.Concat(objectReader.ReadBytes(9)).ToArray();
                                dataLength = header[6];
                            }
                            else dataLength = 1;
                            break;
                    }

                    value += string.Join(", ", header.Concat(objectReader.ReadBytes(dataLength)));
                    break;

                case TableField.Unknown129:
                    value += string.Join(", ", objectReader.ReadBytes(10));
                    break;

                case TableField.Unknown130:
                    address = objectReader.ReadInt32();
                    value += $"{address.ToString("X8")}, {objectReader.ReadInt32()}, {objectReader.ReadInt32()}, {objectReader.ReadInt32()}";
                    break;

                case TableField.Unknown131:
                case TableField.Unknown132:
                    address = objectReader.ReadInt32();
                    value += $"{address.ToString("X8")}, {objectReader.ReadInt32()}, {objectReader.ReadInt32()}";
                    break;

                default:
                    throw new NotImplementedException();
            }

            var entry = MxrObjectReader.ReadString(objectReader);
            if (address != 0)
                Strings.Add(address, entry);

            InfoText += $"{value}, {entry}\n";

            if (objectReader.ReadUInt16() != 0)
                objectReader.BaseStream.Position--;

            return true;
        }
    }
}