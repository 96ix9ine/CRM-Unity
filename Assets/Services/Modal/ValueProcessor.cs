using System.Globalization;

/// <summary>
/// Реализация сервиса для обработки значений полей
/// </summary>
public sealed class ValueProcessor : IValueProcessor
{
    public object Process(string fieldName, string value)
    {
        if (string.IsNullOrEmpty(value)) return null;

        var lower = fieldName.ToLower();
        
        // Обработка дат
        if (lower.Contains("date"))
        {
            if (System.DateTime.TryParse(value, out var dt))
                return dt.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        // Обработка числовых полей
        if (IsNumericField(lower))
        {
            if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var f)) 
                return f;
            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i)) 
                return i;
            return null;
        }

        return value;
    }

    private bool IsNumericField(string lower)
    {
        return lower.Contains("width") ||
               lower.Contains("height") ||
               lower.Contains("thickness") ||
               lower.Contains("weight") ||
               lower.Contains("count") ||
               lower.Contains("amount") ||
               lower.Contains("price") ||
               lower.Contains("quantity");
    }
}
