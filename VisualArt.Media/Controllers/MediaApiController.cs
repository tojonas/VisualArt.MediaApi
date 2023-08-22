using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using VisualArt.Media.Dto;
using VisualArt.Media.Services;

namespace VisualArt.Media.Controllers
{
    public class MediaApiController
    {
        private readonly ILogger _logger;
        private readonly IFileStorage _fileStorage;

        public MediaApiController(ILogger<MediaApiController> logger, IFileStorage fileStorage)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _fileStorage = fileStorage ?? throw new ArgumentNullException(nameof(fileStorage));
        }

        public IEnumerable<FileMetadata> ListFiles(string path, uint start, uint count)
        {
            var decodedPath = WebUtility.UrlDecode(path);
            return _fileStorage.ListFiles(decodedPath).Skip((int)start).Take((int)count);
        }

        public async IAsyncEnumerable<FileMetadata> UploadFiles(string path, IFormFileCollection files)
        {
            var decodedPath = WebUtility.UrlDecode(path);
            foreach (var file in files)
            {
                if (file.Length > _fileStorage.MaxFileSize)
                {
                    _logger.LogWarning($"File [{file.FileName}] is too large: {file.Length} > {_fileStorage.MaxFileSize}");
                    continue;
                }
                using (var stream = file.OpenReadStream())
                {
                    var result = await _fileStorage.SaveFileAsync(decodedPath, file.FileName, stream);

                    if (result.Length != -1)
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}