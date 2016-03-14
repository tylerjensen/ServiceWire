#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    public interface IStats
    {
        #region Methods


        #region Public Methods

        void Log(string name,float value);

        void Log(string category,string name,float value);

        void LogSys();

        #endregion


        #endregion
    }
}