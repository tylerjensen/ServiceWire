* * * * * * * * * * * * * * * * *
* ServiceWire testing readme 
* * * * * * * * * * * * * * * * *

To run the tests, set your Visual Studio startup projects and 
run the test console projects in the following order:

ServiceWireTestHost
ServiceWireTestClient1
ServiceWireTestClient2

If you run in Debug, the test will run very slowly because
Visual Studio will load each dynamically generated client proxy.

We recommend that you run this test without debugging.

If you want to debug through a test, we recommend that you 
write your own specific test or that you wait for us to create
some additional tests.