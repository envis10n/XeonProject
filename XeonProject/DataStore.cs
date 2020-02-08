using XeonStorage;
using System.IO;

namespace XeonProject
{
    public static class Storage
    {
        public static DataStore DataStore;
        public static void LoadDataStore()
        {
            string path = Program.Config.DataStore.Path;
            if (!Path.IsPathRooted(path))
            {
                path = Path.GetFullPath(path, Program.AppDir);
            }
            DataStore = new DataStore(1000 * 60, path);
        }
    }
}