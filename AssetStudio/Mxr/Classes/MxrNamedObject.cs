using System;
using System.Collections.Generic;

namespace AssetStudio.Mxr.Classes
{
    abstract class MxrNamedObject<T> : MxrNamedObject where T : Enum
    {
        protected MxrNamedObject(ObjectReader objectReader)
            : base(objectReader)
        {
            type = ClassIDType.Mesh;
        }

        protected readonly Dictionary<T, int> _fieldValues = new Dictionary<T, int>();

        protected abstract void Read(ObjectReader objectReader, T field);

        protected virtual bool OnFinish(ObjectReader objectReader) => true;

        protected virtual void OnStart(ref ObjectReader objectReader) => objectReader.ReadByte();

        protected virtual byte EndByte => 255;

        protected override void Read(ObjectReader objectReader)
        {
            _fieldValues.Clear();
            OnStart(ref objectReader);

            while (true)
            {
                var eof = objectReader.Position == objectReader.BaseStream.Length;
                var fieldByte = eof ? EndByte : objectReader.ReadByte();
                var field = (T)Enum.ToObject(typeof(T), fieldByte);

                if (fieldByte == EndByte && OnFinish(objectReader))
                {
                    if (!eof)
                        _fieldValues[field] = objectReader.ReadByte();
                    return;
                }
                else Read(objectReader, field);
            }
        }
    }
}