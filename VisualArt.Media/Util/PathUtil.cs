namespace VisualArt.Media.Util
{
    public class PathUtil
    {
        private static readonly char[] _invalidChars = new List<char>(Path.GetInvalidPathChars().Concat(Path.GetInvalidFileNameChars())).ToArray();
        private readonly HashSet<string> _reservedNames = new() { ".", ".." };
        private readonly uint _maxDepth = 32;

        public PathUtil()
        {

        }
        public PathUtil(IEnumerable<string> reservedNames, uint maxDepth)
        {
            _reservedNames = _reservedNames.Concat(reservedNames).ToHashSet();
            _maxDepth = maxDepth;
        }
        public string ValidatePath(string path, int maxDepth = int.MaxValue)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }
            var parts = new List<string>(path.Split("/"));
            if (parts.Count > _maxDepth)
            {
                throw new ArgumentException($"Invalid path: {path}");
            }
            foreach (var part in parts)
            {
                ValidateName(part);
            }
            return Path.Combine(parts.ToArray());
        }

        public string ValidateName(string fileName)
        {
            if (_reservedNames.Contains(fileName))
            {
                throw new ArgumentException($"Invalid file name: {fileName}");
            }
            if (fileName.IndexOfAny(_invalidChars) >= 0)
            {
                throw new ArgumentException($"Invalid file name: {fileName}");
            }
            return SafeFileName.MakeSafe(fileName);
        }
    }
}
