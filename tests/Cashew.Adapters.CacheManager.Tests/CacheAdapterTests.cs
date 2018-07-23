using System;
using CacheManager.Core;
using Moq;
using Xunit;

namespace Cashew.Adapters.CacheManager.Tests
{
    public class CacheAdapterTests
    {
        [Fact]
        public void Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
            Assert.Throws<ArgumentNullException>(() => new CacheAdapter(null,null));
        }

        [Fact]
        public void Get_KeyIsNull_ArgumentNullExceptionIsThrown()
        {
            var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();
            var sut = new CacheAdapter(cacheMock.Object,cacheMock2.Object);

            Assert.Throws<ArgumentNullException>(() => sut.GetResponse(null));
        }

        [Fact]
        public void Get_KeyIsValid_CacheGetIsCalled()
        {
			var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();
            cacheMock.Setup(x => x.Get("key")).Returns("value");
			var sut = new CacheAdapter(cacheMock.Object, cacheMock2.Object);

			var result = sut.GetString("key");

            Assert.Equal("value", result);
            cacheMock.Verify(x => x.Get("key"), Times.Once);
        }

        [Fact]
        public void Put_KeyIsNull_Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
			var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();

			var sut = new CacheAdapter(cacheMock.Object, cacheMock2.Object);

			Assert.Throws<ArgumentNullException>(() => sut.Put(null, "abc"));
        }

        [Fact]
        public void Put_ValueIsNull_Constructor_CacheIsNull_ArgumentNullExceptionIsThrown()
        {
			var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();

			var sut = new CacheAdapter(cacheMock.Object, cacheMock2.Object);

			Assert.Throws<ArgumentNullException>(() => sut.Put("key", (string)null));
        }

        [Fact]
        public void Put_ParametersAreValid_CachePutIsCalled()
        {
            var keyToCheck = "key";
            var valueToCheck = "value";
			var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();

			var sut = new CacheAdapter(cacheMock.Object, cacheMock2.Object);

			sut.Put(keyToCheck, valueToCheck);

            cacheMock.Verify(cache => cache.Put(It.Is<string>(key => key == keyToCheck), It.Is<string>(value => (string)value == valueToCheck)), Times.Once);
        }

		[Fact]
		public void Put_ParametersAreValidResponse_CachePutIsCalled()
		{
			var keyToCheck = "key";

			var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();

			var sut = new CacheAdapter(cacheMock.Object, cacheMock2.Object);

			sut.Put(keyToCheck, new StoredHttpResponseMessage());

			cacheMock2.Verify(cache => cache.Put(It.Is<string>(key => key == keyToCheck), It.IsAny<StoredHttpResponseMessage>()), Times.Once);
		}

		[Fact]
        public void Dispose_CacheDisposeIsCalled()
        {
			var cacheMock = new Mock<ICache<string>>();
			var cacheMock2 = new Mock<ICache<StoredHttpResponseMessage>>();

			var sut = new CacheAdapter(cacheMock.Object, cacheMock2.Object);

			sut.Dispose();

            cacheMock.Verify(x => x.Dispose(), Times.Once);
        }
    }
}