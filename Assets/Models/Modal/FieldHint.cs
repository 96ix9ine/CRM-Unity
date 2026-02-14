/// <summary>
/// Подсказка для поля формы
/// </summary>
public sealed class FieldHint
{
    public string FieldName;
    public string HintText;
    
    public FieldHint(string fieldName, string hintText)
    {
        FieldName = fieldName;
        HintText = hintText;
    }
}
