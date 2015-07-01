using System;
using System.IO;
using System.Collections;
using System.Collections.Specialized;

using Microsoft.Practices.EnterpriseLibrary.Common.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Caching;
using Microsoft.Practices.EnterpriseLibrary.Caching.Expirations;
using Microsoft.Practices.EnterpriseLibrary.Caching.Configuration;
using Microsoft.Practices.EnterpriseLibrary.Caching.BackingStoreImplementations;

namespace Psns.Common.Persistence.Caching
{
    /// <summary>
    /// Persists cache data to the filesystem as specified in *.config, backingStore, path attribute.
    /// </summary>
    [ConfigurationElementType(typeof(CustomCacheStorageData))]
    public class FileSystemBackingStore : BaseBackingStore
    {
        const string _filePathConfigurationName = "path";
        const string _dataExtension = ".cachedata";
        const string _infoExtension = ".cacheinfo";
        string _filePath;

        public FileSystemBackingStore(NameValueCollection configAttributes)
        {
            _filePath = string.Empty;

            var pathAttribute = configAttributes[_filePathConfigurationName];
            if(pathAttribute != null && pathAttribute != string.Empty)
                _filePath = pathAttribute;
            else
                throw new Exception("Error in application configuration: '" + _filePathConfigurationName + "' attribute not found");
        }

        /// <summary>
        /// This key is used to name the cache info and data files
        /// </summary>
        public static int? CurrentStorageKey { get; private set; }

        protected override void AddNewItem(int storageKey, CacheItem newItem)
        {
            CurrentStorageKey = storageKey;

            string[] infoData = new string[3];
            infoData[0] = newItem.Key;
            infoData[1] = newItem.LastAccessedTime.ToString();

            var expirations = newItem.GetExpirations();
            if(expirations.Length > 0)
            {
                var slidingDuration = (SlidingTime)expirations.GetValue(0);
                infoData[2] = slidingDuration.ItemSlidingExpiration.ToString();
            }

            var infoFile = Path.Combine(_filePath, string.Concat(storageKey.ToString(), _infoExtension));
            try
            {
                if(File.Exists(infoFile))
                    File.Delete(infoFile);

                File.WriteAllLines(infoFile, infoData);
            }
            catch
            {
                throw new FileNotFoundException("Cannot create cache info file", infoFile);
            }

            var itemBytes = SerializationUtility.ToBytes(newItem.Value);
            var dataFile = Path.Combine(_filePath, string.Concat(storageKey.ToString(), _dataExtension));
            try
            {
                if(File.Exists(dataFile))
                    File.Delete(dataFile);

                File.WriteAllBytes(dataFile, itemBytes);
            }
            catch
            {
                throw new FileNotFoundException("Cannot create cache data file", dataFile);
            }
        }

        /// <summary>
        /// The count of currently active cache items
        /// </summary>
        public override int Count
        {
            get
            {
                var cacheFiles = Directory.GetFiles(_filePath,
                    string.Concat("*", _dataExtension),
                    SearchOption.TopDirectoryOnly);

                return cacheFiles.Length;
            }
        }

        /// <summary>
        /// Clear all items from the cache
        /// </summary>
        public override void Flush()
        {
            String searchString = String.Concat("*", _dataExtension);
            String[] cacheFiles = Directory.GetFiles(_filePath, searchString, SearchOption.TopDirectoryOnly);

            foreach(String cacheFile in cacheFiles)
            {
                String dataFile = Path.Combine(_filePath, cacheFile);
                String infoName = String.Concat(Path.GetFileNameWithoutExtension(cacheFile), _infoExtension);
                String infoFile = Path.Combine(_filePath, infoName);

                try
                {
                    File.Delete(dataFile);
                }
                catch { }

                try
                {
                    File.Delete(infoFile);
                }
                catch { }
            }
        }

        protected override Hashtable LoadDataFromStore()
        {
            Hashtable cacheItems = new Hashtable();
            String searchString = String.Concat("*", _dataExtension);
            String[] cacheFiles = Directory.GetFiles(_filePath, searchString, SearchOption.TopDirectoryOnly);

            foreach(String cacheFile in cacheFiles)
            {
                String infoName = String.Concat(Path.GetFileNameWithoutExtension(cacheFile), _infoExtension);
                String infoPath = Path.Combine(_filePath, infoName);

                String[] infoData = File.ReadAllLines(infoPath);
                String itemKey = infoData[0];
                DateTime lastAccessed = DateTime.Parse(infoData[1]);

                TimeSpan slidingDuration;
                SlidingTime slidingTime = null;
                if(TimeSpan.TryParse(infoData[2], out slidingDuration))
                    slidingTime = new SlidingTime(slidingDuration);

                Byte[] itemBytes = File.ReadAllBytes(cacheFile);
                Object itemValue = SerializationUtility.ToObject(itemBytes);

                CacheItem item;
                if(slidingTime != null)
                    item = new CacheItem(lastAccessed, itemKey, itemValue, CacheItemPriority.Normal, null, slidingTime);
                else
                    item = new CacheItem(lastAccessed, itemKey, itemValue, CacheItemPriority.Normal, null);

                cacheItems.Add(itemKey, item);
            }

            return cacheItems;
        }

        protected override void Remove(int storageKey)
        {
            var dataFile = Path.Combine(_filePath, string.Concat(storageKey.ToString(), _dataExtension));
            var infoFile = Path.Combine(_filePath, string.Concat(storageKey.ToString(), _infoExtension));

            if(File.Exists(dataFile))
            {
                File.Delete(dataFile);
                try
                {
                    File.Delete(infoFile);
                }
                catch { }
            }
            else
                throw new FileNotFoundException("Cannot remove cached item", dataFile);
        }

        protected override void RemoveOldItem(int storageKey)
        {
            var dataFile = Path.Combine(_filePath, string.Concat(storageKey.ToString(), _dataExtension));
            var infoFile = Path.Combine(_filePath, string.Concat(storageKey.ToString(), _infoExtension));

            try
            {
                File.Delete(dataFile);
            }
            catch { }

            try
            {
                File.Delete(infoFile);
            }
            catch { }
        }

        protected override void UpdateLastAccessedTime(int storageKey, DateTime timestamp)
        {
            string infoFile = Path.Combine(_filePath, String.Concat(storageKey.ToString(), _infoExtension));

            if(File.Exists(infoFile))
            {
                string[] infoData = File.ReadAllLines(infoFile);
                infoData[1] = timestamp.ToString();

                File.Delete(infoFile);
                File.WriteAllLines(infoFile, infoData);
            }
            else
                throw new FileNotFoundException("Cannot find cache info file", infoFile);
        }
    }
}
