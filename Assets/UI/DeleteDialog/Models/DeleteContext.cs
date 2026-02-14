using System;
using System.Collections.Generic;

/// <summary>
/// Контекст удаления для DeleteDialogController
/// </summary>
[Serializable]
public class DeleteContext
{
    public string TableName;
    public string NodeType;
    public string Id;
    public Dictionary<string, object> Extra;
}