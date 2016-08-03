namespace ServiceWire.Aspects
{
    public class InterceptPoint
    {
        public int Id { get; set; }
        public object Target { get; set; }
        public CrossCuttingConcerns Cut { get; set; }
    }
}