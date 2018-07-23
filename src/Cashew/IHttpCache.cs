using System;
using System.Runtime.CompilerServices;

[assembly:InternalsVisibleTo("Cashew.Tests")]
namespace Cashew
{
    /// <summary>
    /// The contract for caching functionality that is used for caching HTTP responses.
    /// </summary>
    public interface IHttpCache : IDisposable
    {
        /// <summary>
        /// Retrieves the value associated with the given key from the cache.
        /// </summary>
        /// <param name="key">The key that is used to identify the value in the cache.</param>
        /// <returns>The value in the cache that is associated with the given key.</returns>
        /// <exception cref="T:System.ArgumentNullException">If the <paramref name="key"/> is null</exception>
        StoredHttpResponseMessage GetResponse(string key);

		string GetString(string key);
        /// <summary>
        /// Removes the value associated with the given key from the cache.
        /// </summary>
        /// <param name="key">The key that is used to identify the value in the cache.</param>
        void Remove(string key);

        /// <summary>
        /// Puts a value associated with the given key into the cache.
        /// </summary>
        /// <param name="key">The key that is used to identify the value in the cache.</param>
        /// <param name="value">The value which should be cached associated with the given key.</param>
        /// <exception cref="T:System.ArgumentNullException">If the <paramref name="key"/> or <paramref name="value"/> is null.</exception>
        void Put(string key, StoredHttpResponseMessage value);

		void Put(string key, string value);
	}


}