﻿// Copyright 2014 The Rector & Visitors of the University of Virginia
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using NUnit.Framework;
using Sensus.DataStores;
using Sensus.DataStores.Local;
using Sensus.DataStores.Remote;
using Sensus.Probes.Device;
using Sensus.Probes.Location;
using Sensus.Probes.Movement;
using Sensus.Tests.Classes;

namespace Sensus.Tests.DataStores.Local
{
    [TestFixture]
    public class FileLocalDataStoreTests
    {
        #region compression should not change the content of the files
        [Test]
        public void UncompressedBytesEqualUncompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes1 = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();
            byte[] uncompressedBytes2 = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();

            CollectionAssert.AreEqual(uncompressedBytes1, uncompressedBytes2);
        }

        [Test]
        public void UncompressedBytesEqualFastestDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(GetLocalDataStoreBytes(data, CompressionLevel.Fastest));

            CollectionAssert.AreEqual(uncompressedBytes, decompressedBytes);
        }

        [Test]
        public void UncompressedBytesEqualOptimalDecompressedBytesTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] uncompressedBytes = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();

            Compressor compressor = new Compressor(Compressor.CompressionMethod.GZip);
            byte[] decompressedBytes = compressor.Decompress(GetLocalDataStoreBytes(data, CompressionLevel.Optimal));

            CollectionAssert.AreEqual(uncompressedBytes, decompressedBytes);
        }
        #endregion

        #region the file sizes should increase without closing the streams. we need this because we track the file sizes to open new files and force remote writes.
        [Test]
        public void UncompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            string path = WriteLocalDataStore(data, CompressionLevel.NoCompression, fileLocalDataStore =>
            {
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.GreaterOrEqual(newSizeMB, currentSizeMB);
                currentSizeMB = newSizeMB;
            });

            Assert.Greater(currentSizeMB, 0);
        }

        [Test]
        public void FastestCompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            string path = WriteLocalDataStore(data, CompressionLevel.Fastest, fileLocalDataStore =>
            {
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.GreaterOrEqual(newSizeMB, currentSizeMB);
                currentSizeMB = newSizeMB;
            });

            Assert.Greater(currentSizeMB, 0);
        }

        [Test]
        public void OptimalCompressedFileSizeIncreasesWithoutClosingTest()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            double currentSizeMB = 0;

            string path = WriteLocalDataStore(data, CompressionLevel.Optimal, fileLocalDataStore =>
            {
                double newSizeMB = SensusServiceHelper.GetFileSizeMB(fileLocalDataStore.Path);
                Assert.GreaterOrEqual(newSizeMB, currentSizeMB);
                currentSizeMB = newSizeMB;
            });

            Assert.Greater(currentSizeMB, 0);
        }
        #endregion

        #region compression should reduce file size
        [Test]
        public void UncompressedBytesGreaterThanFastestCompressedBytes()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = GetLocalDataStoreBytes(data, CompressionLevel.NoCompression).ToArray();
            byte[] bytes2 = GetLocalDataStoreBytes(data, CompressionLevel.Fastest).ToArray();

            Assert.Greater(bytes1.Length, bytes2.Length);
        }

        [Test]
        public void FastestCompressedBytesGreaterOrEqualOptimalCompressedBytes()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            byte[] bytes1 = GetLocalDataStoreBytes(data, CompressionLevel.Fastest).ToArray();
            byte[] bytes2 = GetLocalDataStoreBytes(data, CompressionLevel.Optimal).ToArray();

            Assert.GreaterOrEqual(bytes1.Length, bytes2.Length);
        }
        #endregion

        #region data store should create/promote files
        [Test]
        public void UncompressedRemoteWriteClearsFiles()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            WriteLocalDataStore(data, CompressionLevel.NoCompression, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            localDataStore.WriteToRemoteAsync(CancellationToken.None).Wait();

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Test]
        public void FastestCompressionRemoteWriteClearsFiles()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            WriteLocalDataStore(data, CompressionLevel.Fastest, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            localDataStore.WriteToRemoteAsync(CancellationToken.None).Wait();

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }

        [Test]
        public void OptimalCompressionRemoteWriteClearsFiles()
        {
            InitServiceHelper();
            List<Datum> data = GenerateData();

            FileLocalDataStore localDataStore = null;
            WriteLocalDataStore(data, CompressionLevel.Optimal, (obj) =>
            {
                localDataStore = obj;
            });

            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);

            localDataStore.WriteToRemoteAsync(CancellationToken.None).Wait();

            // there should still be 1 file (the new open file)
            Assert.AreEqual(Directory.GetFiles(localDataStore.StorageDirectory).Length, 1);
        }
        #endregion

        #region helper functions
        private void InitServiceHelper()
        {
            SensusServiceHelper.ClearSingleton();
            TestSensusServiceHelper serviceHelper = new TestSensusServiceHelper();
            SensusServiceHelper.Initialize(() => serviceHelper);
        }

        private List<Datum> GenerateData()
        {
            List<Datum> data = new List<Datum>();
            Random r = new Random();
            for (int i = 0; i < 1000; ++i)
            {
                Datum d;
                int type = r.Next(0, 3);
                if (type == 0)
                {
                    d = new AccelerometerDatum(DateTimeOffset.UtcNow, r.NextDouble(), r.NextDouble(), r.NextDouble());
                }
                else if (type == 1)
                {
                    d = new LocationDatum(DateTimeOffset.UtcNow, r.NextDouble(), r.NextDouble(), r.NextDouble());
                }
                else
                {
                    d = new BatteryDatum(DateTimeOffset.UtcNow, r.NextDouble());
                }

                data.Add(d);
            }

            return data;
        }

        private MemoryStream GetLocalDataStoreBytes(List<Datum> data, CompressionLevel compressionLevel)
        {
            byte[] bytes = File.ReadAllBytes(WriteLocalDataStore(data, compressionLevel));
            return new MemoryStream(bytes);
        }

        private string WriteLocalDataStore(List<Datum> data, CompressionLevel compressionLevel, Action<FileLocalDataStore> postWriteAction = null)
        {
            Protocol protocol = CreateProtocol(compressionLevel);
            FileLocalDataStore localDataStore = protocol.LocalDataStore as FileLocalDataStore;
            protocol.LocalDataStore.Start();
            WriteData(data, localDataStore, postWriteAction);
            string path = localDataStore.Path;
            localDataStore.Stop();
            return path;
        }

        private Protocol CreateProtocol(CompressionLevel compressionLevel)
        {
            FileLocalDataStore localDataStore = new FileLocalDataStore()
            {
                CompressionLevel = compressionLevel
            };

            ConsoleRemoteDataStore remoteDataStore = new ConsoleRemoteDataStore()
            {
                WriteDelayMS = 1000000
            };

            Protocol protocol = new Protocol("test")
            {
                Id = Guid.NewGuid().ToString(),
                LocalDataStore = localDataStore,
                RemoteDataStore = remoteDataStore
            };

            return protocol;
        }

        private void WriteData(List<Datum> data, FileLocalDataStore localDataStore, Action<FileLocalDataStore> postWriteAction = null)
        {
            for (int i = 0; i < data.Count; ++i)
            {
                localDataStore.WriteDatumAsync(data[i], CancellationToken.None).Wait();
                postWriteAction?.Invoke(localDataStore);
            }
        }
        #endregion
    }
}