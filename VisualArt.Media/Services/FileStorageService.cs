using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.IO;
using System.Security.Cryptography;
using VisualArt.Media.Dto;
using VisualArt.Media.Util;

namespace VisualArt.Media.Services
{
    public class FileStorageService : IFileStorage
    {
        private readonly char[] _invalidChars = new List<char>(Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars())).ToArray();

        private const string HashesFolder = ".hashes";
        private readonly string _rootPath;
        private readonly ILogger _logger;
        private readonly Options _options = new();
        public long MaxFileSize => _options.MaxFileSize;

        public FileStorageService(ILogger<FileStorageService> logger, IOptions<Options> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            _rootPath = _options.ExpandedRootPath;

            _logger.LogInformation($"RootPath: [{_rootPath}] MaxFileSize: [{MaxFileSize}]");
            EnsureDirectories(_rootPath);
        }
        string EnsureDirectories( string folder )
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
        public IEnumerable<FileMetadata> ListFiles(string path)
        {
            var safePath = ValidatePath(path);
            var di = new DirectoryInfo(Path.Combine(_rootPath, safePath));
            if( di.Exists == false)
            {
                return Enumerable.Empty<FileMetadata>();
            }
            return di.GetDirectories().Where(d=>d.Name != HashesFolder)
                .Select( d=>CreateMetadata(d))
                .Concat(di.GetFiles().Select(fi => CreateMetadata(fi)));
        }

        public bool FileExistsInStore(string path, string hash)
        {
            return File.Exists(path) && hash == File.ReadAllText(path);
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

            var saveDirectory = EnsureDirectories(Path.Combine(_rootPath, ValidatePath(path)));

            var safeFilename = ValidateName(fileName);
            var storagePath = Path.Combine(saveDirectory, safeFilename);

            var hash = CalculateHash(stream);
            var hashPath = Path.Combine(saveDirectory, HashesFolder, safeFilename);

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
        void EnsureValidFileType( string extension )
        {
            if (_options.BlockedExtensions.Contains(extension.ToLower()))
            {
                throw new ArgumentException($"File extension [{extension}] is not allowed");
            }
        }
        public string ValidatePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            if (path.IndexOfAny(_invalidChars) >= 0)
            {
                throw new ArgumentException($"Invalid path: {path}");
            }
            var parts = new List<string>(path.Split("/"));
            if (parts.Count > _options.MaxFolderDepth)
            {
                throw new ArgumentException($"Invalid path: {path}");
            }
            foreach (var part in parts)
            {
                if (part == HashesFolder || string.IsNullOrWhiteSpace(part) || part == "." || part == "..")
                {
                    throw new ArgumentException($"Invalid path: {path}");
                }
            }
            return Path.Combine(parts.ToArray());
        }
        public string ValidateName(string fileName)
        {
            if (fileName == HashesFolder || string.IsNullOrWhiteSpace(fileName))
            {
                throw new ArgumentException($"Invalid file name: {fileName}");
            }
            if (fileName.IndexOfAny(_invalidChars) >= 0)
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
            return new FileMetadata(fi.Name, fi.Length, false, File.GetCreationTimeUtc(fi.FullName), File.GetLastWriteTimeUtc(fi.FullName));
        }
        FileMetadata CreateMetadata(DirectoryInfo di)
        {
            return new FileMetadata(di.Name, 0, true, File.GetCreationTimeUtc(di.FullName), File.GetLastWriteTimeUtc(di.FullName));
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
            EnsureDirectories(_rootPath);
        }
        public class Options
        {
            public const string SectionName = "FileStorage";
            public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "VisualArt.Media");
            public long MaxFileSize { get; set; } = 500 * 1024 * 1024;
            public long MaxFolderDepth { get; set; } = 16;
            public HashSet<string> BlockedExtensions { get; set; } = new();
            public string ExpandedRootPath => Environment.ExpandEnvironmentVariables(RootPath);
        }
    }
}
