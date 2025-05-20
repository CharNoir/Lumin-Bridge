using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LuminBridgeFramework
{
    public static class SerializationHelper
    {
        public static string ConfigDirectory =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Configs");

        public static string MakeSafeFileName(string name)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(c, '_');
            }
            return name;
        }

        public static string GetConfigPath(string baseName)
        {
            return Path.Combine(ConfigDirectory, MakeSafeFileName(baseName) + ".json");
        }

        /// <summary>
        /// Converts a string to a fixed-length ASCII-only string,
        /// truncating or null-padding as needed.
        /// </summary>
        /// <param name="input">Input string.</param>
        /// <param name="length">Fixed length to output (default: 32).</param>
        /// <returns>ASCII-only string of exact length, null-padded if needed.</returns>
        public static string AsciiStringToFixedLengthString(string input, int length = 32)
        {
            if (string.IsNullOrEmpty(input))
                return new string('\0', length);

            // Convert to ASCII and discard non-ASCII characters
            var ascii = Encoding.ASCII.GetString(Encoding.ASCII.GetBytes(input));

            if (ascii.Length >= length)
                return ascii.Substring(0, length - 1) + '\0';

            return ascii.PadRight(length, '\0');
        }
    }
}
