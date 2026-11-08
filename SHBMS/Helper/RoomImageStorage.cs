using System.IO;

namespace SHBMS.Helper;

public static class RoomImageStorage
{
    // When running from bin/Debug or bin/Release, find the source project.
    // A published app uses the Images folder beside its executable.
    private static string? ProjectDirectory()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "SHBMS.csproj")))
                return directory.FullName;
            directory = directory.Parent;
        }
        return null;
    }

    private static string Root => Path.Combine(ProjectDirectory() ?? AppContext.BaseDirectory, "Images");

    public static string Import(string source, string folder)
    {
        if (folder != "rooms" && folder != "room-types")
            throw new ArgumentException("Choose the rooms or room-types image folder.");
        string extension = Path.GetExtension(source).ToLowerInvariant();
        if (!new[] { ".jpg", ".jpeg", ".png", ".bmp" }.Contains(extension))
            throw new ArgumentException("Choose a JPG, PNG or BMP image.");
        if (new FileInfo(source).Length > 10 * 1024 * 1024)
            throw new ArgumentException("Choose an image no larger than 10 MB.");

        string directory = Path.Combine(Root, folder);
        Directory.CreateDirectory(directory);
        string fileName = Guid.NewGuid().ToString("N") + extension;
        File.Copy(source, Path.Combine(directory, fileName));
        return "Images/" + folder + "/" + fileName;
    }

    private static string? FindFile(string directory, string relativePath)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        string file = Path.GetFullPath(Path.Combine(directory, relativePath));
        return file.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(file) ? file : null;
    }

    public static string? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path)) return null;
        try
        {
            if (Path.IsPathRooted(path)) return File.Exists(path) ? path : null;
            string relative = path.Replace('\\', '/');
            if (relative.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
                relative = relative[7..];

            // Support new paths, old paths such as rooms/abc.jpg, and seed filenames.
            string legacy = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SBHMS", "Images");
            foreach (string directory in new[] { Root, Path.Combine(AppContext.BaseDirectory, "Images"), legacy })
            {
                string? file = FindFile(directory, relative);
                if (file == null && !relative.Contains('/'))
                    file = FindFile(directory, "rooms/" + relative) ?? FindFile(directory, "room-types/" + relative);
                if (file != null) return file;
            }
            return FindFile(AppContext.BaseDirectory, path);
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
