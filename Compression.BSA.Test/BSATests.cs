﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Wabbajack.Common;
using Wabbajack.Lib.Downloaders;
using Wabbajack.Lib.NexusApi;
using Wabbajack.VirtualFileSystem;
using Xunit;
using Xunit.Abstractions;

namespace Compression.BSA.Test
{
    public class BSATests : IAsyncLifetime
    {
        private static AbsolutePath _stagingFolder = ((RelativePath)"NexusDownloads").RelativeToEntryPoint();
        private static AbsolutePath _bsaFolder = ((RelativePath)"BSAs").RelativeToEntryPoint();
        private static AbsolutePath _testDir = ((RelativePath)"BSA Test Dir").RelativeToEntryPoint();
        private static AbsolutePath _tempDir = ((RelativePath)"BSA Temp Dir").RelativeToEntryPoint();
        private IDisposable _unsub;

        public ITestOutputHelper TestContext { get; }

        private static WorkQueue Queue { get; set; }

        public BSATests(ITestOutputHelper helper)
        {
            TestContext = helper;

        }
        
        
        public async Task InitializeAsync()
        {
            Queue = new WorkQueue();
            _unsub = Utils.LogMessages.Subscribe(f => TestContext.WriteLine(f.ShortDescription));
            _stagingFolder.CreateDirectory();
            await _bsaFolder.DeleteDirectory();
            _bsaFolder.CreateDirectory();
        }

        public async Task DisposeAsync()
        {
            await _bsaFolder.DeleteDirectory();
            Queue.Dispose();
            _unsub.Dispose();
        }

        private static async Task<AbsolutePath> DownloadMod(Game game, int mod)
        {
            using var client = await NexusApiClient.Get();
            var results = await client.GetModFiles(game, mod);
            var file = results.files.FirstOrDefault(f => f.is_primary) ??
                       results.files.OrderByDescending(f => f.uploaded_timestamp).First();
            var src = _stagingFolder.Combine(file.file_name);

            if (src.Exists) return src;

            var state = new NexusDownloader.State
            {
                ModID = mod,
                Game = game,
                FileID = file.file_id
            };
            await state.Download(src);
            return src;
        }

        [Theory]
        [InlineData(Game.SkyrimSpecialEdition, 12604)] // SkyUI
        [InlineData(Game.Skyrim, 3863)] // SkyUI
        [InlineData(Game.Skyrim, 51473)] // INeed
        [InlineData(Game.Fallout4, 22223)] // 10mm SMG
        [InlineData(Game.Fallout4, 4472)] // True Storms
        [InlineData(Game.Morrowind, 44537)]
        public async Task BSACompressionRecompression(Game game, int modid)
        {
            var filename = await DownloadMod(game, modid);
            var folder = _bsaFolder.Combine(game.ToString(), modid.ToString());
            await folder.DeleteDirectory();
            folder.CreateDirectory();
            await using var files = await FileExtractor.ExtractAll(Queue, filename);
            await files.MoveAllTo(folder);

            foreach (var bsa in folder.EnumerateFiles().Where(f => Consts.SupportedBSAs.Contains(f.Extension)))
            {
                TestContext.WriteLine($"From {bsa}");
                TestContext.WriteLine("Cleaning Output Dir");
                await _tempDir.DeleteDirectory();
                _tempDir.CreateDirectory();

                TestContext.WriteLine($"Reading {bsa}");
                var tempFile = ((RelativePath)"tmp.bsa").RelativeToEntryPoint();
                var size = bsa.Size;
                
                await using var a = BSADispatch.OpenRead(bsa);
                await a.Files.PMap(Queue, file =>
                {
                    var absName = _tempDir.Combine(file.Path);
                    ViaJson(file.State);

                    absName.Parent.CreateDirectory();
                    using (var fs = absName.Create())
                    {
                        file.CopyDataTo(fs);
                    }

                    Assert.Equal(file.Size, absName.Size);
                });

                TestContext.WriteLine($"Building {bsa}");

                await using (var w = ViaJson(a.State).MakeBuilder(size))
                {
                    var streams = await a.Files.PMap(Queue, async file =>
                    {
                        var absPath = _tempDir.Combine(file.Path);
                        var str = absPath.OpenRead();
                        await w.AddFile(ViaJson(file.State), str);
                        return str;
                    });
                    await w.Build(tempFile);
                    streams.Do(s => s.Dispose());
                }

                TestContext.WriteLine($"Verifying {bsa}");
                await using var b = BSADispatch.OpenRead(tempFile);
                TestContext.WriteLine($"Performing A/B tests on {bsa}");
                Assert.Equal(a.State.ToJson(), b.State.ToJson());

                // Check same number of files
                Assert.Equal(a.Files.Count(), b.Files.Count());

                await a.Files.Zip(b.Files, (ai, bi) => (ai, bi))
                    .PMap(Queue, pair =>
                    {
                        Assert.Equal(pair.ai.State.ToJson(), pair.bi.State.ToJson());
                        //Console.WriteLine($"   - {pair.ai.Path}");
                        Assert.Equal(pair.ai.Path, pair.bi.Path);
                        //Equal(pair.ai.Compressed, pair.bi.Compressed);
                        Assert.Equal(pair.ai.Size, pair.bi.Size);
                        Assert.Equal(GetData(pair.ai), GetData(pair.bi));
                    });
            }
        }

        private static byte[] GetData(IFile pairAi)
        {
            using var ms = new MemoryStream();
            pairAi.CopyDataTo(ms);
            return ms.ToArray();
        }

        private static T ViaJson<T>(T i)
        {
            return i.ToJson().FromJsonString<T>();
        }

    }
}
