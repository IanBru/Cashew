using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using Cashew.Headers;

namespace Cashew.Tests
{
	public class ResponseEqualityComparer : IEqualityComparer<HttpResponseMessage>
	{
		public bool Equals(HttpResponseMessage x, HttpResponseMessage y)
		{
			if (x == null && y == null)
			{
				return true;
			}

			if (x == null || y == null)
			{
				return false;
			}
			if (x.Version != y.Version)
			{
				return false;
			}

			if (x.StatusCode != y.StatusCode)
			{
				return false;
			}

			foreach (var header in x.Headers)
			{
				string[] ignored =
				{
					HttpResponseHeaderExtensions.CashewStatusHeader,
					"Cache-Control",
					"Content-Length",
					"Date"
				};
				if (ignored.Contains(header.Key))
				{
					continue;
				}
				var expected = string.Join("; ", header.Value);
				var actual = string.Join("; ", y.Headers.GetValues(header.Key));
				if (expected != actual)
				{
					return false;
				}
			}
			foreach (var header in x.Content.Headers)
			{
				var expected = string.Join("; ", header.Value);
				var actual = string.Join("; ", y.Content.Headers.GetValues(header.Key));
				if (expected != actual)
				{
					return false;
				}
			}

			return true;

		}

		public int GetHashCode(HttpResponseMessage obj)
		{
			return HashCode.Of(obj.StatusCode)
				.And(obj.Headers)
				.And(obj.Content.Headers)
				.And(obj.Version);


		}



	}
}

public struct HashCode
{
	private readonly int value;
	private HashCode(int value)
	{
		this.value = value;
	}
	public static implicit operator int(HashCode hashCode)
	{
		return hashCode.value;
	}

	public static implicit operator HashCode(int hashCode)
	{
		return new HashCode(hashCode);
	}
	public static HashCode Of<T>(T item)
	{
		return new HashCode(GetHashCode(item));
	}
	public HashCode And<T>(T item)
	{
		return new HashCode(CombineHashCodes(this.value, GetHashCode(item)));
	}

	private static int CombineHashCodes(int h1, int h2)
	{
		unchecked
		{
			// Code copied from System.Tuple so it must be the best way to combine hash codes or at least a good one.
			return ((h1 << 5) + h1) ^ h2;
		}
	}
	private static int GetHashCode<T>(T item)
	{
		return item == null ? 0 : item.GetHashCode();
	}
}
