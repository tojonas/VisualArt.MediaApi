using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VisualArt.Media.Dto;
using VisualArt.Media.Services;

namespace VisualArt.Media.IntegrationTest.Util
{
    public class NonPersistentFileStorageService : IFileStorage, IDisposable
    {
        FileStorageService _service;
        public NonPersistentFileStorageService(FileStorageService service)
        {
            _service = service;
        }

        public long MaxFileSize => _service.MaxFileSize;

        public void Dispose()
        {
        }

        public IEnumerable<FileMetadata> ListFiles(string path)
        {
            return _service.ListFiles(path);
        }

        public Task<FileMetadata> SaveFileAsync(string path, string fileName, Stream stream)
        {
            return _service.SaveFileAsync(path, fileName, stream);
        }
    }
}
