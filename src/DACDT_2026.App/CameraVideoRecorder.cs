using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using Accord.Video.FFMPEG;

namespace DACDT_2026
{
    internal sealed class CameraVideoRecorder : IDisposable
    {
        private readonly object sync = new object();
        private readonly string filePath;
        private readonly int framesPerSecond;
        private readonly int bitRate;
        private VideoFileWriter writer;
        private bool closed;

        public CameraVideoRecorder(string filePath, int framesPerSecond, int bitRate)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A video file path is required.", nameof(filePath));

            this.filePath = filePath;
            this.framesPerSecond = Math.Max(1, framesPerSecond);
            this.bitRate = Math.Max(100000, bitRate);
        }

        public bool WriteFrame(Bitmap source)
        {
            if (source == null)
                return false;

            lock (sync)
            {
                if (closed)
                    return false;

                Bitmap evenSizedFrame = null;
                try
                {
                    if (writer == null)
                    {
                        Directory.CreateDirectory(Path.GetDirectoryName(filePath));
                        int width = source.Width & ~1;
                        int height = source.Height & ~1;
                        if (width < 2 || height < 2)
                            return false;

                        writer = new VideoFileWriter();
                        writer.Open(filePath, width, height, new Accord.Math.Rational(framesPerSecond, 1), VideoCodec.H264, bitRate);
                    }

                    if (source.Width != writer.Width || source.Height != writer.Height)
                    {
                        evenSizedFrame = new Bitmap(writer.Width, writer.Height, PixelFormat.Format24bppRgb);
                        using (Graphics graphics = Graphics.FromImage(evenSizedFrame))
                        {
                            graphics.CompositingMode = CompositingMode.SourceCopy;
                            graphics.DrawImage(source, new Rectangle(0, 0, writer.Width, writer.Height));
                        }

                        writer.WriteVideoFrame(evenSizedFrame);
                    }
                    else
                    {
                        writer.WriteVideoFrame(source);
                    }

                    return true;
                }
                finally
                {
                    evenSizedFrame?.Dispose();
                }
            }
        }

        public void Complete()
        {
            lock (sync)
            {
                if (closed)
                    return;

                closed = true;
                try
                {
                    writer?.Close();
                }
                finally
                {
                    writer?.Dispose();
                    writer = null;
                }
            }
        }

        public void Dispose()
        {
            Complete();
        }
    }
}
