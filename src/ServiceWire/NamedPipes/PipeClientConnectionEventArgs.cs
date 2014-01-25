using System;
using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public class PipeClientConnectionEventArgs : EventArgs
    {
        public PipeClientConnectionEventArgs(NamedPipeServerStream pipeStream)
        {
            this.PipeStream = pipeStream;
        }
        public NamedPipeServerStream PipeStream { get; set; }
    }
}