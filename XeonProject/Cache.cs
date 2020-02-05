using XeonStorage;

namespace XeonProject
{
    public static class Storage
    {
        public static Cache Cache = new Cache(1000 * 60, Program.AppDir, "WorldCache.json");
    }
}