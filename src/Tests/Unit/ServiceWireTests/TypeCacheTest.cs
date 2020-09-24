using ServiceWire;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text;
using Xunit;

namespace ServiceWireTests
{
	public class TypeCacheTest
	{
		[Fact]
		public void Cached()
		{
			var sut = new TypeCacheImpl();
			var type = typeof(DataSet);

			Assert.Null(sut[type]);

			var entry = new TypeCacheEntry { ConfigName = "aaa" };
			sut.Add(type, entry);
			sut.Add(type, entry); // should not fail

			Assert.Equal(entry.ConfigName, sut[type].ConfigName);
		}

	}
}
