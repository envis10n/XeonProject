using System.IO;
using System.Threading.Tasks;

namespace XeonCommon.IO
{
    public static class File
    {
        public class Async
        {
            public static async Task<byte[]> ReadFile(string path)
            {
                using (FileStream sr = System.IO.File.OpenRead(path))
                {
                    byte[] buffer = new byte[sr.Length];
                    await sr.ReadAsync(buffer, 0, buffer.Length);
                    return buffer;
                }
            }
            public static async Task OverwriteFile(byte[] data, string path)
            {
                using (FileStream sr = System.IO.File.Create(path))
                {
                    await sr.WriteAsync(data, 0, data.Length);
                }
            }
            public static async Task AppendFile(byte[] data, string path)
            {
                using (FileStream sr = System.IO.File.OpenWrite(path))
                {
                    await sr.WriteAsync(data, 0, data.Length);
                }
            }
        }
        public class Sync
        {
            public static byte[] ReadFile(string path)
            {
                using (FileStream sr = System.IO.File.OpenRead(path))
                {
                    byte[] buffer = new byte[sr.Length];
                    sr.Read(buffer, 0, buffer.Length);
                    return buffer;
                }
            }

            public static void OverwriteFile(byte[] data, string path)
            {
                using (FileStream sr = System.IO.File.Create(path))
                {
                    sr.Write(data, 0, data.Length);
                }
            }

            public static void AppendFile(byte[] data, string path)
            {
                using (FileStream sr = System.IO.File.OpenWrite(path))
                {
                    sr.Write(data, 0, data.Length);
                }
            }
        }
    }
    public static class Directory
    {
        public static bool Exists(string path)
        {
            return System.IO.Directory.Exists(path);
        }
        public static DirectoryInfo Create(string path)
        {
            if (Exists(path))
            {
                return null;
            }
            else
            {
                return System.IO.Directory.CreateDirectory(path);
            }
        }
        public static string[] GetFiles(string path)
        {
            return System.IO.Directory.GetFiles(path);
        }
    }
}