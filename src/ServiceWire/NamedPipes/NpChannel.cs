using System;
using System.IO;
using System.IO.Pipes;

namespace ServiceWire.NamedPipes
{
    public class NpChannel : StreamingChannel
    {
        /// <summary>
        /// Creates a connection to the concrete object handling method calls on the pipeName server side
        /// </summary>
        /// <param name="serviceType"></param>
        /// <param name="npEndPoint"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        public NpChannel(Type serviceType, NpEndPoint npEndPoint, ISerializer serializer, ICompressor compressor,
            string identity, string identityKey, ILog log, IStats stats, int invokeTimeoutMs)
            : base(serializer, compressor, "inproc://" + npEndPoint.PipeName, identity, identityKey, log, stats, invokeTimeoutMs)
        {
            _serviceType = serviceType;
            try
            {
                SyncInterface();
            }
            catch (Exception)
            {
                this.Dispose(true);
                throw;
            }
        }
    }
}
