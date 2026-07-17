using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DACDT_2026
{
    /// <summary>
    /// Keeps large CAD MQTT payloads below the broker/browser message limit.
    /// Items are already serialized JSON objects, so splitting never changes their content.
    /// </summary>
    public static class CadMqttTransfer
    {
        public const int DefaultChunkBytes = 192 * 1024;

        public static List<List<string>> SplitJsonItems(IEnumerable<string> items, int maxBytes)
        {
            if (items == null)
                throw new ArgumentNullException(nameof(items));
            if (maxBytes < 3)
                throw new ArgumentOutOfRangeException(nameof(maxBytes));

            var result = new List<List<string>>();
            var current = new List<string>();
            int currentBytes = 2; // '[' + ']'

            foreach (string item in items)
            {
                if (string.IsNullOrWhiteSpace(item))
                    throw new ArgumentException("CAD JSON item is empty.", nameof(items));

                int itemBytes = Encoding.UTF8.GetByteCount(item);
                int separatorBytes = current.Count == 0 ? 0 : 1;
                if (itemBytes + 2 > maxBytes)
                    throw new InvalidOperationException("A CAD primitive is larger than the MQTT chunk limit.");

                if (current.Count > 0 && currentBytes + separatorBytes + itemBytes > maxBytes)
                {
                    result.Add(current);
                    current = new List<string>();
                    currentBytes = 2;
                    separatorBytes = 0;
                }

                current.Add(item);
                currentBytes += separatorBytes + itemBytes;
            }

            if (current.Count > 0)
                result.Add(current);

            return result;
        }

        public static string BuildJsonArray(IEnumerable<string> items)
        {
            return "[" + string.Join(",", items ?? Enumerable.Empty<string>()) + "]";
        }
    }
}
