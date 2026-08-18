using System;
using System.Linq;

namespace ServiceWire.InteropContract
{
    public class InteropDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime When { get; set; }
    }

    public interface IInteropSvc
    {
        int Add(int a, int b);
        string Concat(string a, string b);
        DateTime EchoDate(DateTime d);
        Guid EchoGuid(Guid g);
        int[] EchoInts(int[] values);
        string[] EchoStrings(string[] values);
        int CountStrings(string[] values);
        InteropDto EchoDto(InteropDto dto);
        void Boom(string message);
    }

    public class InteropSvc : IInteropSvc
    {
        public int Add(int a, int b) => a + b;
        public string Concat(string a, string b) => a + b;
        public DateTime EchoDate(DateTime d) => d;
        public Guid EchoGuid(Guid g) => g;
        public int[] EchoInts(int[] values) => values;
        public string[] EchoStrings(string[] values) => values;
        public int CountStrings(string[] values) => values.Length;
        public InteropDto EchoDto(InteropDto dto) => dto;
        public void Boom(string message) => throw new InvalidOperationException(message);
    }

    /// <summary>
    /// The interop call battery both drivers run. Returns null on success or a
    /// failure description. No ServiceWire dependency so both peer versions share it.
    /// </summary>
    public static class InteropBattery
    {
        public static string Run(IInteropSvc proxy, bool largeStringArrays, bool includeThrowTest)
        {
            try
            {
                if (proxy.Add(2, 40) != 42) return "Add failed";
                if (proxy.Concat("inter", "op") != "interop") return "Concat failed";

                var utc = new DateTime(2024, 12, 5, 10, 30, 15, 123, DateTimeKind.Utc);
                var echoedDate = proxy.EchoDate(utc);
                if (echoedDate != utc || echoedDate.Kind != DateTimeKind.Utc) return "EchoDate failed: " + echoedDate.ToString("o");

                var guid = new Guid("a1b2c3d4-e5f6-4a5b-8c7d-9e0f1a2b3c4d");
                if (proxy.EchoGuid(guid) != guid) return "EchoGuid failed";

                var ints = Enumerable.Range(0, 1000).ToArray();
                var echoedInts = proxy.EchoInts(ints);
                if (!echoedInts.SequenceEqual(ints)) return "EchoInts failed";

                var strings = new[] { "one", "two", "three" };
                var echoedStrings = proxy.EchoStrings(strings);
                if (!echoedStrings.SequenceEqual(strings)) return "EchoStrings failed";

                if (largeStringArrays)
                {
                    //one-way only: a large string[] over the compression threshold
                    //exercises the CompressedUnknown sender path. A 6.0.1 peer can
                    //RECEIVE that frame but cannot SEND one correctly (the bug fixed
                    //in 7.0), so the old peer must never echo a large array back.
                    var large = Enumerable.Range(0, 50).Select(i => new string((char)('a' + (i % 26)), 200)).ToArray();
                    if (proxy.CountStrings(large) != large.Length) return "CountStrings failed";
                }

                var dto = new InteropDto { Id = 7, Name = "seven", When = utc };
                var echoedDto = proxy.EchoDto(dto);
                if (echoedDto.Id != dto.Id || echoedDto.Name != dto.Name || echoedDto.When != dto.When) return "EchoDto failed";

                if (includeThrowTest)
                {
                    try
                    {
                        proxy.Boom("interop boom");
                        return "Boom did not throw";
                    }
                    catch (InvalidOperationException) { }
                }

                return null;
            }
            catch (Exception ex)
            {
                return "Battery exception: " + ex.GetType().FullName + ": " + ex.Message;
            }
        }
    }
}
