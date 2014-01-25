namespace ServiceWire
{
    internal class NullStats : IStats
    {
        public void Log(string name, float value)
        {
        }

        public void Log(string category, string name, float value)
        {
        }

        public void LogSys()
        {
        }
    }
}