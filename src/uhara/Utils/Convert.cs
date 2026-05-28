using System;
using System.Globalization;

internal class TConvert
{
    public static T Parse<T>(string number, bool forceHex = false)
    {
        try
        {
            if (string.IsNullOrEmpty(number))
                return default;

            NumberStyles numberStyle = NumberStyles.Number;
            number = number.ToLower().Replace(",", ".").Replace(" ", "");

            int multiplier = 1;
            if (number[0] == '-')
            {
                multiplier = -1;
                number = number[1..];
            }
            else if (number[0] == '+')
            {
                number = number[1..];
            }

            if (number.StartsWith("0x"))
            {
                numberStyle = NumberStyles.HexNumber;
                number = number.Replace("0x", "");
            }

            if (forceHex)
                numberStyle = NumberStyles.HexNumber;

            if (typeof(T) == typeof(bool))
            {
                if (bool.TryParse(number, out var val))
                    return (T)(object)val;
            }
            else if (typeof(T) == typeof(sbyte))
            {
                if (sbyte.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(sbyte)(val * multiplier);
            }
            else if (typeof(T) == typeof(byte))
            {
                if (byte.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(byte)(val * multiplier);
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(val * multiplier);
            }
            else if (typeof(T) == typeof(uint))
            {
                if (uint.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(uint)(val * multiplier);
            }
            else if (typeof(T) == typeof(long))
            {
                if (long.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(val * multiplier);
            }
            else if (typeof(T) == typeof(ulong))
            {
                if (ulong.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(ulong)((long)val * multiplier);
            }
            else if (typeof(T) == typeof(float))
            {
                if (float.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(float)(val * multiplier);
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(number, numberStyle, CultureInfo.InvariantCulture, out var val))
                    return (T)(object)(double)(val * multiplier);
            }
        }
        catch { }

        return (T)Convert.ChangeType(0, typeof(T));
    }
}
