using System.Collections.Generic;


/// <summary>
/// Состояние модального окна редактирования
/// </summary>
public sealed class ModalState
{
    public string CurrentTable;
    public string CurrentOperation;
    public List<string> Columns;
    public Dictionary<string, object> InitialData;
    public string ParentKey;
    public object ParentValue;
    public bool IsEditMode;
    
    public ModalState()
    {
        Columns = new List<string>();
        InitialData = new Dictionary<string, object>();
    }
}
