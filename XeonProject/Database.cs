using XeonStorage;
using System.IO;

namespace XeonProject
{
    public static class Storage
    {
        public static Database Database = new Database(
            !Path.IsPathRooted(Program.Config.DataStore.Path) ? 
            Path.GetFullPath(Program.Config.DataStore.Path, 
            Program.AppDir) : Program.Config.DataStore.Path,
            60 * 1000
        );
    }
}