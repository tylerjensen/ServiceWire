#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    internal class NullLogger:ILog
    {
        #region Methods


        #region Public Methods

        public void Debug(string formattedMessage,params object[] args)
        {
        }

        public void Info(string formattedMessage,params object[] args)
        {
        }

        public void Warn(string formattedMessage,params object[] args)
        {
        }

        public void Error(string formattedMessage,params object[] args)
        {
        }

        public void Fatal(string formattedMessage,params object[] args)
        {
        }

        #endregion


        #endregion
    }
}