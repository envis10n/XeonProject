using XeonStorage;
using System.IO;

namespace XeonProject
{
    public static class Storage
    {
        public static Cache Cache;
        public static void LoadCache()
        {
            string path = Program.Config.Cache.Path;
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, Program.AppDir);
            }
            Cache = new Cache(1000 * 60, path);
        }
    }
}