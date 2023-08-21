using VisualArt.Media.Dto;

namespace VisualArt.Media.Services
{
    public interface IFileStorage
    {
        public long MaxFileSize { get; }
        Task<FileMetadata> SaveFileAsync(string fileName, Stream stream);
        IEnumerable<FileMetadata> ListFiles();
    }
}
