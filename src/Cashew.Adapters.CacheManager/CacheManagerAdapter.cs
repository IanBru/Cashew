using System;
using CacheManager.Core;

namespace Cashew.Adapters.CacheManager
{
    public class CacheManagerAdapter : IHttpCache
    {
		private readonly ICacheManager<string> _strings;
		private readonly ICacheManager<StoredHttpResponseMessage> _responses;

		public CacheManagerAdapter(
			ICacheManager<string> strings,
			ICacheManager<StoredHttpResponseMessage> responses)
        {
			_strings = strings ?? throw new ArgumentNullException(nameof(strings));
			_responses = responses ?? throw new ArgumentNullException(nameof(responses));
		}

		public StoredHttpResponseMessage GetResponse(string key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			return _responses.Get(key);
		}

		public string GetString(string key)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			return _strings.Get(key);
		}

		public void Remove(string key)
        {
            if (key == null) throw new ArgumentNullException(nameof(key));
			_responses.Remove(key);
			_strings.Remove(key);
		}

		public void Put(string key, StoredHttpResponseMessage value)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			if (value == null) throw new ArgumentNullException(nameof(value));
			_responses.Put(key,value);
		}

		public void Put(string key, string value)
		{
			if (key == null) throw new ArgumentNullException(nameof(key));
			if (value == null) throw new ArgumentNullException(nameof(value));
			_strings.Put(key,value);
		}

		public void Dispose()
        {
			_strings.Dispose();
			_responses.Dispose();
			GC.SuppressFinalize(this);
        }
    }
}