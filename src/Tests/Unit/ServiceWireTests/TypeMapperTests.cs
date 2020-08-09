using ServiceWire;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace ServiceWireTests
{
    public class TypeMapperTests
    {
        [Fact]
        public void CanMapUriTypes()
        {
            Type uriType = typeof(Uri);

            string framworkDiff = $"{uriType.FullName}, System.Unknown";

            Type mapType = framworkDiff.ToType();

            Assert.NotNull(mapType);

            Assert.Equal(uriType.FullName, mapType.FullName);
        }

        [Fact]
        public async Task CanMapUriTypesAsync()
        {
            Type uriType = typeof(Uri);

            string framworkDiff = $"{uriType.FullName}, System.Unknown";

            Type mapType = await framworkDiff.ToTypeAsync();

            Assert.NotNull(mapType);

            Assert.Equal(uriType.FullName, mapType.FullName);
        }
    }
}
