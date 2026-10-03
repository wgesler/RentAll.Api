using System.Globalization;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace RentAll.Infrastructure.Services;

public static class CreditCardStatementPdfReader
{
    public static bool IsPdf(string? fileName, string? contentType, byte[]? content)
    {
        var name = (fileName ?? string.Empty).Trim();
        var type = (contentType ?? string.Empty).Trim();
        if (name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase) || type.Contains("pdf", StringComparison.OrdinalIgnoreCase))
            return true;

        return content is { Length: >= 5 } && content[0] == (byte)'%' && content[1] == (byte)'P' && content[2] == (byte)'D' && content[3] == (byte)'F' && content[4] == (byte)'-';
    }

    public static string ReadLines(byte[] content)
    {
        if (content == null || content.Length == 0)
            return string.Empty;

        try
        {
            var lines = new List<string>();
            var raw = new List<string>();
            var encryption = TryGetEncryption(content);
            foreach (var stream in ExtractStreams(content, encryption))
            {
                var chunks = new List<TextChunk>();
                CollectText(stream, chunks);
                lines.AddRange(GroupLines(chunks));
                raw.AddRange(chunks.Select(chunk => chunk.Text));
            }

            var joined = string.Join('\n', lines);
            if (!System.Text.RegularExpressions.Regex.IsMatch(joined, @"\d{1,2}[/-]\d{1,2}"))
                joined = string.Join('\n', raw);

            return joined;
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    private static IEnumerable<byte[]> ExtractStreams(byte[] content, PdfEncryption? encryption)
    {
        var text = Encoding.Latin1.GetString(content);
        var index = 0;
        while (index < text.Length)
        {
            var streamAt = FindStreamKeyword(text, index);
            if (streamAt < 0)
                yield break;

            var dataStart = streamAt + "stream".Length;
            if (dataStart < text.Length && text[dataStart] == '\r')
                dataStart++;
            if (dataStart < text.Length && text[dataStart] == '\n')
                dataStart++;

            var dictStart = text.LastIndexOf("<<", streamAt, StringComparison.Ordinal);
            var dict = dictStart >= 0 ? text[dictStart..streamAt] : string.Empty;
            var length = ReadStreamLength(dict);
            byte[] raw;
            int nextIndex;
            if (length is > 0 && dataStart + length.Value <= content.Length)
            {
                raw = content[dataStart..(dataStart + length.Value)];
                nextIndex = dataStart + length.Value;
            }
            else
            {
                var end = text.IndexOf("endstream", dataStart, StringComparison.Ordinal);
                if (end < 0)
                    yield break;

                raw = content[dataStart..end];
                while (raw.Length > 0 && (raw[^1] == (byte)'\n' || raw[^1] == (byte)'\r'))
                    raw = raw[..^1];
                nextIndex = end + "endstream".Length;
            }

            index = nextIndex;
            if (dict.Contains("/Image", StringComparison.Ordinal) || dict.Contains("/Subtype /Image", StringComparison.Ordinal))
                continue;

            var objectId = FindObjectId(text, streamAt);
            if (encryption != null && objectId.Number > 0 && objectId.Number != encryption.EncryptObjectNumber)
                raw = DecryptStream(raw, encryption.Key, objectId.Number, objectId.Generation);

            var decoded = dict.Contains("FlateDecode", StringComparison.Ordinal) ? TryInflate(raw) : raw;
            if (decoded == null || decoded.Length == 0 || decoded.Length > 1_500_000)
                continue;

            var decodedText = Encoding.Latin1.GetString(decoded);
            if (decodedText.Contains("Tj", StringComparison.Ordinal) || decodedText.Contains("TJ", StringComparison.Ordinal))
                yield return decoded;
        }
    }

    private static int FindStreamKeyword(string text, int index)
    {
        while (index < text.Length)
        {
            var at = text.IndexOf("stream", index, StringComparison.Ordinal);
            if (at < 0)
                return -1;

            var before = at - 1;
            while (before >= 0 && char.IsWhiteSpace(text[before]))
                before--;

            var after = at + "stream".Length;
            var afterOk = after >= text.Length || char.IsWhiteSpace(text[after]);
            if (before >= 0 && text[before] == '>' && afterOk)
                return at;

            index = at + "stream".Length;
        }

        return -1;
    }

    private static int? ReadStreamLength(string dict)
    {
        var match = System.Text.RegularExpressions.Regex.Match(dict, @"/Length\s+(\d+)");
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var length) ? length : null;
    }

    private static byte[]? TryInflate(byte[] raw)
    {
        while (raw.Length > 0 && raw[^1] is (byte)'\n' or (byte)'\r' or 0)
            raw = raw[..^1];

        var zlib = Decompress(raw, true);
        if (zlib is { Length: > 0 })
            return zlib;

        if (raw.Length > 2)
        {
            var deflate = Decompress(raw[2..], false);
            if (deflate is { Length: > 0 })
                return deflate;
        }

        return Decompress(raw, false);
    }

    private static byte[]? Decompress(byte[] raw, bool zlib)
    {
        try
        {
            using var input = new MemoryStream(raw);
            using Stream source = zlib ? new ZLibStream(input, CompressionMode.Decompress) : new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            source.CopyTo(output);
            return output.ToArray();
        }
        catch (Exception)
        {
            return null;
        }
    }

    private static void CollectText(byte[] stream, List<TextChunk> chunks)
    {
        var tokens = Tokenize(Encoding.Latin1.GetString(stream));
        var operands = new List<PdfOperand>();
        double x = 0;
        double y = 0;
        double lineStart = 0;
        double leading = 12;
        foreach (var token in tokens)
        {
            if (!IsOperator(token))
            {
                operands.Add(ParseOperand(token));
                continue;
            }

            ApplyOperator(token, operands, chunks, ref x, ref y, ref lineStart, ref leading);
            operands.Clear();
        }
    }

    private static void ApplyOperator(string op, List<PdfOperand> operands, List<TextChunk> chunks, ref double x, ref double y, ref double lineStart, ref double leading)
    {
        if (op is "Tj" && LastString(operands) is { } shown)
        {
            AddChunk(chunks, x, y, shown);
            return;
        }

        if (op is "'" && LastString(operands) is { } nextLine)
        {
            y -= leading;
            x = lineStart;
            AddChunk(chunks, x, y, nextLine);
            return;
        }

        if (op is "T*")
        {
            y -= leading;
            x = lineStart;
            return;
        }

        if (op is "Td" or "TD" && operands.Count >= 2 && operands[^2].Number is { } dx && operands[^1].Number is { } dy)
        {
            x += dx;
            y += dy;
            lineStart = x;
            if (op == "TD" && dy < 0)
                leading = -dy;
            return;
        }

        if (op == "Tm" && operands.Count >= 6 && operands[^2].Number is { } textX && operands[^1].Number is { } textY)
        {
            x = textX;
            y = textY;
            lineStart = x;
            return;
        }

        if (op == "TJ")
        {
            foreach (var operand in operands)
            {
                if (operand.Text == null)
                    continue;

                AddChunk(chunks, x, y, operand.Text);
                x += operand.Text.Length * 4;
            }
        }
    }

    private static void AddChunk(List<TextChunk> chunks, double x, double y, string text)
    {
        var cleaned = DecodePdfText(text.Replace("\0", string.Empty)).Trim();
        if (cleaned.Length == 0)
            return;

        chunks.Add(new TextChunk(x, y, cleaned));
    }

    private static string DecodePdfText(string value)
    {
        if (value.Length < 2 || value[0] != '\u00FE' || value[1] != '\u00FF')
            return value;

        var bytes = new byte[value.Length - 2];
        for (var index = 2; index < value.Length; index++)
            bytes[index - 2] = (byte)value[index];

        return Encoding.BigEndianUnicode.GetString(bytes);
    }

    private static string? LastString(List<PdfOperand> operands)
    {
        for (var i = operands.Count - 1; i >= 0; i--)
        {
            if (operands[i].Text != null)
                return operands[i].Text;
        }

        return null;
    }

    private static List<string> GroupLines(List<TextChunk> chunks)
    {
        var lines = new List<string>();
        var current = new List<TextChunk>();
        double? lineY = null;
        foreach (var chunk in chunks.OrderByDescending(item => item.Y).ThenBy(item => item.X))
        {
            if (lineY.HasValue && Math.Abs(chunk.Y - lineY.Value) > 2)
            {
                lines.Add(JoinChunks(current));
                current.Clear();
            }

            if (current.Count == 0)
                lineY = chunk.Y;

            current.Add(chunk);
        }

        if (current.Count > 0)
            lines.Add(JoinChunks(current));

        return lines.Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
    }

    private static string JoinChunks(List<TextChunk> chunks)
    {
        var ordered = chunks.OrderBy(item => item.X).ToList();
        var text = new StringBuilder(ordered[0].Text);
        var cursor = ordered[0].X + Math.Max(ordered[0].Text.Length * 4.5, 4);
        for (var index = 1; index < ordered.Count; index++)
        {
            if (ordered[index].X - cursor > 1.5)
                text.Append(' ');

            text.Append(ordered[index].Text);
            cursor = Math.Max(cursor, ordered[index].X) + Math.Max(ordered[index].Text.Length * 4.5, 4);
        }

        return text.ToString();
    }

    private static bool IsOperator(string token) => token is "Tj" or "TJ" or "Td" or "TD" or "Tm" or "T*" or "'" or "\"" or "Tf" or "BT" or "ET";

    private static PdfOperand ParseOperand(string token)
    {
        if (token.StartsWith('\u0001'))
            return new PdfOperand(null, token[1..]);

        if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out var number))
            return new PdfOperand(number, null);

        return new PdfOperand(null, token);
    }

    private static List<string> Tokenize(string content)
    {
        var tokens = new List<string>();
        var index = 0;
        while (index < content.Length)
        {
            var current = content[index];
            if (char.IsWhiteSpace(current))
            {
                index++;
                continue;
            }

            if (current == '%')
            {
                while (index < content.Length && content[index] != '\n')
                    index++;
                continue;
            }

            if (current == '(')
            {
                tokens.Add("\u0001" + ReadLiteral(content, ref index));
                continue;
            }

            if (current == '<' && index + 1 < content.Length && content[index + 1] != '<')
            {
                tokens.Add("\u0001" + ReadHex(content, ref index));
                continue;
            }

            if (current is '[' or ']' or '>' )
            {
                index++;
                continue;
            }

            var start = index;
            while (index < content.Length && !char.IsWhiteSpace(content[index]) && content[index] is not '(' and not '<' and not '[' and not ']')
                index++;

            if (index == start)
                index++;

            if (index > start)
                tokens.Add(content[start..index]);
        }

        return tokens;
    }

    private static string ReadLiteral(string content, ref int index)
    {
        index++;
        var value = new StringBuilder();
        var depth = 1;
        while (index < content.Length && depth > 0)
        {
            var current = content[index++];
            if (current == '\\' && index < content.Length)
            {
                var escaped = content[index++];
                value.Append(escaped switch
                {
                    'n' => '\n',
                    'r' => '\r',
                    't' => '\t',
                    'b' => '\b',
                    'f' => '\f',
                    _ when escaped is >= '0' and <= '7' => ReadOctal(content, escaped, ref index),
                    _ => escaped
                });
                continue;
            }

            if (current == '(')
                depth++;
            else if (current == ')')
            {
                depth--;
                if (depth == 0)
                    break;
            }

            value.Append(current);
        }

        return value.ToString();
    }

    private static char ReadOctal(string content, char first, ref int index)
    {
        var digits = new StringBuilder().Append(first);
        while (digits.Length < 3 && index < content.Length && content[index] is >= '0' and <= '7')
            digits.Append(content[index++]);

        return (char)Convert.ToInt32(digits.ToString(), 8);
    }

    private static string ReadHex(string content, ref int index)
    {
        index++;
        var start = index;
        while (index < content.Length && content[index] != '>')
            index++;

        var hex = content[start..index];
        if (index < content.Length)
            index++;

        var bytes = new List<byte>();
        for (var i = 0; i + 1 < hex.Length; i += 2)
        {
            if (byte.TryParse(hex.AsSpan(i, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
                bytes.Add(value);
        }

        return Encoding.Latin1.GetString(bytes.ToArray());
    }

    private static PdfEncryption? TryGetEncryption(byte[] content)
    {
        var text = Encoding.Latin1.GetString(content);
        var encryptAt = text.IndexOf("/Type /Encrypt", StringComparison.Ordinal);
        if (encryptAt < 0 || !Regex.IsMatch(text, @"/Filter\s*/Standard"))
            return null;

        var dictStart = text.LastIndexOf("<<", encryptAt, StringComparison.Ordinal);
        var dictEnd = text.IndexOf(">>", encryptAt, StringComparison.Ordinal);
        if (dictStart < 0 || dictEnd < 0)
            return null;

        var dict = text[dictStart..dictEnd];
        var revision = MatchInt(dict, @"/R\s+(\d+)");
        var lengthBits = MatchInt(dict, @"/Length\s+(\d+)");
        var permissions = MatchInt(dict, @"/P\s+(-?\d+)");
        if (revision is not 2 || lengthBits is not > 0 || permissions == null)
            return null;

        var user = ReadNamedLiteral(dict, "/U");
        var owner = ReadNamedLiteral(dict, "/O");
        var fileId = ReadFileId(text);
        if (user == null || owner == null || fileId == null || user.Length < 32 || owner.Length < 32)
            return null;

        var keyLength = lengthBits.Value / 8;
        var key = ComputeEncryptionKey(PadPassword([]), owner.AsSpan(0, 32).ToArray(), permissions.Value, fileId, keyLength, revision.Value);
        var expected = Rc4(key, PasswordPadding);
        if (!expected.AsSpan(0, 32).SequenceEqual(user.AsSpan(0, 32)))
            return null;

        var encryptObject = FindObjectId(text, encryptAt);
        return new PdfEncryption(key, encryptObject.Number);
    }

    private static byte[] ComputeEncryptionKey(byte[] password, byte[] owner, int permissions, byte[] fileId, int keyLength, int revision)
    {
        var material = new byte[password.Length + owner.Length + 4 + fileId.Length];
        Buffer.BlockCopy(password, 0, material, 0, password.Length);
        Buffer.BlockCopy(owner, 0, material, password.Length, owner.Length);
        material[password.Length + owner.Length] = (byte)permissions;
        material[password.Length + owner.Length + 1] = (byte)(permissions >> 8);
        material[password.Length + owner.Length + 2] = (byte)(permissions >> 16);
        material[password.Length + owner.Length + 3] = (byte)(permissions >> 24);
        Buffer.BlockCopy(fileId, 0, material, password.Length + owner.Length + 4, fileId.Length);
        var hash = MD5.HashData(material);
        if (revision >= 3)
        {
            for (var round = 0; round < 50; round++)
                hash = MD5.HashData(hash.AsSpan(0, keyLength).ToArray());
        }

        return hash.AsSpan(0, keyLength).ToArray();
    }

    private static byte[] PadPassword(byte[] password)
    {
        var padded = new byte[32];
        var count = Math.Min(password.Length, 32);
        Buffer.BlockCopy(password, 0, padded, 0, count);
        Buffer.BlockCopy(PasswordPadding, 0, padded, count, 32 - count);
        return padded;
    }

    private static byte[] DecryptStream(byte[] data, byte[] key, int objectNumber, int generation)
    {
        var extended = new byte[key.Length + 5];
        Buffer.BlockCopy(key, 0, extended, 0, key.Length);
        extended[key.Length] = (byte)objectNumber;
        extended[key.Length + 1] = (byte)(objectNumber >> 8);
        extended[key.Length + 2] = (byte)(objectNumber >> 16);
        extended[key.Length + 3] = (byte)generation;
        extended[key.Length + 4] = (byte)(generation >> 8);
        var hash = MD5.HashData(extended);
        var objectKey = hash.AsSpan(0, Math.Min(key.Length + 5, 16)).ToArray();
        return Rc4(objectKey, data);
    }

    private static byte[] Rc4(byte[] key, byte[] data)
    {
        var state = new byte[256];
        for (var index = 0; index < 256; index++)
            state[index] = (byte)index;

        var swap = 0;
        for (var index = 0; index < 256; index++)
        {
            swap = (swap + state[index] + key[index % key.Length]) & 255;
            (state[index], state[swap]) = (state[swap], state[index]);
        }

        var left = 0;
        var right = 0;
        var output = new byte[data.Length];
        for (var index = 0; index < data.Length; index++)
        {
            left = (left + 1) & 255;
            right = (right + state[left]) & 255;
            (state[left], state[right]) = (state[right], state[left]);
            output[index] = (byte)(data[index] ^ state[(state[left] + state[right]) & 255]);
        }

        return output;
    }

    private static (int Number, int Generation) FindObjectId(string text, int position)
    {
        var start = Math.Max(0, position - 5000);
        var window = text[start..position];
        var matches = Regex.Matches(window, @"(\d+)\s+(\d+)\s+obj");
        if (matches.Count == 0)
            return (0, 0);

        var match = matches[^1];
        return (int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture), int.Parse(match.Groups[2].Value, CultureInfo.InvariantCulture));
    }

    private static int? MatchInt(string text, string pattern)
    {
        var match = Regex.Match(text, pattern);
        return match.Success && int.TryParse(match.Groups[1].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var value) ? value : null;
    }

    private static byte[]? ReadFileId(string text)
    {
        var match = Regex.Match(text, @"/ID\s*\[\s*<([0-9A-Fa-f]+)>");
        if (!match.Success || match.Groups[1].Value.Length % 2 != 0)
            return null;

        var hex = match.Groups[1].Value;
        var bytes = new byte[hex.Length / 2];
        for (var index = 0; index < bytes.Length; index++)
            bytes[index] = byte.Parse(hex.AsSpan(index * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);

        return bytes;
    }

    private static byte[]? ReadNamedLiteral(string dict, string name)
    {
        var at = dict.IndexOf(name, StringComparison.Ordinal);
        if (at < 0)
            return null;

        var open = dict.IndexOf('(', at + name.Length);
        if (open < 0)
            return null;

        return ReadLiteralBytes(dict, open);
    }

    private static byte[] ReadLiteralBytes(string content, int openIndex)
    {
        var index = openIndex + 1;
        var value = new List<byte>();
        var depth = 1;
        while (index < content.Length && depth > 0)
        {
            var current = (byte)content[index++];
            if (current == (byte)'\\' && index < content.Length)
            {
                var escaped = (byte)content[index++];
                value.Add(escaped switch
                {
                    (byte)'n' => (byte)'\n',
                    (byte)'r' => (byte)'\r',
                    (byte)'t' => (byte)'\t',
                    (byte)'b' => (byte)'\b',
                    (byte)'f' => (byte)'\f',
                    >= (byte)'0' and <= (byte)'7' => ReadOctalByte(content, escaped, ref index),
                    _ => escaped
                });
                continue;
            }

            if (current == (byte)'(')
                depth++;
            else if (current == (byte)')')
            {
                depth--;
                if (depth == 0)
                    break;
            }

            value.Add(current);
        }

        return value.ToArray();
    }

    private static byte ReadOctalByte(string content, byte first, ref int index)
    {
        var digits = new StringBuilder().Append((char)first);
        while (digits.Length < 3 && index < content.Length && content[index] is >= '0' and <= '7')
            digits.Append(content[index++]);

        return (byte)Convert.ToInt32(digits.ToString(), 8);
    }

    private static readonly byte[] PasswordPadding =
    [
        0x28, 0xBF, 0x4E, 0x5E, 0x4E, 0x75, 0x8A, 0x41, 0x64, 0x00, 0x4E, 0x56,
        0xFF, 0xFA, 0x01, 0x08, 0x2E, 0x2E, 0x00, 0xB6, 0xD0, 0x68, 0x3E, 0x80,
        0x2F, 0x0C, 0xA9, 0xFE, 0x64, 0x53, 0x69, 0x7A
    ];

    private sealed class PdfEncryption
    {
        public PdfEncryption(byte[] key, int encryptObjectNumber)
        {
            Key = key;
            EncryptObjectNumber = encryptObjectNumber;
        }

        public byte[] Key { get; }

        public int EncryptObjectNumber { get; }
    }

    private readonly record struct TextChunk(double X, double Y, string Text);

    private readonly record struct PdfOperand(double? Number, string? Text);
}
