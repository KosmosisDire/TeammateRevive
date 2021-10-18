using System;

namespace TeammateRevive.Common
{
    public static class FloatExtensions
    {
        public static float Truncate(this float value, int digits)
        {
            double mult = Math.Pow(10.0, digits);
            double result = Math.Truncate( mult * value ) / mult;
            return (float) result;
        }
    }
}