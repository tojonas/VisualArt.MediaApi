namespace VisualArt.Media.Dto
{
    public record FileMetadata(string Name, long Length, bool Folder, DateTime Created, DateTime Modified)
    {
        static public FileMetadata Create(string path)
        {
            return Create(new FileInfo(path));
        }
        static public FileMetadata Create(FileInfo fi)
        {
            return new FileMetadata(fi.Name, fi.Length, false, File.GetCreationTimeUtc(fi.FullName), File.GetLastWriteTimeUtc(fi.FullName));
        }
        static public FileMetadata Create(DirectoryInfo di)
        {
            return new FileMetadata(di.Name, 0, true, File.GetCreationTimeUtc(di.FullName), File.GetLastWriteTimeUtc(di.FullName));
        }
    }
}
