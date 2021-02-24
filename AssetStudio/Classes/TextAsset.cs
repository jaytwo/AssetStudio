namespace AssetStudio
{
    public class TextAsset : NamedObject
    {
        public byte[] m_Script;

        public TextAsset(ObjectReader reader) : base(reader)
        {
            Read();
        }

        protected virtual void Read()
        {
            m_Script = reader.ReadUInt8Array();
        }
    }
}