namespace VisualArt.Media.Util
{
    public class FileTransaction : IDisposable
    {
        private HashSet<string> _paths = new();
        public FileTransaction(params string[] paths)
        {
            _paths = paths.ToHashSet();
        }
        public void StartTracking(string path)
        {
            _paths.Add(path);
        }
        public void Commit(string? path=null)
        {
            if( path == null )
            {
                _paths.Clear();
            }
            else
            {
                _paths.Remove(path);
            }
        }
        public void Dispose()
        {
            foreach (var path in _paths)
            {
                try
                {
                    File.Delete(path);
                }
                catch
                {
                    // Just eat the exception
                }
            }
        }
    }
}
