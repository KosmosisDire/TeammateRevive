public static class StructExts
{
    public static T Clone<T> ( this T val ) where T : struct => (T)val;
}