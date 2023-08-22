using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using VisualArt.Media.Dto;
using VisualArt.Media.IntegrationTest.Util;
using VisualArt.Media.Services;

namespace VisualArt.Media.IntegrationTest
{

    public class TestMediaStoreApi : IClassFixture<WebApplicationFactory<MediaApi.Program>>, IDisposable
    {
        //https://www.istockphoto.com/en/collaboration/boards/PRk4F0mZ_E6z_qcKsC2qSg
        private readonly List<(string name, long size)> _samples = new(){
            ("istockphoto-1.jpg",76786),
            ("istockphoto-2.jpg",44548),
            ("istockphoto-3.jpg",65028),
            ("istockphoto-4.jpg",41838),
            ("istockphoto-5.jpg",69847)
        };
        private readonly WebApplicationFactory<MediaApi.Program> _factory;

        public TestMediaStoreApi(WebApplicationFactory<MediaApi.Program> factory)
        {
            _factory = factory;
        }
        HttpClient CreateInjectedClient()
        {
            return _factory.CreateDefaultClient();
        }

        void DropStorage()
        {
            using (var scope = _factory.Services.CreateScope())
            {
                var storage = scope.ServiceProvider.GetRequiredService<IFileStorage>();
                (storage as FileStorageService)?.DropStorage();
            }
        }

        [Theory]
        [InlineData("/api/media/metadata")]
        public async Task Get_EndpointsReturnSuccessAndCorrectContentType(string url)
        {
            // Arrange
            var client = CreateInjectedClient();
            // Act
            var response = await client.GetAsync(url);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
        }

        [Theory]
        [InlineData("/api/media/metadata/<")]
        [InlineData("/api/media/metadata/>")]
        [InlineData("/api/media/metadata/|")]
        [InlineData("/api/media/metadata/:")]
        public async Task Get_EndpointsReturnBadRequestWhenCalledWithInvalidPath(string url)
        {
            // Arrange
            var client = CreateInjectedClient();
            // Act
            var response = await client.GetAsync(url);

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GivenOneFile_SavesOneFile()
        {
            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            var file = FileUtil.OpenRead($"StoreFiles/{_samples[0].name}");
            content.Add(new StreamContent(file), "files", _samples[0].name);

            // Act
            var response = await client.PostAsync("/api/media", content);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
            var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>() ?? throw new Exception("Null response");
            Assert.Single(files);
            Assert.Equal(_samples[0].name, files[0].Name);
            Assert.Equal(_samples[0].size, files[0].Length);
        }

        [Fact]
        public async Task TryingToSaveFile20LevelsDeep_ReturnsBadRequest()
        {
            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            var file = FileUtil.OpenRead($"StoreFiles/{_samples[0].name}");
            content.Add(new StreamContent(file), "files", _samples[0].name);

            // Act
            var response = await client.PostAsync("/api/media/1/2/3/4/5/6/7/8/9/10/11/12/13/14/15/16/17/18/19/20", content);

            // Assert
            Assert.Equal( System.Net.HttpStatusCode.BadRequest, response.StatusCode );
        }

        [Fact]
        public async Task TryingToListFiles20LevelsDeep_ReturnsBadRequest()
        {
            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            var file = FileUtil.OpenRead($"StoreFiles/{_samples[0].name}");
            content.Add(new StreamContent(file), "files", _samples[0].name);

            // Act
            var response = await client.GetAsync("/api/media/metadata/1/2/3/4/5/6/7/8/9/10/11/12/13/14/15/16/17/18/19/20");

            // Assert
            Assert.Equal(System.Net.HttpStatusCode.BadRequest, response.StatusCode);
        }

        [Fact]
        public async Task GivenFiveFiles_SavesFiveFiles()
        {
            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            foreach (var file in _samples)
            {
                content.Add(new StreamContent(FileUtil.OpenRead($"StoreFiles/{file.name}")), "files", file.name);
            }

            // Act
            var response = await client.PostAsync("/api/media", content);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
            var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>() ?? throw new Exception("Null response");
            Assert.Equal(_samples.Count, files?.Count);

            foreach (var file in files!)
            {
                Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
            }
        }

        [Fact]
        public async Task GivenFiveFiles_SavesFiveFiles_ListMetadataReturnsAllFiles()
        {
            //https://www.istockphoto.com/en/collaboration/boards/PRk4F0mZ_E6z_qcKsC2qSg

            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            foreach (var file in _samples)
            {
                content.Add(new StreamContent(FileUtil.OpenRead($"StoreFiles/{file.name}")), "files", file.name);
            }

            // Act
            var response = await client.PostAsync("/api/media", content);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
            var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>();
            Assert.Equal(_samples.Count, files?.Count);

            foreach (var file in files!)
            {
                Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
            }

            var fileMetadata = await client.GetFromJsonAsync<List<FileMetadata>>("/api/media/metadata");
            Assert.Equal(_samples.Count, fileMetadata?.Count);
            foreach (var file in fileMetadata!)
            {
                Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
            }
        }

        [Fact]
        public async Task SavesFiveFiles_ThreeLevelsDeep_ListMetadataReturnsAllFiles()
        {
            //https://www.istockphoto.com/en/collaboration/boards/PRk4F0mZ_E6z_qcKsC2qSg

            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            foreach (var file in _samples)
            {
                content.Add(new StreamContent(FileUtil.OpenRead($"StoreFiles/{file.name}")), "files", file.name);
            }

            // Act
            var response = await client.PostAsync("/api/media/level1/level2/level3", content);

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
            var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>();
            Assert.Equal(_samples.Count, files?.Count);

            foreach (var file in files!)
            {
                Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
            }

            var fileMetadata = await client.GetFromJsonAsync<List<FileMetadata>>("/api/media/metadata/level1/level2/level3");
            Assert.Equal(_samples.Count, fileMetadata?.Count);
            foreach (var file in fileMetadata!)
            {
                Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
            }
        }

        [Fact]
        public async Task SavesFiveFiles_AtAllLevels_ThreeLevelsDeep_ListMetadataReturnsAllFiles()
        {
            //https://www.istockphoto.com/en/collaboration/boards/PRk4F0mZ_E6z_qcKsC2qSg

            // Arrange
            var client = CreateInjectedClient();
            var content = new MultipartFormDataContent();
            foreach (var file in _samples)
            {
                content.Add(new StreamContent(FileUtil.OpenRead($"StoreFiles/{file.name}")), "files", file.name);
            }

            var folders = new List<string> { "", "/level1", "/level1/level2", "/level1/level2/level3" };
            // Act
            foreach (var folder in folders)
            {
                var response = await client.PostAsync($"/api/media{folder}", content);

                // Assert
                response.EnsureSuccessStatusCode();
                Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
                var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>();
                Assert.Equal(_samples.Count, files?.Count);

                foreach (var file in files!)
                {
                    Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
                }

                var fileMetadata = await client.GetFromJsonAsync<List<FileMetadata>>($"/api/media/metadata{folder}");
                var fileCount = 0;
                foreach (var file in fileMetadata!)
                {
                    if (file.Folder == false)
                    {
                        fileCount++;
                        Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
                    }
                }
                Assert.Equal(_samples.Count, fileCount);
            }
        }
        [Fact]
        public async Task GivenOneFile_OverMaxSize_IgnoresSaving()
        {
            // Arrange
            var dummyFile = Path.Combine(Path.GetTempPath(), "large-dummy-file.txt");
            try
            {
                using (var client = _factory.CreateClient())
                {
                    var content = new MultipartFormDataContent();

                    using (var fileStream = new FileStream(dummyFile, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fileStream.SetLength(501 * 1024 * 1024);
                    }
                    using (var file = File.OpenRead(dummyFile))
                    {
                        content.Add(new StreamContent(file), "files", "large-dummy-file.txt");

                        // Act
                        var response = await client.PostAsync("/api/media", content);

                        // Assert
                        response.EnsureSuccessStatusCode();
                        Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
                        var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>();
                        Assert.Equal(0, files?.Count);
                    }
                }
            }
            finally
            {
                File.Delete(dummyFile);
            }
        }

        [Fact]
        public async Task GivenOneFileOverMaxSize_AndFiveFilesWithinSize_IgnoresSavingOne_AndSavesTheRest()
        {
            // Arrange
            var client = _factory.CreateClient();
            var content = new MultipartFormDataContent();
            var dummyFile = Path.Combine(Path.GetTempPath(), "large-dummy-file.txt");
            try
            {
                using (var fileStream = new FileStream(dummyFile, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    fileStream.SetLength(501 * 1024 * 1024);
                }
                using (var f = File.OpenRead(dummyFile))
                {
                    content.Add(new StreamContent(f), "files", "large-dummy-file.txt");

                    foreach (var file in _samples)
                    {
                        content.Add(new StreamContent(FileUtil.OpenRead($"StoreFiles/{file.name}")), "files", file.name);
                    }

                    // Act
                    var response = await client.PostAsync("/api/media", content);

                    // Assert
                    response.EnsureSuccessStatusCode();
                    Assert.Equal("application/json; charset=utf-8", response!.Content!.Headers!.ContentType!.ToString());
                    var files = await response.Content.ReadFromJsonAsync<List<FileMetadata>>();
                    Assert.Equal(_samples.Count, files?.Count);
                    foreach (var file in files!)
                    {
                        Assert.NotEqual(-1, _samples.IndexOf((file.Name, file.Length)));
                    }
                }
            }
            finally
            {
                File.Delete(dummyFile);
            }
        }

        public void Dispose()
        {
            DropStorage();
        }
    }
}