#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


#region Usings

using System;
using System.IO.Pipes;

#endregion


namespace ServiceWire.NamedPipes
{
    public class PipeClientConnectionEventArgs:EventArgs
    {
        #region Constractor

        public PipeClientConnectionEventArgs(NamedPipeServerStream pipeStream)
        {
            PipeStream=pipeStream;
        }

        #endregion


        #region  Proporties

        public NamedPipeServerStream PipeStream { get; set; }

        #endregion
    }
}