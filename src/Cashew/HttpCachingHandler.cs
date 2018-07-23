using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Cashew.Headers;
using Cashew.Keys;

namespace Cashew
{
    /// <summary>
    /// The main class of this library, is supposed to be sent into the <see cref="HttpClient"/> constructor.
    /// </summary>
    public class HttpCachingHandler : DelegatingHandler
    {
        private readonly IHttpCache _cache;
        private readonly ICacheKeyStrategy _keyStrategy;

        internal ISystemClock SystemClock { get; set; } = new DefaultSystemClock();

        /// <summary>
        /// The list of status codes that are seen as cacheable by <see cref="HttpCachingHandler"/>.
        /// </summary>
        public HttpStatusCode[] CacheableStatusCodes { get; set; } = {
            HttpStatusCode.OK, HttpStatusCode.NonAuthoritativeInformation, HttpStatusCode.NoContent,
            HttpStatusCode.PartialContent, HttpStatusCode.MultipleChoices, HttpStatusCode.MovedPermanently,
            HttpStatusCode.NotFound, HttpStatusCode.MethodNotAllowed, HttpStatusCode.Gone,
            HttpStatusCode.RequestUriTooLong, HttpStatusCode.NotImplemented,
        };

        /// <summary>
        /// Initialises a new instance of the <see cref="HttpCachingHandler"/> class with the default message handler.
        /// </summary>
        /// <param name="cache">The <see cref="IHttpCache"/> that will be used to store HTTP responses.</param>
        /// <param name="keyStrategy">The <see cref="ICacheKeyStrategy"/> that will be used to create cache keys.</param>
        public HttpCachingHandler(IHttpCache cache, ICacheKeyStrategy keyStrategy) : this(cache, keyStrategy, new HttpClientHandler()) { }

        /// <summary>
        /// Initialises a new instance of the <see cref="HttpCachingHandler"/> class with the given <see cref="HttpMessageHandler"/>
        /// </summary>
        /// <param name="cache">The <see cref="IHttpCache"/>; that will be used to store HTTP responses.</param>
        /// <param name="keyStrategy">The <see cref="ICacheKeyStrategy"/> that will be used to create cache keys.</param>
        /// <param name="innerHandler">The <see cref="HttpMessageHandler"/> that will be used to send the HTTP request.</param>
        public HttpCachingHandler(IHttpCache cache, ICacheKeyStrategy keyStrategy, HttpMessageHandler innerHandler)
        {
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _keyStrategy = keyStrategy ?? throw new ArgumentNullException(nameof(keyStrategy));
            InnerHandler = innerHandler;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));

            if (!IsRequestCacheable(request))
            {
                return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            }

            //This does not seem entirely correct, should we have some other form of handling when request.CacheControl is null?
            EnsureRequestCacheControl(request);

            var key = _keyStrategy.GetCacheKey(request);

            var serializedCachedResponse = _cache.GetResponse(key);
            var cachedResponse = serializedCachedResponse?.CopyIntoResponseMessage(request);

			var status = CacheStatus.Miss;

            if (cachedResponse != null)
            {
                var currentAge = cachedResponse.CalculateAgeAt(SystemClock.UtcNow);
                var freshnessLifetime = cachedResponse.CalculateFreshnessLifetime();
                var isResponseFresh = IsResponseFresh(request, currentAge, freshnessLifetime);

                if (isResponseFresh && !cachedResponse.Headers.CacheControl.NoCache && !request.Headers.CacheControl.NoCache)
                {
                    cachedResponse.Headers.AddClientCacheStatusHeader(CacheStatus.Hit);
                    return cachedResponse;
                }

                var isStaleResponseAcceptable = IsStaleResponseAcceptable(request, cachedResponse, currentAge, freshnessLifetime);
                if (isStaleResponseAcceptable)
                {
                    cachedResponse.Headers.AddClientCacheStatusHeader(CacheStatus.Stale);
                    return cachedResponse;
                }

                //If we reach this point it must mean that the cached reponse was NOT fresh and it was NOT acceptable to return a stale response, 
                //therefore we need try to revalidate the cached response.
				status = CacheStatus.Revalidated;
                request.Headers.AddCacheValidationHeader(cachedResponse);
            }
            else if (request.Headers.CacheControl.OnlyIfCached)
            {
                var gatewayResponse = new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                {
                    RequestMessage = request,
                };
                gatewayResponse.Headers.AddClientCacheStatusHeader(CacheStatus.Miss);

                return gatewayResponse;
            }

            var serverResponse = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
			serverResponse.Headers.AddClientCacheStatusHeader(status);
            return await HandleServerResponse(request, serverResponse, key, serializedCachedResponse, status).ConfigureAwait(false);
        }

        private async Task<HttpResponseMessage> HandleServerResponse(HttpRequestMessage request, HttpResponseMessage serverResponse, string cacheKey, StoredHttpResponseMessage storedCachedResponse, CacheStatus status)
        {
            if (serverResponse == null)
            {
                return null;
            }

            var updatedCacheKey = _keyStrategy.GetCacheKey(request, serverResponse);


            var wasRevalidated = status == CacheStatus.Revalidated;
            var isResponseCacheable = IsResponseCacheable(serverResponse);

            if (wasRevalidated)
            {

                if (serverResponse.StatusCode == HttpStatusCode.NotModified)
				{
					var cachedResponse = storedCachedResponse.CopyIntoResponseMessage(request);
                    cachedResponse.Headers.CacheControl = serverResponse.Headers.CacheControl;
                    cachedResponse.RequestMessage = request;
                    cachedResponse.Headers.Date = SystemClock.UtcNow;

                    _cache.Put(cacheKey, storedCachedResponse);

                    //We need to dispose the response from the server since we're not returning it and because of that the pipeline will not dispose it.
                    serverResponse.Dispose();
					cachedResponse.Headers.AddClientCacheStatusHeader(status);
                    return cachedResponse;
                }

                if (isResponseCacheable)
                {
                    _cache.Remove(cacheKey);
                    var serializedResponse = await StoredHttpResponseMessage.Create(serverResponse).ConfigureAwait(false);
                    _cache.Put(updatedCacheKey, serializedResponse);
                }
            }
            else if (isResponseCacheable)
            {
                var serializedResponse = await StoredHttpResponseMessage.Create(serverResponse).ConfigureAwait(false);
                _cache.Put(updatedCacheKey, serializedResponse);
            }
			serverResponse.Headers.AddClientCacheStatusHeader(status);

			return serverResponse;
        }

        private static bool IsRequestCacheable(HttpRequestMessage request)
        {
            if (request.Method != HttpMethod.Get)
            {
                return false;
            }

            var cacheControlHeaders = request.Headers.CacheControl;
            if (cacheControlHeaders != null && cacheControlHeaders.NoStore)
            {
                return false;
            }

            return true;
        }

        private static bool IsResponseFresh(HttpRequestMessage request, TimeSpan? currentAge, TimeSpan? freshnessLifeTime)
        {
            if (currentAge == null || freshnessLifeTime == null)
            {
                return false;
            }

            if (request.Headers.CacheControl.MinFresh.HasValue)
            {
                return currentAge.Value + request.Headers.CacheControl.MinFresh.Value <= freshnessLifeTime.Value;
            }

            return freshnessLifeTime.Value > currentAge.Value;
        }

        private static bool IsStaleResponseAcceptable(HttpRequestMessage request, HttpResponseMessage cachedResponse, TimeSpan? currentAge, TimeSpan? freshnessLifetime)
        {
            var responseCacheControl = cachedResponse.Headers.CacheControl;
            var requestCacheControl = request.Headers.CacheControl;
            if (responseCacheControl == null)
            {
                return false;
            }

            if (requestCacheControl.NoCache || responseCacheControl.NoCache)
            {
                return false;
            }

            if (responseCacheControl.MustRevalidate)
            {
                return false;
            }

            if (requestCacheControl.MaxStale)
            {
                return true;
            }

            if (currentAge == null || freshnessLifetime == null)
            {
                return false;
            }

            if (requestCacheControl.MaxStaleLimit.HasValue)
            {
                var lifetimeFreshnessDifference = currentAge.Value - freshnessLifetime.Value;
                return lifetimeFreshnessDifference <= requestCacheControl.MaxStaleLimit.Value;
            }

            if (requestCacheControl.MaxAge.HasValue)
            {
                return requestCacheControl.MaxAge.Value > currentAge.Value;
            }

            return false;
        }

        private static void EnsureRequestCacheControl(HttpRequestMessage request)
        {
            if (request.Headers.CacheControl == null)
            {
                request.Headers.CacheControl = new CacheControlHeaderValue();
            }
        }

        private bool IsResponseCacheable(HttpResponseMessage response)
        {
            if (!CacheableStatusCodes.Contains(response.StatusCode))
            {
                return false;
            }

            var cacheControl = response.Headers.CacheControl;
            if (cacheControl == null)
            {
                return false;
            }

            if (cacheControl.NoStore)
            {
                return false;
            }

            if (response.Content == null)
            {
                return false;
            }

            return cacheControl.SharedMaxAge != null || cacheControl.MaxAge != null || response.Content.Headers.Expires != null;
        }

        protected override void Dispose(bool disposeManaged)
        {
            if (disposeManaged)
            {
                _cache.Dispose();
                _keyStrategy.Dispose();
            }

            base.Dispose(disposeManaged);
        }
    }
}