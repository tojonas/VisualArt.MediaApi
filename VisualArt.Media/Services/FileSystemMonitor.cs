﻿using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace VisualArt.Media.Services
{
    public class FileSystemMonitor : BackgroundService, IDisposable
    {
        private readonly ILogger _logger;
        private readonly Options _options;
        FileSystemWatcher _monitor;

        public FileSystemMonitor(ILogger<FileSystemMonitor> logger, IOptions<Options> options)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options.Value ?? throw new ArgumentNullException(nameof(options));

            var rootPath = Environment.ExpandEnvironmentVariables(_options.RootPath);
            EnsureDirectories(rootPath);
            _monitor = new FileSystemWatcher(rootPath);

            _monitor.NotifyFilter = NotifyFilters.Attributes
                                 | NotifyFilters.CreationTime
                                 | NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastAccess
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Security
                                 | NotifyFilters.Size;

            _monitor.Changed += OnChanged;
            _monitor.Created += (object sender, FileSystemEventArgs e) => _logger.LogInformation($"Created: {e.FullPath}");
            _monitor.Deleted += (object sender, FileSystemEventArgs e) => _logger.LogInformation($"Deleted: {e.FullPath}");
            _monitor.Renamed += (object sender, RenamedEventArgs e) => _logger.LogInformation($"Renamed:  {e.OldFullPath} >> {e.FullPath}");
            _monitor.Error += OnError;

            _monitor.Filter = "*.*";
            _monitor.IncludeSubdirectories = true;

        }

        void EnsureDirectories(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                _logger.LogInformation($"Created RootPath: [{path}]");
            }
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _monitor.EnableRaisingEvents = true;
            return Task.CompletedTask;
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Changed)
            {
                return;
            }
            _logger.LogInformation($"Changed: {e.FullPath}");
        }

        private void OnError(object sender, ErrorEventArgs e)
        {
            var ex = e.GetException();
            if (ex != null)
            {
                _logger.LogError(ex, ex.StackTrace);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _monitor.Dispose();
        }

        public class Options
        {
            public const string SectionName = "FileSystemMonitor";
            public string RootPath { get; set; } = Path.Combine(Path.GetTempPath(), "VisualArt.Media");
        }
    }
}
