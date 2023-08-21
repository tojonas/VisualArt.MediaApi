using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using VisualArt.Media.Dto;
using VisualArt.Media.Util;

namespace VisualArt.Media.Services
{
    public class FileStorageService : IFileStorage
    {
        private const string HashesFolder = ".hashes";
        private readonly string _rootPath;
        private readonly ILogger _logger;
        private Options _options = new();
        public long MaxFileSize => _options.MaxFileSize;

        public FileStorageService(ILogger<FileStorageService> logger, IOptions<Options> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            _rootPath = _options.ExpandedRootPath;

            _logger.LogInformation($"RootPath: [{_rootPath}] MaxFileSize: [{MaxFileSize}]");
            EnsureDirectories();
        }
        void EnsureDirectories()
        {
            if (!Directory.Exists(_rootPath))
            {
                Directory.CreateDirectory(_rootPath);
                _logger.LogInformation($"Created RootPath: [{_rootPath}]");
            }
            var hashes = Path.Combine(_rootPath, HashesFolder);
            if (!Directory.Exists(hashes))
            {
                Directory.CreateDirectory(hashes);
                _logger.LogInformation($"Created HashesFolder: [{hashes}]");
            }
        }
        public IEnumerable<FileMetadata> ListFiles()
        {
            var di = new DirectoryInfo(_rootPath);
            return di.GetFiles().Select(fi => CreateMetadata(fi));
        }

        public bool FileExistsInStore(string path, string hash)
        {
            return File.Exists(path) && hash == File.ReadAllText(path);
        }

        public async Task<FileMetadata> SaveFileAsync(string fileName, Stream stream)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _ = stream ?? throw new ArgumentNullException(nameof(stream));

            if (stream.Length > MaxFileSize)
            {
                // Not the best way to handle this, but it works for now
                return new FileMetadata(fileName, 0, DateTime.MinValue, DateTime.MinValue);
            }
            var safeFilename = ValidateName(fileName);
            var storagePath = Path.Combine(_rootPath, safeFilename);

            var hash = CalculateHash(stream);
            var hashPath = Path.Combine(_rootPath, HashesFolder, safeFilename);

            if (FileExistsInStore(hashPath, hash))
            {
                _logger.LogInformation($"File [{safeFilename}] already exists: {hashPath}");
            }
            else
            {
                try
                {
                    var tempPath = $"{storagePath}.{Guid.NewGuid()}.tmp";
                    using (var tx = new FileTransaction(storagePath, tempPath, hashPath))
                    {
                        using (var fileStream = File.Create(tempPath))
                        {
                            await stream.CopyToAsync(fileStream);
                        }
                        // Use a lock to prevent multiple threads from writing to the same file
                        File.Move(tempPath, storagePath, true);
                        File.WriteAllText(hashPath, hash);

                        _logger.LogInformation($"Saved file: {storagePath}");
                        tx.Commit();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error saving file: {storagePath}");
                    throw;
                }
            }
            return CreateMetadata(storagePath);
        }

        public string ValidateName(string fileName)
        {
            if (fileName == HashesFolder || string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"Invalid file name: {fileName}");
            }
            return SafeFileName.MakeSafe(fileName);
        }

        FileMetadata CreateMetadata(string path)
        {
            return CreateMetadata(new FileInfo(path));
        }
        FileMetadata CreateMetadata(FileInfo fi)
        {
            return new FileMetadata(fi.Name, fi.Length, File.GetCreationTimeUtc(fi.FullName), File.GetLastWriteTimeUtc(fi.FullName));
        }

        public static string CalculateHash(Stream stream)
        {
            using (SHA1 sha1 = SHA1.Create())
            {
                byte[] hashBytes = sha1.ComputeHash(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public void DropStorage()
        {
            _logger.LogInformation($"Dropping Storage: {_rootPath}");
            Directory.Delete(_rootPath, true);
            EnsureDirectories();
        }
        public class Options
        {
            public const string SectionName = "FileStorage";
            public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "VisualArt.Media");
            public long MaxFileSize { get; set; } = 500 * 1024 * 1024;
            public string ExpandedRootPath => Environment.ExpandEnvironmentVariables(RootPath);
        }
    }
}
