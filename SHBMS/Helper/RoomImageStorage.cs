using System.IO;

namespace SHBMS.Helper;

public static class RoomImageStorage
{
    // When running from bin/Debug or bin/Release, find the source project.
    // A published app uses the Images folder beside its executable.

    /// <summary>
    /// Find the source project when running from a local build folder.
    /// </summary>
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

    // Development saves images in the project; published apps use their application folder.
    private static string Root => Path.Combine(ProjectDirectory() ?? AppContext.BaseDirectory, "Images");

    /// <summary>
    /// Copy a supported image into the project and return its relative path.
    /// </summary>
    public static string Import(string source, string folder)
    {
        if (folder != "rooms" && folder != "room-types")
            throw new ArgumentException("Choose the rooms or room-types image folder.");
        string extension = Path.GetExtension(source).ToLowerInvariant();
        if (!new[]
        {
            ".jpg",
            ".jpeg",
            ".png",
            ".bmp"
        }.Contains(extension))
            throw new ArgumentException("Choose a JPG, PNG or BMP image.");
        if (new FileInfo(source).Length > 10 * 1024 * 1024)
            throw new ArgumentException("Choose an image no larger than 10 MB.");

        string directory = Path.Combine(Root, folder);
        Directory.CreateDirectory(directory);
        // Use a unique filename so selecting another image does not overwrite an existing one.
        string fileName = Guid.NewGuid().ToString("N") + extension;
        File.Copy(source, Path.Combine(directory, fileName));
        // Relative paths can still work when the project moves to another computer.
        return "Images/" + folder + "/" + fileName;
    }

    /// <summary>
    /// Resolve a file only when it stays inside the requested image directory.
    /// </summary>
    private static string? FindFile(string directory, string relativePath)
    {
        string root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        string file = Path.GetFullPath(Path.Combine(directory, relativePath));
        return file.StartsWith(root, StringComparison.OrdinalIgnoreCase) && File.Exists(file) ? file : null;
    }

    /// <summary>
    /// Find a saved image path while supporting older image locations.
    /// </summary>
    public static string? Resolve(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;
        try
        {
            if (Path.IsPathRooted(path))
                return File.Exists(path) ? path : null;
            string relative = path.Replace('\\', '/');
            if (relative.StartsWith("Images/", StringComparison.OrdinalIgnoreCase))
                relative = relative[7..];

            // Support new paths, old paths such as rooms/abc.jpg, and seed filenames.
            string legacy = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SBHMS",
                "Images"
            );
            foreach (string directory in new[]
            {
                Root,
                Path.Combine(AppContext.BaseDirectory, "Images"),
                legacy
            })
            {
                string? file = FindFile(directory, relative);
                if (file == null && !relative.Contains('/'))
                    file = FindFile(directory, "rooms/" + relative) ?? FindFile(directory, "room-types/" + relative);
                if (file != null)
                    return file;
            }
            return FindFile(AppContext.BaseDirectory, path);
        }
        // A missing or invalid image should clear the preview instead of breaking the page.
        catch (Exception ex) when (ex is ArgumentException or IOException or NotSupportedException or UnauthorizedAccessException)
        {
            return null;
        }
    }
}
