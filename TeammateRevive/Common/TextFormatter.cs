namespace TeammateRevive.Common
{
    public static class TextFormatter
    {
        public static string Colored(this string text, string color)
        {
            return $"<color={color}>{text}</color>";
        }

        public static string Red(string text) => text.Colored("\"red\"");
        public static string Yellow(string text) => text.Colored("\"yellow\"");
        public static string Green(string text) => text.Colored("\"green\"");
        public static string Blue(string text) => text.Colored("\"blue\"");
    }
}