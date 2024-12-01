using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public class DefaultNamedPipeServerStreamFactory : INamedPipeServerStreamFactory
    {
        public NamedPipeServerStream Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, 
            PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize)
        {
            return new NamedPipeServerStream(pipeName, direction, maxNumberOfServerInstances, transmissionMode, options, inBufferSize, outBufferSize);
        }
    }
}
