using AssetStudio.Mxr.Fields;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AssetStudio.Mxr.Classes
{
    class Group : Dictionary<ModelField, object>
    {
        public List<Group> TextureGroup { get; set; }
    }

    class MxrModel : Mesh, IMxrPropertyInfo
    {
        private short[] _faceGroups;
        private readonly List<Group> _groups = new List<Group>();

        private Group _group;

        public string InfoText { get; private set; }

        public MxrModel(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read()
        {
            MxrObjectReader.Read<ModelField>(this, ClassIDType.Mesh, ReadField);
        }

        private bool ReadField(ObjectReader objectReader, Dictionary<ModelField, int> fieldValues, ModelField field)
        {
            switch (field)
            {
                case ModelField.End:
                    return false;

                case ModelField.UnknownMarker19:
                    return true;

                case ModelField.UnknownShort150:
                    _group.Add(field, objectReader.ReadInt16());
                    return true;

                case ModelField.UnknownArray22:
                    var unknown22 = objectReader.ReadBytes((int)_group[ModelField.VertexCount]);
                    _group.Add(field, unknown22.Count());
                    return true;

                case ModelField.Vertices:
                    m_VertexCount = (int)_group[ModelField.VertexCount];
                    m_Vertices = objectReader.ReadSingleArray(3 * m_VertexCount);
                    return true;

                case ModelField.FacesSingle:
                    _group[field] = objectReader.ReadByte();
                    var indices = objectReader.ReadBytes((int)_group[ModelField.IndexCount]);
                    UnpackFaces(indices.Select(b => (ushort)b).ToArray());
                    return true;

                case ModelField.FacesDouble:
                    UnpackFaces(Enumerable.Range(0, (int)_group[ModelField.IndexCount])
                        .Select(i => objectReader.ReadUInt16()).ToArray());
                    return true;

                case ModelField.FaceGroups34:
                    if ((int)_group[ModelField.IndexCount] != 0)
                        _faceGroups = Enumerable.Range(0, (int)_group[ModelField.FaceCount])
                            .Select(i => objectReader.ReadInt16())
                            .ToArray();
                    return true;

                case ModelField.FaceGroups36:
                    var bitsPerItem = objectReader.ReadByte();
                    var byteCount = (int)_group[ModelField.FaceCount] * bitsPerItem / 32.0;
                    var faceGroups = objectReader.ReadBytes((int)Math.Ceiling(byteCount));
                    if (bitsPerItem == 16)
                        faceGroups = faceGroups.SelectMany(b => new[]
                        {
                            (byte)((b & 0xF0) >> 4),
                            (byte)(b & 0x0F)
                        })
                        .ToArray();

                    if (byteCount != Math.Ceiling(byteCount))
                        faceGroups = faceGroups.Take(faceGroups.Length - 1).ToArray();
                    _faceGroups = faceGroups.Select(b => (short)b).ToArray();
                    return true;

                case ModelField.UnknownArray113:
                    _group.Add(field, objectReader.ReadBytes(12));
                    return true;

                case ModelField.UnknownArray115:
                    _group.Add(field, Enumerable.Range(0, 6)
                        .Select(i => objectReader.ReadUInt32())
                        .Where(i => i != uint.MaxValue)
                        .ToArray());
                    return true;

                case ModelField.MaterialPower:
                case ModelField.MaterialAmbient:
                case ModelField.MaterialEmissive:
                case ModelField.MaterialSpecular:
                    _group.Add(field, Enumerable.Range(0, 4)
                        .Select(i => objectReader.ReadSingle())
                        .ToArray());
                    return true;

                case ModelField.UnknownShort149:
                case ModelField.UnknownShort151:
                    _group.Add(field, objectReader.ReadInt16());
                    return true;

                case ModelField.FaceCount:
                case ModelField.IndexCount:
                case ModelField.VertexCount:
                case ModelField.UnknownInt21:
                case ModelField.UnknownInt23:
                case ModelField.UnknownInt112:
                case ModelField.UnknownInt114:
                case ModelField.Texture:
                case ModelField.UnknownInt129:
                case ModelField.UnknownInt130:
                case ModelField.TextureGroup:
                case ModelField.UnknownInt152:
                case ModelField.UnknownInt154:
                case ModelField.UnknownInt176:
                case ModelField.UnknownInt178:
                case ModelField.UnknownInt192:
                case ModelField.UnknownInt193:
                case ModelField.UnknownInt194:
                case ModelField.UnknownInt195:
                case ModelField.UnknownInt198:
                case ModelField.UnknownInt199:
                case ModelField.UnknownInt200:
                case ModelField.UnknownInt201:
                case ModelField.UnknownInt202:
                    if (_group == null || _group.ContainsKey(field))
                        _groups.Add(_group = new Group());
                    _group.Add(field, objectReader.ReadInt32());
                    return true;

                case ModelField.UnknownFloat148:
                case ModelField.TextureDivisionU:
                case ModelField.TextureDivisionV:
                case ModelField.TexturePositionU:
                case ModelField.TexturePositionV:
                case ModelField.TextureRotateX:
                case ModelField.TextureRotateY:
                case ModelField.TextureRotateZ:
                case ModelField.UnknownFloat196:
                case ModelField.UnknownFloat197:
                    _group.Add(field, objectReader.ReadSingle());
                    return true;

                case ModelField.GroupName:
                    _group.Add(field, _groups.Count + " " + MxrObjectReader.ReadString(objectReader));
                    return true;

                default:
                    throw new NotImplementedException();
            }
        }

        private void UnpackFaces(ushort[] indices)
        {
            var i = 0;
            m_Indices = new List<uint>();

            while (i < indices.Length)
            {
                var face = Enumerable.Range(0, indices[i] + 1).Select(j => indices[i++]).ToArray();

                m_Indices.Add(face[1]);
                m_Indices.Add(face[2]);
                m_Indices.Add(face[3]);

                // Split quads into triangles
                if (face[0] == 4)
                {
                    m_Indices.Add(face[1]);
                    m_Indices.Add(face[3]);
                    m_Indices.Add(face[4]);
                }
            }
        }
    }
}