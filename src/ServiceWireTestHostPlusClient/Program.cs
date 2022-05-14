// Small Test Program To Debug issues with throwing errors over a channel
using System;
using ServiceWire.NamedPipes;

namespace ServiceWireTestHostPlustClient
{
    public class Program
    {
        public static void Main()
        {
            // Create Host
            var nphost = new NpHost("TEST", null, null);
            nphost.AddService<ISimpleError>(new SimpleError());

            // Create Client
            var npClient = new NpClient<ISimpleError>(new NpEndPoint("TEST"));
            // Test everything works
            if (npClient.Proxy.TestReturn3() != 3) throw new Exception("Interface not working");

            // See that we get the INNER exception with the Fixed code
            try
            {
                npClient.Proxy.RaiseAnError();
            }
            catch (Exception ex)
            {
                // Should be the exception we raised
                Console.WriteLine(ex.Message);
            }
            Console.ReadLine();
            nphost.Dispose();
        }
    }

    public interface ISimpleError
    {
        int TestReturn3();
        void RaiseAnError();
    }

    public class SimpleError : ISimpleError
    {
        public int TestReturn3() => 3;
        public void RaiseAnError()
        {
            throw new Exception("Deliberate Exception for Test");
        }
    }
}

