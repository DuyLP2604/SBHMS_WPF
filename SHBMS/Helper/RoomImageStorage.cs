using System.IO;
namespace SHBMS.Helper;
public static class RoomImageStorage
{
    private static string Root => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SBHMS", "Images");
    public static string Import(string source, string folder)
    {
        string extension = Path.GetExtension(source).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(extension))
            throw new ArgumentException("Choose a JPG, PNG or BMP image.");
        if (new FileInfo(source).Length > 10 * 1024 * 1024)
            throw new ArgumentException("Choose an image smaller than 10 MB.");
        string directory = Path.Combine(Root, folder);
        Directory.CreateDirectory(directory);
        string fileName = Guid.NewGuid().ToString("N") + extension;
        File.Copy(source, Path.Combine(directory, fileName));
        return folder + "/" + fileName;
    }
    public static string? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            if (Path.IsPathRooted(path)) return File.Exists(path) ? path : null;
            string imported = Path.GetFullPath(Path.Combine(Root, path));
            if (imported.StartsWith(Root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                && File.Exists(imported)) return imported;
            string bundled = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, path));
            return File.Exists(bundled) ? bundled : null;
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException) { return null; }
    }
}
