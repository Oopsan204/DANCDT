using System;
using System.Collections.Generic;
using System.IO;

namespace DACDT_2026
{
    public sealed class WebCadUploadSession
    {
        public const int MaxUploadBytes = 8 * 1024 * 1024;
        public const int MaxChunks = 512;

        private readonly Dictionary<int, byte[]> chunks = new Dictionary<int, byte[]>();
        private int expectedChunks;
        private int expectedBytes;

        public string JobId { get; private set; }
        public string FileName { get; private set; }

        public int ReceivedChunks => chunks.Count;
        public int ExpectedChunks => expectedChunks;

        public void Reset()
        {
            chunks.Clear();
            JobId = null;
            FileName = null;
            expectedChunks = 0;
            expectedBytes = 0;
        }

        public void Begin(string jobId, string fileName, int totalChunks, int totalBytes)
        {
            if (string.IsNullOrWhiteSpace(jobId))
                throw new ArgumentException("Upload job id is empty.");
            if (!IsAllowedFileName(fileName))
                throw new ArgumentException("Only DXF and G-code files are accepted.");
            if (totalChunks <= 0 || totalChunks > MaxChunks)
                throw new ArgumentException("Invalid upload chunk count.");
            if (totalBytes <= 0 || totalBytes > MaxUploadBytes)
                throw new ArgumentException("Upload file is empty or too large.");

            chunks.Clear();
            JobId = jobId.Trim();
            FileName = Path.GetFileName(fileName);
            expectedChunks = totalChunks;
            expectedBytes = totalBytes;
        }

        public bool AddChunk(string jobId, int index, string base64)
        {
            if (!string.Equals(JobId, jobId, StringComparison.Ordinal))
                return false;
            if (index < 0 || index >= expectedChunks)
                throw new ArgumentOutOfRangeException(nameof(index), "Upload chunk index is out of range.");
            if (string.IsNullOrEmpty(base64))
                throw new ArgumentException("Upload chunk is empty.");

            chunks[index] = Convert.FromBase64String(base64);
            return chunks.Count == expectedChunks;
        }

        public byte[] Assemble()
        {
            if (chunks.Count != expectedChunks)
                throw new InvalidOperationException("Upload is missing chunks.");

            using (var ms = new MemoryStream(expectedBytes))
            {
                for (int i = 0; i < expectedChunks; i++)
                {
                    byte[] data;
                    if (!chunks.TryGetValue(i, out data))
                        throw new InvalidOperationException("Upload is missing chunk " + i + ".");
                    ms.Write(data, 0, data.Length);
                    if (ms.Length > MaxUploadBytes)
                        throw new InvalidOperationException("Upload file is too large.");
                }

                if (ms.Length != expectedBytes)
                    throw new InvalidOperationException("Upload byte count does not match.");

                return ms.ToArray();
            }
        }

        public static bool IsAllowedFileName(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return false;

            string ext = Path.GetExtension(fileName).ToLowerInvariant();
            switch (ext)
            {
                case ".dxf":
                case ".gcode":
                case ".g":
                case ".gc":
                case ".nc":
                case ".ngc":
                case ".cnc":
                case ".tap":
                    return true;
                default:
                    return false;
            }
        }
    }
}
