using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Cashew
{
	[Serializable]
    public class StoredHttpResponseMessage
	{
		public List<Tuple<string, string[]>> Headers { get; set; }
			= new List<Tuple<string, string[]>>();

		public List<Tuple<string, string[]>> ContentHeaders { get; set; }
			= new List<Tuple<string, string[]>>();

		public string Version { get; set; }

		public HttpStatusCode StatusCode { get; set; }

		public byte[] Content { get; set; }

		public HttpResponseMessage CopyIntoResponseMessage()
		{
			var retval = new HttpResponseMessage(StatusCode)
			{
				Version = new Version(Version),
				Content = new StreamContent(new MemoryStream(Content)),
			};

			foreach (var header in Headers)
			{
				retval.Headers.Add(header.Item1,header.Item2);
			}

			if (retval.Headers.CacheControl == null)
			{
				retval.Headers.CacheControl = new CacheControlHeaderValue();
			}

			foreach (var cheader in ContentHeaders)
			{
				retval.Content.Headers.Add(cheader.Item1,cheader.Item2);
			}

			return retval;
		}

		public static async Task<StoredHttpResponseMessage> Create(HttpResponseMessage response)
		{
			byte[] content;
			if (response.Content == null)
			{
				content = new byte[0];
			}
			else
			{
				content = await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
			}

			return Create(response, content);
		}

		internal static StoredHttpResponseMessage Create(HttpResponseMessage response, byte[] content)
		{
			var retval = new StoredHttpResponseMessage
			{
				Content = content,
				StatusCode = response.StatusCode,
				Version = response.Version?.ToString(),
			};

			if (response.Headers != null)
			{
				foreach (var header in response.Headers)
				{
					retval.Headers.Add(new Tuple<string, string[]>(header.Key, header.Value.ToArray()));
				}
			}


			if (response.Content != null)
			{
				foreach (var cheader in response.Content?.Headers)
				{
					retval.ContentHeaders.Add(new Tuple<string, string[]>(cheader.Key, cheader.Value.ToArray()));
				}

			}


			return retval;
		}
	}
}
