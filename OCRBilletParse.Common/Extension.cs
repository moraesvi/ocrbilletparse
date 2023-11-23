namespace OCRBilletParse.Common;
public static class Extension
{
    //.Net 7 Linq has been really improving - performance and memory allocation
    public static string OnlyNumbers(this string value)
    {
        string s = new string(value.Where(v => char.IsDigit(v) || v == '.' || v == ',').ToArray());
        return s;
    }
    //.Net 7 Linq has been really improving - performance and memory allocation
    public static string OnlyChars(this string value)
    {
        string s = new string(value.Where(v => !char.IsDigit(v) && v != '.' && v != ',' && v != '(' && v != ')').ToArray());
        return s;
    }
    //.Net 7 Linq has been really improving - performance and memory allocation
    public static int CountChars(this string value)
    {
        int count = value.Count(char.IsLetter);
        return count;
    }
    public static string RemoveSpecialChars(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        Span<char> buffer = stackalloc char[value.Length];
        int idx = 0;

        foreach (char c in value)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
                || (c >= 'a' && c <= 'z') || (c == '.') || (c == '_'))
            {
                buffer[idx] = c;
                idx++;
            }
        }

        return new string(buffer.ToArray(), 0, idx);
    }
    public static bool IsNumber(this string value)
    {
        if (decimal.TryParse(value, out _))
            return true;

        if (string.IsNullOrEmpty(value) || string.Equals(value, ".") || string.Equals(value, ",") || value.Contains("-") || value.Contains(".."))
            return false;

        bool isNum = value.All(c => !char.IsLetter(c) && !char.IsSymbol(c));
        return isNum;
    }
    public static bool AllChars(this string value)
    {
        bool all = value.All(char.IsLetter);
        return all;
    }
    public static bool HasNumber(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Any(char.IsNumber);
    }
    public static bool HasDecimalPlaces(this object value) 
    {
        if (value == null)
            return false;
        if (!IsNumber(value.ToString()))
            return false;

        return value.ToString().IndexOf(".") >= 0 || value.ToString().IndexOf(",") >= 0;
    }
    public static bool HasParentheses(this string value) 
    {
        return value.Contains("(") || value.Contains(")");
    }
    public static bool HasChar(this string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        return value.Any(char.IsLetter);
    }
    //Prevent null exception
    public static string GetArrayValue(this string[] value, int index)
    {
        if (value == null)
            return string.Empty;

        string elValue = value.ElementAtOrDefault(index);
        if (!string.IsNullOrWhiteSpace(elValue))
            return elValue;

        return string.Empty;
    }
}
