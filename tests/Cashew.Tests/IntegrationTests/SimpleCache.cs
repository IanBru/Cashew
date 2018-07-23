using System.Collections.Generic;

namespace Cashew.Tests.IntegrationTests
{
    public class SimpleCache : IHttpCache
    {
        private readonly IDictionary<string,object> _cache = new Dictionary<string, object>();
        
        private object Get(string key)
        {
            if (_cache.TryGetValue(key, out var value))
            {
                return value;
            }
            return null;
        }

		public StoredHttpResponseMessage GetResponse(string key)
		{
			return Get(key) as StoredHttpResponseMessage;
		}

		public string GetString(string key)
		{
			return Get(key) as string;
		}

		public void Remove(string key)
        {
            _cache.Remove(key);
        }

		public void Put(string key, StoredHttpResponseMessage value)
		{
			Put(key,(object)value);
		}

		public void Put(string key, string value)
		{
			Put(key, (object)value);
		}

		private void Put(string key, object value)
        {
            Remove(key);
            _cache[key] = value;
        }

        public void Dispose()
        {
            _cache.Clear();
        }
    }
}