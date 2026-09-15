using System;
using System.Globalization;

/// <summary>Formats money the way the HUD shows it: "$7", "$42.0", "$101.7k", "$3.2M".</summary>
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

        while (magnitude >= 1000d && tier < Suffixes.Length - 1)
        {
            magnitude /= 1000d;
            tier++;
        }

        string digits = tier == 0 && wholeDollars
            ? magnitude.ToString("0", CultureInfo.InvariantCulture)
            : magnitude.ToString("0.0", CultureInfo.InvariantCulture);

        return (value < 0d ? "-$" : "$") + digits + Suffixes[tier];
    }
    #endregion
}
