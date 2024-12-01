using System.IO;

namespace ServiceWire.NamedPipes
{
    public class ReadFileToStream
    {
        private readonly string _fileName;
        private readonly StreamString _streamString;

        public ReadFileToStream(StreamString str, string filename)
        {
            _fileName = filename;
            _streamString = str;
        }

        public void Start()
        {
            _streamString.WriteString(File.ReadAllText(_fileName));
        }
    }
}