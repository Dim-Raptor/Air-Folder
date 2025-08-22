using Shell32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

public class FileItem
{
    public string Name { get; set; }
    public string Path { get; set; }
    public BitmapSource Icon { get; set; }

    public static async Task<List<FileItem>> GetFilesAsync(string folderPath)
    {
        var files = new List<FileItem>();

        foreach (var file in Directory.GetFiles(folderPath))
        {
            var item = new FileItem { Name = Path.GetFileName(file) };

            // Получение иконки через Shell32
            var shellFile = ShellFile.FromFilePath(file);
            var icon = shellFile.Thumbnail.ExtraLargeBitmap;

            item.Icon = Imaging.CreateBitmapSourceFromHBitmap(
                icon.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            files.Add(item);
            await Task.Delay(1); // Для виртуализации
        }

        return files;
    }
}
