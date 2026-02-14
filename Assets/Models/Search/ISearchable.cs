/// <summary>
/// Интерфейс для реализации поиска в любом компоненте
/// Используется в SearchField для унификации API поиска
/// </summary>
public interface ISearchable
{
    void Search(string query);
    void ClearSearch();
} 