using System;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ServiceWire;
using ServiceWire.NamedPipes;
using Xunit;

namespace ServiceWireTests
{
	public class AsyncTests : IDisposable
	{
		private NpHost _nphost;

		[Fact]
		public async Task Divide_ShouldThrow_Exception()
		{
			await Task.Factory.StartNew(StartPipeListener, TaskCreationOptions.LongRunning);

			await Task.Delay(2000);

			var npClient = new NpClient<ISum>(new NpEndPoint("8642D0EA-13B4-408F-8DBB-BB8CDEAAA0EC"), new CustomPipeSerializer());

			var exception = await Assert.ThrowsAsync<PipeException>(async () =>
			{
				await npClient.Proxy.Divide(4.0, 0);
			});

			Assert.IsType<PipeException>(exception);
		}

		void StartPipeListener()
		{
			_nphost = new NpHost("8642D0EA-13B4-408F-8DBB-BB8CDEAAA0EC", null, null, new CustomPipeSerializer());
			_nphost.UseCompression = true;
			_nphost.AddService<ISum>(new Sum());
			_nphost.Open();
		}

		public void Dispose()
		{
			_nphost.Close();
		}
	}

	public interface ISum
	{
		Task<double> Divide(double a, int b);
	}

	public class Sum : ISum
	{
		public Task<double> Divide(double a, int b)
		{
			if (b == 0)
				throw new PipeException("Trying to divide by zero.");

			return Task.FromResult(a / b);
		}
	}

	internal class PipeException : ApplicationException
	{
		public PipeException(string message = null)
			: base(message)
		{
		}
	}

	internal class CustomPipeSerializer : ISerializer
	{
		private readonly JsonSerializerOptions _customOptions;

		public CustomPipeSerializer()
		{
			_customOptions = new JsonSerializerOptions()
			{
				Converters = { new PipeExceptionJsonConverter(), new JsonStringEnumConverter() },
			};
		}

		public byte[] Serialize<T>(T obj) => obj == null ? null : JsonSerializer.SerializeToUtf8Bytes<T>(obj);

		public byte[] Serialize(object obj, string typeConfigName) =>
			obj == null ? null : JsonSerializer.SerializeToUtf8Bytes(obj, typeConfigName.ToType(), _customOptions);

		public T Deserialize<T>(byte[] bytes) => bytes == null || bytes.Length == 0
			? default(T)
			: JsonSerializer.Deserialize<T>((ReadOnlySpan<byte>)bytes, _customOptions);

		public object Deserialize(byte[] bytes, string typeConfigName)
		{
			Type t = typeConfigName != null ? typeConfigName.ToType() : throw new ArgumentNullException(nameof(typeConfigName));
			return bytes == null || bytes.Length == 0
				? t.GetDefault()
				: JsonSerializer.Deserialize((ReadOnlySpan<byte>)bytes, t, _customOptions);
		}
	}

	internal class PipeExceptionJsonConverter : JsonConverter<PipeException>
	{
		public override PipeException Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
		{
			if (reader.TokenType != JsonTokenType.StartObject)
			{
				throw new JsonException();
			}

			string message = "Message Not Available.";

			while (reader.Read())
			{
				if (reader.TokenType == JsonTokenType.EndObject)
				{
					break;
				}

				if (reader.TokenType == JsonTokenType.PropertyName)
				{
					var propertyName = reader.GetString();
					reader.Read();

					switch (propertyName)
					{
						case "Message":
							var messageRead = reader.GetString();
							if (messageRead != null) message = messageRead;
							break;
					}
				}
			}

			return new PipeException(message);
		}

		public override void Write(Utf8JsonWriter writer, PipeException value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();
			writer.WriteString("Message", value.Message);
			writer.WriteEndObject();
		}
	}
}