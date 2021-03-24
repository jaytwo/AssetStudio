using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace AssetStudio.Mxr
{
    class MxrStringSubstituter
    {
        private static readonly char[] _exemptions = "０１２３４５６７８９ＸＹＺ0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ.".ToArray();
        
        private readonly Dictionary<string, string> _strings = new Dictionary<string, string>();
        private readonly StreamWriter _writer;

        public MxrStringSubstituter(string path)
        {
            if (File.Exists(path + ".in.txt"))
                using (var keys = new StreamReader(File.OpenRead(path + ".in.txt")))
                using (var values = new StreamReader(File.OpenRead(path + ".out.txt")))
                    while (!keys.EndOfStream && !values.EndOfStream)
                        _strings[keys.ReadLine()] = values.ReadLine();

            _writer = new StreamWriter(path + ".in2.txt");
        }

        public void Save(string keysPath)
        {
            using (var writer = new StreamWriter(File.OpenWrite(keysPath)))
                foreach (var key in _strings.Keys
                    .Select(k => k.Intersect(_exemptions).Any() ? new string(k.Except(_exemptions).ToArray()) : k)
                    .OrderBy(k => k)
                    .Distinct()
                    .Where(k => k.Any(c => c >= '\u303f')))
                {
                    writer.WriteLine(key);
                }
        }

        public string Read(BinaryReader source)
        {
            var s = Encoding.GetEncoding(932).GetString(source.ReadBytes(source.ReadInt32()));
            var word = s.TrimEnd(_exemptions).Normalize();

            if (_strings.TryGetValue(word, out var translation))
                s = translation + new string(s.Substring(word.Length)
                    .Select(c => c >= '０' && c <= '９' ? (char)(c + '0' - '０') : c)
                    .ToArray());
            else
            {
                _strings.Add(word, word);

                if (word.Any(c => c >= '\u303f'))
                {
                    _writer.WriteLine(word);
                    _writer.Flush();
                }
            }

            return s;
        }
    }
}