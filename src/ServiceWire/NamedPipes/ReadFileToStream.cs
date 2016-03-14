#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System.IO;

#endregion


namespace ServiceWire.NamedPipes
{
    public class ReadFileToStream
    {
        #region Constractor

        public ReadFileToStream(StreamString str,string filename)
        {
            fn=filename;
            ss=str;
        }

        #endregion


        #region Fields

        private readonly string fn;
        private readonly StreamString ss;

        #endregion


        #region Methods


        #region Public Methods

        public void Start()
        {
            var contents=File.ReadAllText(fn);
            ss.WriteString(contents);
        }

        #endregion


        #endregion
    }
}