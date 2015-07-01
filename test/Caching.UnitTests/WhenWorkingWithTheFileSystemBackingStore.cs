using System;
using System.Collections.Specialized;
using System.IO;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using Psns.Common.Test.BehaviorDrivenDevelopment;
using Psns.Common.Persistence.Caching;

using Microsoft.Practices.EnterpriseLibrary.Caching;
using Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;

namespace Caching.UnitTests
{
    [Serializable]
    public class Data
    {
        public string Name { get; set; }
    }

    public class WhenWorkingWithTheFileSystemBackingStore : BehaviorDrivenDevelopmentCaseTemplate
    {
        protected FileSystemBackingStore Store;
        protected ICacheManager CacheManager;
        protected NameValueCollection ConfigAttributes;

        public override void Arrange()
        {
            base.Arrange();

            Directory.CreateDirectory("cache");
            CacheManager = CacheFactory.GetCacheManager("Cache Manager");

            ConfigAttributes = new NameValueCollection { { "path", "cache" } };
            Store = new FileSystemBackingStore(ConfigAttributes);
        }

        [TestCleanup]
        public override void CleanUp()
        {
            base.CleanUp();
            
            Store.Dispose();
            Directory.Delete("cache", true);
        }
    }

    [TestClass]
    public class AndConstructingWithANullPathAttribute : WhenWorkingWithTheFileSystemBackingStore
    {
        public override void Act()
        {
            base.Act();

            ConfigAttributes.Remove("path");
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ThenAnExceptionShouldBeThrown()
        {
            Store = new FileSystemBackingStore(ConfigAttributes);
            Assert.Fail();
        }
    }

    [TestClass]
    public class AndConstructingWithAnEmptyPathAttribute : WhenWorkingWithTheFileSystemBackingStore
    {
        public override void Act()
        {
            base.Act();

            ConfigAttributes["path"] = string.Empty;
        }

        [TestMethod]
        [ExpectedException(typeof(Exception))]
        public void ThenAnExceptionShouldBeThrown()
        {
            Store = new FileSystemBackingStore(ConfigAttributes);
            Assert.Fail();
        }
    }

    [TestClass]
    public class AndAddingANewItem : WhenWorkingWithTheFileSystemBackingStore
    {
        public override void Act()
        {
            base.Act();

            try
            {
                Store.Add(new CacheItem("item1", new Data { Name = "Data 1" }, CacheItemPriority.None, null));
            }
            catch { }

            CacheManager.Add("item1", new Data { Name = "Data 1" });
        }

        [TestMethod]
        public void ThenAnInfoFileAndDataFileShouldExist()
        {
            Assert.IsTrue(File.Exists(Path.Combine("cache", string.Format("{0}.cachedata", FileSystemBackingStore.CurrentStorageKey))));
            Assert.IsTrue(File.Exists(Path.Combine("cache", string.Format("{0}.cacheinfo", FileSystemBackingStore.CurrentStorageKey))));

            var data = (Data)CacheManager.GetData("item1");
            Assert.AreEqual("Data 1", data.Name);
        }
    }

    [TestClass]
    public class AndRemovingAnItem : WhenWorkingWithTheFileSystemBackingStore
    {
        int _activeItemCount;

        public override void Arrange()
        {
            base.Arrange();

            CacheManager.Add("item1", new Data { Name = "Data 1" });
            CacheManager.Add("item2", new Data { Name = "Data 2" });
        }

        public override void Act()
        {
            base.Act();

            CacheManager.Remove("item2");
            _activeItemCount = CacheManager.Count;
        }

        [TestMethod]
        public void ThenOnlyNonExpiredCacheItemsShouldBeCounted()
        {
            Assert.AreEqual(1, _activeItemCount);
        }
    }

    [TestClass]
    public class AndGettingCountOfNonExpiredItems : WhenWorkingWithTheFileSystemBackingStore
    {
        int _activeItemCount;

        public override void Arrange()
        {
            base.Arrange();

            CacheManager.Add("item1", new Data { Name = "Data 1" });
            CacheManager.Add("item2", new Data { Name = "Data 2" }, CacheItemPriority.None, null, new SlidingTime(TimeSpan.FromSeconds(1)));

            Thread.Sleep(TimeSpan.FromSeconds(1));
        }

        public override void Act()
        {
            base.Act();

            CacheManager.GetData("item2");
            _activeItemCount = CacheManager.Count;
        }

        [TestMethod]
        public void ThenOnlyNonExpiredCacheItemsShouldBeCounted()
        {
            Assert.AreEqual(1, _activeItemCount);
        }
    }

    [TestClass]
    public class AndFlushingTheCache : WhenWorkingWithTheFileSystemBackingStore
    {
        int _activeItemCount;

        public override void Arrange()
        {
            base.Arrange();

            CacheManager.Add("item1", new Data { Name = "Data 1" });
            CacheManager.Add("item2", new Data { Name = "Data 2" });
        }

        public override void Act()
        {
            base.Act();

            CacheManager.Flush();
            _activeItemCount = CacheManager.Count;
        }

        [TestMethod]
        public void ThenOnlyNonExpiredCacheItemsShouldBeCounted()
        {
            Assert.AreEqual(0, _activeItemCount);
        }
    }
}
