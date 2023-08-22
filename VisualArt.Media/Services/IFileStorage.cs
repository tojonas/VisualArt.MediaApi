using VisualArt.Media.Dto;

namespace VisualArt.Media.Services
{
    public interface IFileStorage
    {
        long MaxFileSize { get; }
        Task<FileMetadata> SaveFileAsync(string path, string fileName, Stream stream);
        IEnumerable<FileMetadata> ListFiles(string path);
    }
}
