using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public interface INamedPipeServerStreamFactory
    {
        NamedPipeServerStream Create(string pipeName, PipeDirection direction, int maxNumberOfServerInstances, PipeTransmissionMode transmissionMode, PipeOptions options, int inBufferSize, int outBufferSize);
    }
}
