using System;
using System.IO;

namespace Application.StreamExtensions
{
    public static class StreamExtensions
    {
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
    }
}