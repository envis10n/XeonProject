using XeonCore.Vector;
using Newtonsoft.Json;

namespace XeonCore
{
    public class Entity
    {
        public Vec2D Location;
        public Entity()
        {
            Location = new Vec2D();
        }
        public override string ToString()
        {
            return $"[{this.GetType().FullName}]\n" + JsonConvert.SerializeObject(this, Formatting.Indented);
        }
    }
}
