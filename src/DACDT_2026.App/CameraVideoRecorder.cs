using System;
using System.Drawing;
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

                Bitmap normalizedFrame = null;
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
                        // The bundled FFmpeg 3.2.2 x86 H264 encoder crashes during sws_scale.
                        writer.Open(filePath, width, height, new Accord.Math.Rational(framesPerSecond, 1), VideoCodec.MPEG4, bitRate);
                    }

                    normalizedFrame = CameraVideoFrameNormalizer.CreateRgb24Frame(source, writer.Width, writer.Height);
                    writer.WriteVideoFrame(normalizedFrame);

                    return true;
                }
                finally
                {
                    normalizedFrame?.Dispose();
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
