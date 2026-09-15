using System;
using System.Globalization;

/// <summary>Formats money the way the HUD shows it: "$7", "$42.0", "$1k", "$101.7k", "$3.2M".</summary>
public static class MoneyFormatter
{
    #region Private Fields
    private static readonly string[] Suffixes = { "", "k", "M", "B", "T", "aa", "ab", "ac", "ad" };
    #endregion

    #region Public Methods
    /// <param name="wholeDollars">Below $1000, drop the decimal ("$7" instead of "$7.0").</param>
    public static string Format(double value, bool wholeDollars = false)
    {
        double magnitude = Math.Abs(value);
        int tier = 0;

        // Fix: round before comparing so $999.96 becomes "$1k" rather than "$1000.0".
        while (tier < Suffixes.Length - 1 && Math.Round(magnitude, DecimalsFor(tier, wholeDollars)) >= 1000d)
        {
            magnitude /= 1000d;
            tier++;
        }

        string digits = magnitude.ToString(PatternFor(tier, wholeDollars), CultureInfo.InvariantCulture);
        return (value < 0d ? "-$" : "$") + digits + Suffixes[tier];
    }
    #endregion

    #region Private Methods
    private static int DecimalsFor(int tier, bool wholeDollars) => tier == 0 && wholeDollars ? 0 : 1;

    /// <summary>Plain dollars keep one decimal ("$42.0"); suffixed amounts drop a trailing zero ("$1k").</summary>
    private static string PatternFor(int tier, bool wholeDollars)
    {
        if (tier > 0)
            return "0.#";

        return wholeDollars ? "0" : "0.0";
    }
    #endregion
}
