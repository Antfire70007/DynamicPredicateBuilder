namespace DynamicPredicateBuilder.Core;

public static class LikeHelper
{
    public enum LikeMode
    {
        Contains,
        StartsWith,
        EndsWith
    }

    public static LikeMode GetLikeMode(string pattern, out string cleanPattern)
    {
        cleanPattern = pattern;

        var startsWith = pattern.StartsWith("%");
        var endsWith = pattern.EndsWith("%");

        if (startsWith && endsWith)
        {
            cleanPattern = pattern.Trim('%');
            return LikeMode.Contains;
        }
        if (startsWith)
        {
            cleanPattern = pattern.TrimStart('%');
            return LikeMode.EndsWith;
        }
        if (endsWith)
        {
            cleanPattern = pattern.TrimEnd('%');
            return LikeMode.StartsWith;
        }

        return LikeMode.Contains;
    }
}