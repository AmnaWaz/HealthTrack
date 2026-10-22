using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;

namespace HealthTrack.Services
{
    public static class ProfilePhotoService
    {
        public static string Save(HttpPostedFileBase file, HttpServerUtilityBase server)
        {
            if (file == null || file.ContentLength == 0)
                throw new InvalidOperationException("Choose an image to upload.");
            if (file.ContentLength > 3 * 1024 * 1024)
                throw new InvalidOperationException("The image must be 3 MB or smaller.");
            var extension = Path.GetExtension(file.FileName ?? "").ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png")
                throw new InvalidOperationException("Choose a JPEG or PNG image.");
            using (var original = Image.FromStream(file.InputStream, false, true))
            {
                if (original.Width < 64 || original.Height < 64 ||
                    original.Width > 6000 || original.Height > 6000 ||
                    original.Width * (long)original.Height > 24000000)
                    throw new InvalidOperationException("Choose an image between 64 and 6000 pixels per side.");
                var side = Math.Min(original.Width, original.Height);
                var square = new Rectangle((original.Width - side) / 2, (original.Height - side) / 2, side, side);
                var name = Guid.NewGuid().ToString("N") + ".jpg";
                var folder = server.MapPath("~/Content/uploads/profiles/");
                Directory.CreateDirectory(folder);
                using (var output = new Bitmap(512, 512))
                using (var graphics = Graphics.FromImage(output))
                {
                    graphics.Clear(Color.White);
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    graphics.DrawImage(original, new Rectangle(0, 0, 512, 512), square, GraphicsUnit.Pixel);
                    var codec = ImageCodecInfo.GetImageEncoders().First(c => c.MimeType == "image/jpeg");
                    using (var enc = new EncoderParameters(1))
                    {
                        enc.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 85L);
                        output.Save(Path.Combine(folder, name), codec, enc);
                    }
                }
                return "/Content/uploads/profiles/" + name;
            }
        }

        public static void DeleteOld(string oldPath, HttpServerUtilityBase server)
        {
            const string prefix = "/Content/uploads/profiles/";
            if (string.IsNullOrEmpty(oldPath) || !oldPath.StartsWith(prefix, StringComparison.Ordinal)) return;
            var name = oldPath.Substring(prefix.Length);
            if (!System.Text.RegularExpressions.Regex.IsMatch(name, @"^[a-f0-9]{32}\.jpg$")) return;
            var path = Path.Combine(server.MapPath("~/Content/uploads/profiles/"), name);
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
