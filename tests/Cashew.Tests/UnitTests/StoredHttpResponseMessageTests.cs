using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using Newtonsoft.Json;
using Xunit;

namespace Cashew.Tests.UnitTests
{
	public class StoredHttpResponseMessageTests
	{

		[Fact]
		public void StoredHttpResponse_Serializes_With_Json()
		{
			var sample = Sample();
			var serializer = new JsonSerializer {TypeNameHandling = TypeNameHandling.Objects};
			var stored = StoredHttpResponseMessage.Create(sample).Result;
			string serialized;

			using (var writer = new StringWriter())
			{
				serializer.Serialize(writer,stored);
				serialized = writer.ToString();
			}

			StoredHttpResponseMessage restored;
			using (var reader = new StringReader(serialized))
			{
				restored = (StoredHttpResponseMessage)serializer.Deserialize(reader, typeof(object));
			}

			Check(sample,restored.CopyIntoResponseMessage(new HttpRequestMessage()));
		}

		[Fact]
		public void StoredHttpResponse_Serializes_With_BinaryFormatter()
		{
			var sample = Sample();
			var serializer = new BinaryFormatter();
			var stored = StoredHttpResponseMessage.Create(sample).Result;
		    byte[] serialized;

			using (var writeStream = new MemoryStream())
			{
				serializer.Serialize(writeStream, stored);
				serialized = writeStream.ToArray();
			}

			StoredHttpResponseMessage restored;
			using (var readStream = new MemoryStream(serialized))
			{
				restored = (StoredHttpResponseMessage)serializer.Deserialize(readStream);
			}

			Check(sample, restored.CopyIntoResponseMessage(new HttpRequestMessage()));
		}


		private const string Html = "<html><head><title>Hello</title></head><body><h1>Hello</hi></body></html>";
		public HttpResponseMessage Sample()
		{
			var retval=new HttpResponseMessage(HttpStatusCode.OK);

			retval.Headers.Age = TimeSpan.FromMinutes(10);
			retval.Headers.Server.Add(new ProductInfoHeaderValue("Cashew","1.0.1"));
			retval.Headers.Server.Add(new ProductInfoHeaderValue("Cashew.Tests", "1.0.3"));
			retval.Content = new StringContent(Html, Encoding.UTF8, "text/html");
			retval.Content.Headers.Expires = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1));

			return retval;
		}

		public void Check(HttpResponseMessage sample, HttpResponseMessage regenerated)
		{
			foreach (var header in sample.Headers)
			{
				var expected = string.Join("; ", header.Value);
				var actual = string.Join("; ", regenerated.Headers.GetValues(header.Key));
				Assert.True(expected==actual,$"Header {header.Key} did not match {expected}");
			}
			foreach (var header in sample.Content.Headers)
			{
				var expected = string.Join("; ", header.Value);
				var actual = string.Join("; ", regenerated.Content.Headers.GetValues(header.Key));
				Assert.True(expected == actual, $"Content Header {header.Key} did not match {expected}");
			}
			Assert.True(sample.StatusCode==regenerated.StatusCode,$"Status code mismatch {sample.StatusCode}!={regenerated.StatusCode}");
			Assert.True(sample.Version == regenerated.Version, $"Version mismatch {sample.StatusCode}!={regenerated.StatusCode}");

			var expectedContent = sample.Content.ReadAsStringAsync().Result;
			var actualContent = regenerated.Content.ReadAsStringAsync().Result;

			Assert.True(expectedContent == actualContent, $"Content mismatch {expectedContent}!={actualContent}");

		}
	}
}
