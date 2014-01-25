using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace ServiceWire
{
    public class Logger : LoggerBase, ILog
    {
        private LogLevel _logLevel = LogLevel.Error;

        /// <summary>
        /// Set to Debug for all logging on. To None for no logging. 
        /// Order is: None, Fatal, Error, Warn, Info, Debug
        /// </summary>
        public LogLevel LogLevel 
        {
            get { return _logLevel; }
            set { _logLevel = value; }
        }

        private const string LogFilePrefixDefault = "log-";
        private const string LogFileExtensionDefault = ".txt";

        public Logger(string logDirectory = null,
            string logFilePrefix = null, 
            string logFileExtension = null, 
            LogLevel logLevel = LogLevel.Error, 
            int messageBufferSize = 32, 
            LogOptions options = LogOptions.LogOnlyToFile, 
            LogRollOptions rollOptions = LogRollOptions.Daily, 
            int rollMaxMegaBytes = 1024,
            bool useUtcTimeStamp = false)
        {
            _logDirectory = logDirectory ?? Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
            Directory.CreateDirectory(_logDirectory); //will throw if unable - does not throw if already exists
            _logFilePrefix = logFilePrefix ?? LogFilePrefixDefault;
            _logFileExtension = logFileExtension ?? LogFileExtensionDefault;
            _logLevel = logLevel;
            _messageBufferSize = messageBufferSize;
            _rollOptions = rollOptions;
            _rollMaxMegaBytes = rollMaxMegaBytes;
            _useUtcTimeStamp = useUtcTimeStamp;

            LogOptions = options; //setter validates
            if (_messageBufferSize < 1) _messageBufferSize = 1;
            if (_messageBufferSize > 4096) _messageBufferSize = 4096;
            if (_rollOptions == LogRollOptions.Size)
            {
                if (_rollMaxMegaBytes < 1) _rollMaxMegaBytes = 1;
                if (_rollMaxMegaBytes < 4096) _rollMaxMegaBytes = 4096;
            }
        }

        private void WriteMessage(LogLevel logLevel, string formattedMessage, params object[] args)
        {
            if (null == formattedMessage) return; //do nothing
            if ((int) logLevel <= (int) _logLevel)
            {
                string msg = (null != args && args.Length > 0)
                    ? string.Format(formattedMessage, args)
                    : formattedMessage;
                _logQueue.Enqueue(new string[] { string.Format("{0}\t{1}\t{2}", GetTimeStamp(), logLevel, msg) });
                if (_logQueue.Count >= _messageBufferSize)
                    Task.Factory.StartNew(() => WriteBuffer(_messageBufferSize));
            }
        }

        public void Debug(string formattedMessage, params object[] args)
        {
            WriteMessage(LogLevel.Debug, formattedMessage, args);
        }

        public void Info(string formattedMessage, params object[] args)
        {
            WriteMessage(LogLevel.Info, formattedMessage, args);
        }

        public void Warn(string formattedMessage, params object[] args)
        {
            WriteMessage(LogLevel.Warn, formattedMessage, args);
        }

        public void Error(string formattedMessage, params object[] args)
        {
            WriteMessage(LogLevel.Error, formattedMessage, args);
        }

        public void Fatal(string formattedMessage, params object[] args)
        {
            WriteMessage(LogLevel.Fatal, formattedMessage, args);
        }
    }
}