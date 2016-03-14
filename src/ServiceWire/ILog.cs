#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    public interface ILog
    {
        #region Methods


        #region Public Methods

        void Debug(string formattedMessage,params object[] args);

        void Info(string formattedMessage,params object[] args);

        void Warn(string formattedMessage,params object[] args);

        void Error(string formattedMessage,params object[] args);

        void Fatal(string formattedMessage,params object[] args);

        #endregion


        #endregion
    }
}