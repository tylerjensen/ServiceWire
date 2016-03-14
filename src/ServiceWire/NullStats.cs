#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:36

#endregion


namespace ServiceWire
{
    internal class NullStats:IStats
    {
        #region Methods


        #region Public Methods

        public void Log(string name,float value)
        {
        }

        public void Log(string category,string name,float value)
        {
        }

        public void LogSys()
        {
        }

        #endregion


        #endregion
    }
}