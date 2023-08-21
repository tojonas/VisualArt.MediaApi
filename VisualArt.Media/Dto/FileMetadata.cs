namespace VisualArt.Media.Dto
{
    public record FileMetadata(string Name, long Length, bool Folder, DateTime Created, DateTime Modified);
}
