using System.Collections.Generic;
using System.Text;

namespace AssetStudio.Mxr.Classes
{
    class MxrEvents : TextAsset
    {
        public MxrEvents(ObjectReader objectReader)
            : base(objectReader) { }

        protected override void Read() { }

        public void Read(Dictionary<int, string> strings, long toPosition)
        {
            type = ClassIDType.MonoScript;
            string infoText = null;

            while (reader.BaseStream.Position < toPosition)
            {
                var address = reader.ReadInt32();
                strings.TryGetValue(address, out var s);
                infoText += $"{address:X8} {s}\n";
            }

            m_Name = "Events";
            m_Script = Encoding.UTF8.GetBytes(infoText);
        }
    }
}