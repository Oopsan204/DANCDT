using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace DACDT_2026
{
    internal static class CameraVideoFrameNormalizer
    {
        public static Bitmap CreateRgb24Frame(Bitmap source, int width, int height)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            int targetWidth = width & ~1;
            int targetHeight = height & ~1;
            if (targetWidth < 2 || targetHeight < 2)
                throw new ArgumentOutOfRangeException("Video dimensions must be at least two pixels and even.");

            var result = new Bitmap(targetWidth, targetHeight, PixelFormat.Format24bppRgb);
            try
            {
                using (Graphics graphics = Graphics.FromImage(result))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.DrawImage(source, new Rectangle(0, 0, targetWidth, targetHeight));
                }

                return result;
            }
            catch
            {
                result.Dispose();
                throw;
            }
        }
    }
}
