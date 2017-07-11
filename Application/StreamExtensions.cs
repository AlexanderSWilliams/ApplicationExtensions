using System.IO;

namespace Application.StreamExtensions
{
    public static class StreamExtensions
    {
        public static Stream MakeEmptyNull(this Stream stream)
        {
            return stream.CanSeek ? (stream.Length > 0 ? stream : null) : null;
        }

        public static byte[] ToArrayFromBuffer(this Stream stream)
        {
            using (var memoryStream = new MemoryStream())
            {
                byte[] buffer = new byte[8192];
                int bytesRead;
                while ((bytesRead = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    memoryStream.Write(buffer, 0, bytesRead);
                }
                return memoryStream.ToArray();
            }
        }

        public static string ToStringFromBOM(this Stream stream)
        {
            if (stream == null) return null;
            if (stream.CanSeek)
                stream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(stream, true))
            {
                return reader.ReadToEnd();
            }
        }
    }
}