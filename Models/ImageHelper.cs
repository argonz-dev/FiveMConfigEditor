using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace FiveMConfigEditorWPF.Models
{
    public static class ImageHelper
    {
        /// <summary>
        /// Jika path adalah .ico, convert ke .png di folder yang sama dan return path PNG-nya.
        /// Jika bukan .ico, return path asli.
        /// </summary>
        public static string EnsurePng(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return path;
            if (!path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase)) return path;

            var pngPath = Path.ChangeExtension(path, ".png");
            try
            {
                var frame = GetBestIcoFrame(path);
                if (frame == null) return path;

                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(frame));
                using var fs = File.OpenWrite(pngPath);
                encoder.Save(fs);
                return pngPath;
            }
            catch
            {
                return path; // fallback ke path asli kalau gagal
            }
        }

        /// <summary>
        /// Load BitmapSource dari file .ico atau gambar biasa.
        /// </summary>
        public static BitmapSource? LoadImage(string path)
        {
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return null;
            try
            {
                if (path.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                    return GetBestIcoFrame(path);

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = new Uri(path);
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch { return null; }
        }

        private static BitmapFrame? GetBestIcoFrame(string path)
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            var decoder = new IconBitmapDecoder(stream,
                BitmapCreateOptions.PreservePixelFormat,
                BitmapCacheOption.OnLoad);

            BitmapFrame? best = null;
            foreach (var frame in decoder.Frames)
                if (best == null || frame.PixelWidth > best.PixelWidth)
                    best = frame;

            if (best == null) return null;

            // Freeze supaya bisa dipakai di thread lain
            var copy = BitmapFrame.Create(best);
            copy.Freeze();
            return copy;
        }
    }
}
