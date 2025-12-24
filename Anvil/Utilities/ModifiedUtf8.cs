using System.Text;

namespace Anvil.Utilities;

/// <summary>
/// Handles strict JVM Modified UTF-8 encoding and decoding.
/// Spec §4.4.7
/// </summary>
public static class ModifiedUtf8
{
    public static byte[] Encode(string s)
    {
        // Pre-calculate length to allocate buffer (slight overestimation is usually more efficient than resizing).
        // In the worst case, the length is string.Length * 3.
        using var stream = new MemoryStream(s.Length * 2); 
        
        foreach (char c in s)
        {
            if (c >= 0x0001 && c <= 0x007F)
            {
                // 1-byte group: 0xxxxxxx
                stream.WriteByte((byte)c);
            }
            else if (c == 0x0000 || (c >= 0x0080 && c <= 0x07FF))
            {
                // 2-byte group: 110xxxxx 10xxxxxx
                // Note: \u0000 is also handled here, encoded as C0 80
                stream.WriteByte((byte)(0xC0 | (0x1F & (c >> 6))));
                stream.WriteByte((byte)(0x80 | (0x3F & c)));
            }
            else
            {
                // 3-byte group: 1110xxxx 10xxxxxx 10xxxxxx
                // Includes normal characters and split parts of Surrogate Pairs
                stream.WriteByte((byte)(0xE0 | (0x0F & (c >> 12))));
                stream.WriteByte((byte)(0x80 | (0x3F & (c >> 6))));
                stream.WriteByte((byte)(0x80 | (0x3F & c)));
            }
        }
        
        return stream.ToArray();
    }

    public static string Decode(byte[] bytes)
    {
        char[] buffer = new char[bytes.Length]; // Character count is always <= byte count
        int charCount = 0;
        int i = 0;

        while (i < bytes.Length)
        {
            int b = bytes[i++] & 0xFF;

            if (b <= 0x7F) // 0xxxxxxx
            {
                buffer[charCount++] = (char)b;
            }
            else if ((b >> 5) == 0x06) // 110xxxxx
            {
                if (i >= bytes.Length) throw new FormatException("Invalid MUTF-8: truncated 2-byte sequence");
                int b2 = bytes[i++];
                // ((x & 0x1f) << 6) + (y & 0x3f)
                buffer[charCount++] = (char)(((b & 0x1F) << 6) | (b2 & 0x3F));
            }
            else if ((b >> 4) == 0x0E) // 1110xxxx
            {
                if (i + 1 >= bytes.Length) throw new FormatException("Invalid MUTF-8: truncated 3-byte sequence");
                int b2 = bytes[i++];
                int b3 = bytes[i++];
                // ((x & 0xf) << 12) + ((y & 0x3f) << 6) + (z & 0x3f)
                buffer[charCount++] = (char)(((b & 0x0F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F));
            }
            else
            {
                throw new FormatException($"Invalid MUTF-8 byte header: {b:X2}");
            }
        }

        return new string(buffer, 0, charCount);
    }
}
