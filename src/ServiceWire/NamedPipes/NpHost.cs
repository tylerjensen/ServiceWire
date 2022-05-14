using ServiceWire.ZeroKnowledge;
using System;
using System.IO;

namespace ServiceWire.NamedPipes
{
    public class NpHost : Host
    {
        private string _pipeName;

        /// <summary>
        /// Constructs an instance of the host and starts listening for incoming connections.
        /// All listener threads are regular background threads.
        /// 
        /// NOTE: the instance is not automatically thread safe!
        /// </summary>
        /// <param name="pipeName">The pipe name for incoming requests</param>
        /// <param name="log"></param>
        /// <param name="stats"></param>
        /// <param name="serializer">Inject your own serializer for complex objects and avoid using the Newtonsoft JSON DefaultSerializer.</param>
        /// <param name="compressor">Inject your own compressor and avoid using the standard GZIP DefaultCompressor.</param>
        /// <param name="zkRepository">Inject your zero knowledge password hash lookup repository. Null ignores ZK algorithm.</param>
        /// <param name="sessionTimeoutMins">How long in minutes before the host will close an unused client connection. Default is 20 minutes.</param>
        public NpHost(string pipeName, ILog log = null, IStats stats = null, ISerializer serializer = null, ICompressor compressor = null, 
            IZkRepository zkRepository = null, int sessionTimeoutMins = 20)
            : base("inproc://" + pipeName, sessionTimeoutMins, serializer, compressor, log, stats, zkRepository)
        {
            _pipeName = pipeName;
        }

        /// <summary>
        /// Gets the end point this host is listening on
        /// </summary>
        public string PipeName
        {
            get { return _pipeName; }
        }
    }
}
