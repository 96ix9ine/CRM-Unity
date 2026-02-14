using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Реализация политики валидации записей
/// </summary>
public sealed class RecordValidationPolicy : IRecordValidationPolicy
{
    public bool Validate(string tableName, Dictionary<string, object> data, out string error)
    {
        error = null;
        
        switch (tableName)
        {
            case "components":
                return ValidateAllOrTitleAndParent(data, ComponentsRequired(), out error);
            case "pcbs":
                return ValidateAllOrTitleAndParent(data, new[] { "componentId", "standId" }, out error);
            case "stands":
                return ValidateAllOrTitleAndParent(data, StandsRequired(), out error);
            default:
                return true;
        }
    }

    private static bool ValidateAllOrTitleAndParent(Dictionary<string, object> data, IEnumerable<string> required, out string error)
    {
        error = null;
        
        // Проверяем, есть ли дополнительные поля (кроме title и parentId)
        bool hasAdditional = required.Any(f => data.TryGetValue(f, out var v) && !string.IsNullOrEmpty(v?.ToString()));
        if (!hasAdditional) return true;

        // Если есть дополнительные поля, проверяем все обязательные
        var missing = required.Where(f => !data.TryGetValue(f, out var v) || string.IsNullOrEmpty(v?.ToString())).ToList();
        if (missing.Count > 0)
        {
            error = "Заполните обязательные поля: " + string.Join(", ", missing);
            return false;
        }
        
        return true;
    }

    private static IEnumerable<string> ComponentsRequired() =>
        new[] { "photo", "width", "height", "thickness", "weight", "material", "receiptDate", "drawingReference", "placementId" };

    private static IEnumerable<string> StandsRequired() =>
        new[] { "image", "width", "height", "thickness", "weightNetto", "weightBrutto", "link", "vendorCode", "manufactureTime", "comment", "employeeId", "standTypeId" };
}
