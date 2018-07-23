using System;
using CacheManager.Core;
using Moq;
using Xunit;

namespace Cashew.Adapters.CacheManager.Tests
{
    public class CacheManagerAdapterTests
    {
        [Fact]
        public void Constructor_CacheManagerIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheManagerAdapter(null, null));
        }

        [Fact]
        public void Get_KeyIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();
            var sut = new CacheManagerAdapter(cacheMock.Object,cacheMock2.Object);

            Assert.Throws<ArgumentNullException>(() => sut.GetString(null));
        }

        [Fact]
        public void Get_KeyIsValid_CacheManagerGetIsCalled()
        {
			var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();

			cacheMock.Setup(x => x.Get("key")).Returns("value");
			var sut = new CacheManagerAdapter(cacheMock.Object, cacheMock2.Object);

			var result = sut.GetString("key") as string;

            Assert.Equal("value", result);
            cacheMock.Verify(x => x.Get("key"), Times.Once);
        }

        [Fact]
        public void Put_KeyIsNull_ArgumentNullExceptionIsThrown()
        {
			var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();

			var sut = new CacheManagerAdapter(cacheMock.Object, cacheMock2.Object);

			Assert.Throws<ArgumentNullException>(() => sut.Put(null, "value"));
        }

        [Fact]
        public void Put_ValueIsNull_ArgumentNullExceptionIsThrown()
        {
			var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();

			var sut = new CacheManagerAdapter(cacheMock.Object, cacheMock2.Object);

			Assert.Throws<ArgumentNullException>(() => sut.Put("key", (string)null));
        }

        [Fact]
        public void Put_ParametersAreValid_CacheManagerPutIsCalled()
        {
			var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();

			cacheMock.Setup(x => x.Put("key", "value"));
			var sut = new CacheManagerAdapter(cacheMock.Object, cacheMock2.Object);

			sut.Put("key", "value");

            cacheMock.Verify(x => x.Put("key", "value"), Times.Once);
        }

		[Fact]
		public void Put_ResponsesAreValid_CacheManagerPutIsCalled()
		{
			var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();

			cacheMock.Setup(x => x.Put("key", "value"));
			var sut = new CacheManagerAdapter(cacheMock.Object, cacheMock2.Object);

			sut.Put("key", new StoredHttpResponseMessage());

			cacheMock2.Verify(x => x.Put("key", It.IsAny<StoredHttpResponseMessage>()), Times.Once);
		}

		[Fact]
        public void Dispose_CacheManagerDisposeIsCalled()
        {
			var cacheMock = new Mock<ICacheManager<string>>();
			var cacheMock2 = new Mock<ICacheManager<StoredHttpResponseMessage>>();

			var sut = new CacheManagerAdapter(cacheMock.Object, cacheMock2.Object);

			sut.Dispose();

            cacheMock.Verify(x => x.Dispose(), Times.Once);
        }
    }
}