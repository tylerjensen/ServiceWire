using NetMQ;
using System;
using System.Net.Http;

namespace ServiceWire.Messaging
{
    /// <summary>
    /// Critical pass through to key NetMQConfig methods.
    /// </summary>
    internal static class Wire
    {
        /// <summary>
        /// Cleanup library resources, call this method when your process is shutting-down.
        /// </summary>
        /// <param name="block">Set to true when you want to make sure sockets send all pending messages</param>
        public static void Cleanup(bool block = true)
        {
            _publicIpAddress = string.Empty;
            NetMQConfig.Cleanup(block);
        }

        /// <summary>
        /// Get or set the default linger period for the all sockets,
        /// which determines how long pending messages which have yet to be sent to a peer
        /// shall linger in memory after a socket is closed.
        /// </summary>
        /// <remarks>
        /// This also affects the termination of the socket's context.
        /// -1: Specifies infinite linger period. Pending messages shall not be discarded after the socket is closed;
        /// attempting to terminate the socket's context shall block until all pending messages have been sent to a peer.
        /// 0: The default value of 0 specifies an no linger period. Pending messages shall be discarded immediately when the socket is closed.
        /// Positive values specify an upper bound for the linger period. Pending messages shall not be discarded after the socket is closed;
        /// attempting to terminate the socket's context shall block until either all pending messages have been sent to a peer,
        /// or the linger period expires, after which any pending messages shall be discarded.
        /// </remarks>
        public static TimeSpan Linger {
            get {
                return NetMQConfig.Linger;
            }
            set {
                NetMQConfig.Linger = value;
            }
        }

        /// <summary>
        /// Get or set the number of IO Threads NetMQ will create, default is 1.
        /// 1 is good for most cases.
        /// </summary>
        public static int ThreadPoolSize {
            get {
                return NetMQConfig.ThreadPoolSize;
            }
            set {
                NetMQConfig.ThreadPoolSize = value;
            }
        }

        /// <summary>
        /// Get or set the maximum number of sockets.
        /// </summary>
        public static int MaxSockets {
            get {
                return NetMQConfig.MaxSockets;
            }
            set {
                NetMQConfig.MaxSockets = value;
            }
        }

        private static object _syncRoot = new object();
        private static string _publicIpAddress = string.Empty;

        public static string PublicIpAddress 
        {
            get 
            {
                if (string.IsNullOrEmpty(_publicIpAddress))
                {
                    lock (_syncRoot)
                    {
                        if (string.IsNullOrEmpty(_publicIpAddress))
                        {
                            try
                            {
                                //TODO -- use pool? or dump this?
                                using (var client = new HttpClient())
                                {
                                    _publicIpAddress = client.GetAsync("http://checkip.amazonaws.com/")
                                        .Result
                                        .Content
                                        .ReadAsStringAsync()
                                        .Result;
                                }
                            }
                            catch
                            {
                                _publicIpAddress = "127.0.0.1";
                            }
                        }
                    }
                }
                return _publicIpAddress;
            }
        }
    }
}
