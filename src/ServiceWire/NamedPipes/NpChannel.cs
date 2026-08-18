using System;
using System.IO;
using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public class NpChannel : StreamingChannel
    {
        private readonly NamedPipeClientStream _clientStream;
        private readonly NpChannelIdentifier _channelIdentifier;

        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the pipeName server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="npEndPoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        public NpChannel(Type serviceType, NpEndPoint npEndPoint, ISerializer serializer, ICompressor compressor, ILog logger = null, IStats stats = null)
            : base(serializer, compressor, logger, stats)
        {
            _serviceType = serviceType;
            _channelIdentifier = new NpChannelIdentifier(npEndPoint);
            _clientStream = new NamedPipeClientStream(npEndPoint.ServerName, npEndPoint.PipeName, PipeDirection.InOut);
            _clientStream.Connect(npEndPoint.ConnectTimeOutMs);
            _stream = _clientStream;
            //independent read and write buffers: a shared BufferedStream cannot serve
            //concurrent readers and writers (single buffer, mode switch needs a seek)
            _binReader = new BinaryReader(new BufferedStream(_clientStream));
            _binWriter = new BinaryWriter(new BufferedStream(_clientStream));
            try
            {
                SyncInterface(_serviceType);
            }
            catch (Exception)
            {
                this.Dispose(true);
                throw;
            }
        }

        protected override IChannelIdentifier ChannelIdentifier => _channelIdentifier;

        //named pipes stay on the v1 wire: their synchronous handles cannot overlap
        //reads and writes, so v2 buys no pipelining, and its per-call frame cost is
        //a measured net loss on a local transport
        protected override bool AllowWireV2 => false;

        public override bool IsConnected { get { return (null != _clientStream) && _clientStream.IsConnected; } }
    }
}
