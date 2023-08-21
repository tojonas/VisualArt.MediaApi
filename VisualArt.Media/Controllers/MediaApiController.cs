using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
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

        public IEnumerable<FileMetadata> ListFiles(uint? start, uint? count)
        {
            return _fileStorage.ListFiles().Skip((int)(start ?? 0)).Take((int)(count ?? int.MaxValue));
        }

        public async IAsyncEnumerable<FileMetadata> UploadFiles( IFormFileCollection files )
        {
            foreach (var file in files)
            {
                if (file.Length > _fileStorage.MaxFileSize)
                {
                    _logger.LogWarning($"File [{file.FileName}] is too large: {file.Length} > {_fileStorage.MaxFileSize}");
                    continue;
                }
                using (var stream = file.OpenReadStream())
                {
                    var result = await _fileStorage.SaveFileAsync(file.FileName, stream);
                    if( result.Length != -1)
                    {
                        yield return result;
                    }
                }
            }
        }
    }
}