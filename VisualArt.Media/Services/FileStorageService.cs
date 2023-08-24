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
        private readonly ILogger _logger;
        private readonly Options _options = new();
        private readonly PathUtil _pathUtil;
        private string RootPath => _options.RootPath;
        public long MaxFileSize => _options.MaxFileSize;

        public FileStorageService(ILogger<FileStorageService> logger, IOptions<Options> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            _pathUtil = new PathUtil(_options.MaxFolderDepth, HashesFolder);
            _logger.LogInformation($"RootPath: [{RootPath}] MaxFileSize: [{MaxFileSize}]");
            EnsureDirectories(RootPath);
        }

        public IEnumerable<FileMetadata> ListFiles(string path)
        {
            var safePath = ValidatePath(path);
            var di = new DirectoryInfo(Path.Combine(RootPath, safePath));
            if (di.Exists == false)
            {
                return Enumerable.Empty<FileMetadata>();
            }
            return di.GetDirectories().Where(d => d.Name != HashesFolder)
                .Select(d => FileMetadata.Create(d))
                .Concat(di.GetFiles().Select(fi => FileMetadata.Create(fi)));
        }
        public async Task<FileMetadata> SaveFileAsync(string path, string fileName, Stream stream)
        {
            _ = fileName ?? throw new ArgumentNullException(nameof(fileName));
            _ = stream ?? throw new ArgumentNullException(nameof(stream));

            if (stream.Length > MaxFileSize)
            {
                // Not the best way to handle this, but it works for now
                return new FileMetadata(fileName, -1, false, DateTime.MinValue, DateTime.MinValue);
            }
            EnsureValidFileType(Path.GetExtension(fileName));

            var saveDirectory = EnsureDirectories(Path.Combine(RootPath, ValidatePath(path)));

            var safeFilename = ValidateName(fileName);
            var storagePath = Path.Combine(saveDirectory, safeFilename);

            var hash = await CalculateHashAsync(stream);
            var hashPath = Path.Combine(saveDirectory, HashesFolder, $"{safeFilename}.txt");

            if ( await FileExistsInStoreAsync(hashPath, hash))
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
                        await File.WriteAllTextAsync(hashPath, hash);

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
            return FileMetadata.Create(storagePath);
        }

        string ValidatePath(string path) => _pathUtil.ValidatePath(path);
        string ValidateName(string fileName) => _pathUtil.ValidateName(fileName);
        string EnsureDirectories(string folder)
        {
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
                _logger.LogInformation($"Created RootPath: [{folder}]");
            }
            var hashes = Path.Combine(folder, HashesFolder);
            if (!Directory.Exists(hashes))
            {
                Directory.CreateDirectory(hashes);
                _logger.LogInformation($"Created HashesFolder: [{hashes}]");
            }
            return folder;
        }
        async Task<bool> FileExistsInStoreAsync(string path, string hash)
        {
            return File.Exists(path) && hash == await File.ReadAllTextAsync(path);
        }

        void EnsureValidFileType(string extension)
        {
            if (_options.BlockedExtensions.Contains(extension.ToLower()))
            {
                throw new ArgumentException($"File extension [{extension}] is not allowed");
            }
        }

        static async Task<string> CalculateHashAsync(Stream stream)
        {
            using (var sha1 = SHA1.Create())
            {
                byte[] hashBytes = await sha1.ComputeHashAsync(stream);
                stream.Seek(0, SeekOrigin.Begin);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }

        public void DropStorage()
        {
            _logger.LogInformation($"Dropping Storage: {RootPath}");
            Directory.Delete(RootPath, true);
            EnsureDirectories(RootPath);
        }
        public class Options
        {
            public const string SectionName = "FileStorage";
            private string _rootPath = Path.Combine(Path.GetTempPath(), "VisualArt.Media");

            public string RootPath
            {
                get { return _rootPath; }
                set { _rootPath = Environment.ExpandEnvironmentVariables(value); }
            }
            public long MaxFileSize { get; set; } = 500 * 1024 * 1024;
            public uint MaxFolderDepth { get; set; } = 16;
            public HashSet<string> BlockedExtensions { get; set; } = new();
        }
    }
}
