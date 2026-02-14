using System.Collections.Generic;
using Newtonsoft.Json.Linq;

/// <summary>
/// Метаданные формы для модального окна редактирования
/// Содержит информацию о полях и их типах
/// </summary>
public sealed class FormMetadata
{
    /// <summary>
    /// Информация о поле формы
    /// </summary>
    public sealed class Field
    {
        public string Name;
        public string Type; // "text" | "select" | "date" | ...
        public List<DropdownOption> Options; // только для select
        public string DisplayName;
        public string Hint;
    }

    public Dictionary<string, Field> FieldsByName = new();

    public bool TryGetField(string name, out Field field) => FieldsByName.TryGetValue(name, out field);

    /// <summary>
    /// Создает FormMetadata из сырых данных с сервера
    /// </summary>
    public static FormMetadata FromRaw(Dictionary<string, object> raw)
    {
        var fm = new FormMetadata();
        if (raw == null) return fm;
        
        foreach (var kv in raw)
        {
            var jObj = kv.Value as JObject;
            if (jObj == null) continue;
            
            var f = new Field
            {
                Name = kv.Key,
                Type = jObj["type"]?.ToString(),
                Options = new List<DropdownOption>(),
                DisplayName = jObj["displayName"]?.ToString(),
                Hint = jObj["hint"]?.ToString()
            };
            
            if (f.Type == "select")
            {
                var arr = jObj["options"] as JArray;
                if (arr != null)
                {
                    foreach (var o in arr)
                    {
                        var oj = o as JObject;
                        if (oj == null) continue;
                        
                        var id = oj["id"]?.ToString();
                        var label = oj["label"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            f.Options.Add(new DropdownOption { Id = id, Label = label ?? id });
                        }
                    }
                }
            }
            
            fm.FieldsByName[f.Name] = f;
        }
        
        return fm;
    }
}
