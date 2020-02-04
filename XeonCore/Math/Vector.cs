using System;
using Newtonsoft.Json;

namespace XeonCore
{
    namespace Vector
    {
        public struct Vec2D
        {
            public int X;
            public int Y;
            public Vec2D(int x = 0, int y = 0)
            {
                X = x;
                Y = y;
            }
            public override string ToString()
            {
                return $"Vec2D [{X}, {Y}]";
            }
            public int Manhattan(Vec2D b)
            {
                return Math.Abs(X - b.X) + Math.Abs(Y - b.Y);
            }
            public Vec2D Scale(float scale)
            {
                X = (int)scale * X;
                Y = (int)scale * Y;
                return this;
            }
            public static Vec2D operator +(Vec2D a, Vec2D b)
            {
                return new Vec2D(a.X + b.X, a.Y + b.Y);
            }
            public static Vec2D operator -(Vec2D a, Vec2D b)
            {
                return new Vec2D(a.X - b.X, a.Y - b.Y);
            }
            public static Vec2D operator *(Vec2D a, Vec2D b)
            {
                return new Vec2D(a.X * b.X, a.Y * b.Y);
            }
            public static Vec2D operator /(Vec2D a, Vec2D b)
            {
                return new Vec2D(a.X / b.X, a.Y / b.Y);
            }
            public static int Manhattan(Vec2D a, Vec2D b)
            {
                return Math.Abs(a.X - b.X) + Math.Abs(a.Y - b.Y);
            }
        }
    }
}
