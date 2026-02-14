/// <summary>
/// Опция для выпадающего списка
/// </summary>
public struct DropdownOption
{
    public string Id;
    public string Label;
    
    public DropdownOption(string id, string label)
    {
        Id = id;
        Label = label;
    }
}
