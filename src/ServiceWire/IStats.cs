namespace ServiceWire
{
    public interface IStats
    {
        void Log(string name, float value);
        void Log(string category, string name, float value);
    }
}