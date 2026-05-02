using System;
using TaleWorlds.Core;
using TaleWorlds.Library;

namespace RealmsForgotten.Utility
{
    public static class RFMaths
    {
        static Random _random = new();
        public static Vec2 GetRandomPointOnLine(Vec2 pointA, Vec2 pointB)
        {
            var lineA = (pointB.y - pointA.y) / (pointB.x - pointA.x);
            var lineB = pointA.y - pointA.x * lineA;
            var xValue = _random.NextFloat();
            xValue *= Math.Abs(pointB.x - pointA.x);
            xValue += Math.Min(pointA.x, pointB.x);
            var yValue = xValue * lineA + lineB;
            return new Vec2(xValue, yValue);
        }
    }
}
