using XeonStorage;
using System.IO;

namespace XeonProject
{
    static class DataStorage
    {
        public static Database Database = new Database(
            !Path.IsPathRooted(Program.Config.DataStore.Path) ? 
            Path.GetFullPath(Program.Config.DataStore.Path, 
            Program.AppDir) : Program.Config.DataStore.Path,
            60 * 1000
        );
        public static void Setup()
        {
            // Init new DB stuff here
            if (!Database.HasCollection("actors"))
            {
                Database.AddCollection("actors", out Collection actors);
            }
            Database.Save();
        }
    }
}