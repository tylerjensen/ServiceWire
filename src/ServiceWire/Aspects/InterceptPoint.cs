#region File Creator

// This File Created By Ersin Tarhan
// For Project : ServiceWire - ServiceWire
// On 2016 03 14 04:35

#endregion


namespace ServiceWire.Aspects
{
    public class InterceptPoint
    {
        #region  Proporties

        public int Id { get; set; }
        public object Target { get; set; }
        public CrossCuttingConcerns Cut { get; set; }

        #endregion
    }
}