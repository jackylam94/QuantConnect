/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.IO;
using System.Collections.Concurrent;
using System.Collections.Generic;
using QuantConnect.Logging;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Ionic.Zip;
using Ionic.Zlib;
using QuantConnect.Interfaces;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds
{
    /// <summary>
    /// File provider implements optimized zip archives caching facility. Cache is thread safe.
    /// </summary>
    public class ZipDataCacheProvider : IDataCacheProvider
    {
        private const int CacheSeconds = 10;

        // ZipArchive cache used by the class
        private static readonly ConcurrentDictionary<string, CachedZipFile> _zipFileCache = new ConcurrentDictionary<string, CachedZipFile>();
        private static readonly ConcurrentDictionary<string, object> _zipFileWriteLocks = new ConcurrentDictionary<string, object>();
        
        private readonly IDataProvider _dataProvider;

        /// <summary>
        /// Property indicating the data is temporary in nature and should not be cached.
        /// </summary>
        public bool IsDataEphemeral { get; }

        /// <summary>
        /// Constructor that sets the <see cref="IDataProvider"/> used to retrieve data
        /// </summary>
        public ZipDataCacheProvider(IDataProvider dataProvider, bool isDataEphemeral = true)
        {
            IsDataEphemeral = isDataEphemeral;
            _dataProvider = dataProvider;
        }

        /// <summary>
        /// Does not attempt to retrieve any data
        /// </summary>
        public Stream Fetch(string key)
        {
            string entryName = null; // default to all entries
            var filename = key;
            var hashIndex = key.LastIndexOf("#", StringComparison.Ordinal);
            if (hashIndex != -1)
            {
                entryName = key.Substring(hashIndex + 1);
                filename = key.Substring(0, hashIndex);
            }

            // handles zip files
            if (!filename.EndsWith(".zip"))
            {
                // handles text files
                return _dataProvider.Fetch(filename);
            }

            try
            {
                if (!_zipFileCache.TryGetValue(filename, out var existingEntry))
                {
                    return CacheAndCreateStream(filename, entryName);
                }

                byte[] entry = null;
                if (entryName == null)
                {
                    return CreateStream(existingEntry, entry, true);
                }
                
                existingEntry.EntryCache.TryGetValue(entryName, out entry);
                return CreateStream(existingEntry, entry);
            }
            catch (Exception err)
            {
                Log.Error(err, "Inner try/catch");
                return null;
            }
        }

        /// <summary>
        /// Store the data in the cache. Not implemented in this instance of the IDataCacheProvider
        /// </summary>
        /// <param name="key">The source of the data, used as a key to retrieve data in the cache</param>
        /// <param name="data">The data as a byte array</param>
        public void Store(string key, byte[] data)
        {
            //
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        /// <filterpriority>2</filterpriority>
        public void Dispose()
        {
        }

        private Stream CacheAndCreateStream(string filename, string entryName)
        {
            try
            {
                if (!_zipFileWriteLocks.TryGetValue(filename, out var readWriteLock))
                {
                    readWriteLock = new object();
                    if (!_zipFileWriteLocks.TryAdd(filename, readWriteLock))
                    {
                        readWriteLock = _zipFileWriteLocks[filename];
                    }
                }

                lock (readWriteLock)
                {
                    byte[] entry = null;
                    if (_zipFileCache.TryGetValue(filename, out var existingEntry))
                    {
                        if (entryName == null)
                        {
                            return CreateStream(existingEntry, entry, true);
                        }

                        existingEntry.EntryCache.TryGetValue(entryName, out entry);
                        return CreateStream(existingEntry, entry);
                    }

                    var dataStream = _dataProvider.Fetch(filename);
                    if (dataStream == null)
                    {
                        return null;
                    }

                    var newItem = new CachedZipFile(dataStream, filename);
                    if (entryName != null)
                    {
                        newItem.EntryCache.TryGetValue(entryName, out entry);
                    }

                    _zipFileCache.TryAdd(filename, newItem);
                    return CreateStream(newItem, entry, entryName == null);
                }
            }
            catch (Exception exception)
            {
                if (exception is ZipException || exception is ZlibException)
                {
                    Log.Error("ZipDataCacheProvider.Fetch(): Corrupt zip file/entry: " + filename + "#" +
                        entryName + " Error: " + exception);
                }
                else throw;
            }
            
            return null;
        }

        /// <summary>
        /// Create a stream of a specific ZipEntry
        /// </summary>
        /// <param name="zipFile">The zipFile containing the zipEntry</param>
        /// <param name="entryName">The name of the entry</param>
        /// <param name="fileName">The name of the zip file on disk</param>
        /// <returns>A <see cref="Stream"/> of the appropriate zip entry</returns>
        private Stream CreateStream(CachedZipFile zipFile, byte[] entry, bool readFirstValue = false)
        {
            if (readFirstValue)
            {
                entry = zipFile.EntryCache.FirstOrDefault().Value;
            }
            
            if (entry != null)
            {
                var ms = new MemoryStream(entry, false);
                ms.Seek(0, SeekOrigin.Begin);

                return ms;
            }

            return null;
        }


        /// <summary>
        /// Type for storing zipfile in cache
        /// </summary>
        private class CachedZipFile
        {
            private readonly Timer _cacheCleaner;
            private readonly DateTime _dateCached;
            private readonly string _key;

            /// <summary>
            /// Contains all entries of the zip file by filename
            /// </summary>
            public readonly Dictionary<string, byte[]> EntryCache = new Dictionary<string, byte[]>(StringComparer.OrdinalIgnoreCase);

            /// <summary>
            /// Initializes a new instance of the <see cref="CachedZipFile"/>
            /// </summary>
            /// <param name="dataStream">Stream containing the zip file</param>
            /// <param name="utcNow">Current utc time</param>
            public CachedZipFile(Stream dataStream, string key)
            {
                _key = key;
                _cacheCleaner = new Timer(state => CleanCache(), null, TimeSpan.FromSeconds(CacheSeconds), Timeout.InfiniteTimeSpan);
                
                var zipFile = ZipFile.Read(dataStream);
                
                foreach (var entry in zipFile.Entries)
                {
                    var buf = new byte[entry.UncompressedSize];
                    var ms = new MemoryStream(buf);
                    entry.Extract(ms);
                    EntryCache[entry.FileName] = buf;
                }
                
                _dateCached = DateTime.UtcNow;
                zipFile.Dispose();
                dataStream.Dispose();
            }

            /// <summary>
            /// Method used to check if this object was created before a certain time
            /// </summary>
            /// <param name="date">DateTime which is compared to the DateTime this object was created</param>
            /// <returns>Bool indicating whether this object is older than the specified time</returns>
            public bool Uncache(DateTime date)
            {
                return (date - _dateCached).TotalSeconds >= CacheSeconds;
            }

            /// <summary>
            /// Remove items in the cache that are older than the cutoff date
            /// </summary>
            private void CleanCache()
            {
                try
                {
                    // clean all items that that are older than CacheSeconds than the current date

                    if (Uncache(DateTime.UtcNow))
                    {
                        // only clear items if they are not being used
                        _zipFileCache.TryRemove(_key, out _);
                    }
                }
                finally
                {
                    try
                    {
                        _cacheCleaner.Change(TimeSpan.FromSeconds(CacheSeconds), Timeout.InfiniteTimeSpan);
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
            }
        }
    }
}
