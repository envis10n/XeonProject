using System.IO;
using System.Threading.Tasks;

namespace XeonStorage
{
    public static class IO
    {
        public static async Task<byte[]> ReadFile(params string[] pathSegments)
        {
            string path = Path.Join(pathSegments);
            using (FileStream sr = File.OpenRead(path))
            {
                byte[] buffer = new byte[sr.Length];
                await sr.ReadAsync(buffer, 0, (int)sr.Length);
                return buffer;
            }
        }
        public static async Task WriteFile(byte[] data, params string[] pathSegments)
        {
            string path = Path.Join(pathSegments);
            using (FileStream sr = File.OpenWrite(path))
            {
                await sr.WriteAsync(data, 0, data.Length);
            }
        }
    }
}