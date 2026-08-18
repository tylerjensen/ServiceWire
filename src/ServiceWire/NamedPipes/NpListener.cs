using System;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace ServiceWire.NamedPipes
{
    public class NpListener
    {
        private volatile bool _running;
        private readonly EventWaitHandle _terminateHandle = new EventWaitHandle(false, EventResetMode.AutoReset);
        private readonly int _maxConnections = 254;
        private readonly ILog _log;
        private readonly IStats _stats;
        private readonly INamedPipeServerStreamFactory _streamFactory;

        public string PipeName { get; set; }

        public event EventHandler<PipeClientConnectionEventArgs> RequestReieved;
        internal event Action<Exception> Faulted;

        public NpListener(string pipeName, int maxConnections = 254, ILog log = null, IStats stats = null, INamedPipeServerStreamFactory streamFactory = null)
        {
            _log = log ?? new NullLogger();
            _stats = stats ?? new NullStats();
            if (maxConnections > 254) maxConnections = 254;
            _maxConnections = maxConnections;
            this.PipeName = pipeName;
            _streamFactory = streamFactory ?? new DefaultNamedPipeServerStreamFactory();
        }

        public void Start()
        {
            _terminateHandle.Reset();
            _running = true;
            Task.Factory.StartNew(() => ServerLoop(), TaskCreationOptions.LongRunning);
        }

        public void Stop()
        {
            if (_running)
            {
                _running = false;
                //make fake connection to terminate the waiting stream
                try
                {
                    using (var client = new NamedPipeClientStream(PipeName))
                    {
                        client.Connect(50);
                    }
                }
                catch (TimeoutException)
                {
                    // The listener task may not have reached WaitForConnection yet.
                    // It will observe _running == false and terminate normally.
                }
                catch (Exception e)
                {
                    _log.Error("Stop error: {0}", e.ToString().Flatten());
                }
                _terminateHandle.WaitOne();
            }
        }

        private void ServerLoop()
        {
            try
            {
                while (_running)
                {
                    ProcessNextClient();
                }
            }
            catch (Exception e)
            {
                Faulted?.Invoke(e);
                _log.Fatal("ServerLoop fatal error: {0}", e.ToString().Flatten());
            }
            finally
            {
                _running = false;
                _terminateHandle.Set();
            }
        }

        private void ProcessClientThread(NamedPipeServerStream pipeStream)
        {
            try
            {
                if (this.RequestReieved != null) //has event subscribers
                {
                    var args = new PipeClientConnectionEventArgs(pipeStream);
                    RequestReieved(this, args);
                }
            }
            catch (Exception e)
            {
                _log.Error("ProcessClientThread error: {0}", e.ToString().Flatten());
            }
            finally
            {
                if (pipeStream.IsConnected) pipeStream.Close();
                pipeStream.Dispose();
            }
        }

        public void ProcessNextClient()
        {
            NamedPipeServerStream pipeStream = null;
            try
            {
                pipeStream = _streamFactory.Create(PipeName, PipeDirection.InOut, _maxConnections, PipeTransmissionMode.Byte, PipeOptions.None, 8192, 8192);
                pipeStream.WaitForConnection();

                // Stop() makes a local connection to release WaitForConnection. Do not
                // dispatch that sentinel connection as a real client request.
                if (!_running) return;

                //Task.Factory.StartNew(() => ProcessClientThread(pipeStream), TaskCreationOptions.LongRunning);
                var connectedPipeStream = pipeStream;
                pipeStream = null; //ownership is transferred to the client task
                Task.Factory.StartNew(() => ProcessClientThread(connectedPipeStream));
            }
            catch (IOException e) when (IsAllPipeInstancesBusy(e) && _running)
            {
                // The listener can temporarily exhaust the Windows named-pipe instance
                // limit. Back off before retrying; all other failures are fatal.
                Thread.Sleep(50);
            }
            finally
            {
                pipeStream?.Dispose();
            }
        }

        private static bool IsAllPipeInstancesBusy(IOException exception)
        {
            const int errorPipeBusy = 231;
            return (exception.HResult & 0xffff) == errorPipeBusy;
        }
    }

    // Defines the data protocol for reading and writing strings on our stream

    // Contains the method executed in the context of the impersonated user
}
