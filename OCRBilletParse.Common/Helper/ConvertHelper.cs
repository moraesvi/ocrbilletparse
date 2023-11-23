using System.Globalization;

namespace OCRBilletParse.Common;
public class ConvertHelper
{
    /// <summary>
    /// Prevent exception on invalid parse case
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static short ToInt16(object value)
    {
        if (value == null)
            return default;

        bool ok = short.TryParse(value.ToString(), out short decimalValue);
        if (ok)
            return decimalValue;
        return default;
    }
    /// <summary>
    /// Prevent exception on invalid parse case
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static float ToFloat(object value)
    {
        if (value == null)
            return default;

        bool ok = float.TryParse(value.ToString().Replace(",", "."), CultureInfo.InvariantCulture, out float floatValue);
        if (ok)
            return floatValue;
        return default;
    }
    /// <summary>
    /// Prevent exception on invalid parse case
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static decimal ToDecimal(object value)
    {
        if (value == null)
            return default;

        bool ok = decimal.TryParse(value.ToString().Replace(",", "."), CultureInfo.InvariantCulture, out decimal decimalValue);
        if (ok)
            return decimalValue;
        return default;
    }
}
