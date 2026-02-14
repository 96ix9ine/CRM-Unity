using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;
using Validation.Components;

/// <summary>
/// Стратегия для модального окна сотрудников
/// Объединяет поля из таблиц peoples, professions и employees
/// </summary>
public class EmployeeModalStrategy : IModalStrategy
{
    public string StrategyName => "сотрудника";
    
    private ActionsButtonsMainMenu actionsButtons;
    
    public EmployeeModalStrategy(ActionsButtonsMainMenu actionsButtons)
    {
        this.actionsButtons = actionsButtons;
    }
    
    public async void CreateFields(TableEditController modal, bool isEdit, Dictionary<string, object> data = null)
    {
        // Если это редактирование, загружаем связанные данные
        Dictionary<string, object> peopleData = null;
        Dictionary<string, object> professionData = null;
        
        if (isEdit && data != null && data.ContainsKey("id"))
        {
            string employeeId = data["id"].ToString();
            Debug.Log($"Редактирование сотрудника с ID: {employeeId}");
            
            // Загружаем данные человека
            if (data.ContainsKey("peopleId"))
            {
                string peopleId = data["peopleId"].ToString();
                peopleData = await LoadPeopleData(peopleId);
                Debug.Log($"Загружены данные человека: {JsonConvert.SerializeObject(peopleData)}");
            }
            
            // Загружаем данные профессии
            professionData = await LoadProfessionData(employeeId);
            if (professionData != null)
            {
                Debug.Log($"Загружены данные профессии: {JsonConvert.SerializeObject(professionData)}");
            }
            else
            {
                Debug.Log($"У сотрудника {employeeId} нет назначенной профессии");
                professionData = new Dictionary<string, object>();
                professionData["professionId"] = null;
            }
        }
        
        // Создаем разделитель для полей человека
        modal.CreateTableNameSeparator("peoples", modal.GetFieldsContainer());
        
        // Добавляем поля из таблицы peoples
        var peopleFields = new List<string> { "firstName", "lastName", "middleName", "phone", "email", "comment", "birthDate" };
        await modal.CreateFieldsForTable("peoples", peopleFields, modal.GetFieldsContainer(), isEdit, peopleData);

        // Явно помечаем обязательные поля
        if (modal.GetFieldRendererValidationExtension() != null)
        {
            modal.GetFieldRendererValidationExtension().SetupFieldValidation("firstName", modal.GetFieldsContainer(), true);
            modal.GetFieldRendererValidationExtension().SetupFieldValidation("lastName", modal.GetFieldsContainer(), true);
        }
        
        // Создаем разделитель для полей профессии
        modal.CreateTableNameSeparator("professions", modal.GetFieldsContainer());
        
        // Добавляем поле выбора профессии
        await modal.CreateProfessionDropdown(modal.GetFieldsContainer(), isEdit, professionData);
        
        // Создаем разделитель для полей сотрудника
        modal.CreateTableNameSeparator("employees", modal.GetFieldsContainer());
        
        // Добавляем поля из таблицы employees
        var employeeFields = new List<string> { "hiringDate", "dismissalDate" };
        await modal.CreateFieldsForTable("employees", employeeFields, modal.GetFieldsContainer(), isEdit, data);
    }
    
    public void HandleSave(TableEditController modal, Dictionary<string, object> data, System.Action onSuccess)
    {
        // Разделяем данные по таблицам
        var empData = new Dictionary<string, object>();
        var peopleData = new Dictionary<string, object>();
        string professionId = null;
        
        // Поля сотрудника
        if (data.ContainsKey("hiringDate")) empData["hiringDate"] = data["hiringDate"];
        if (data.ContainsKey("dismissalDate")) empData["dismissalDate"] = data["dismissalDate"];
        
        // Поля человека
        if (data.ContainsKey("firstName")) peopleData["firstName"] = data["firstName"];
        if (data.ContainsKey("lastName")) peopleData["lastName"] = data["lastName"];
        if (data.ContainsKey("middleName")) peopleData["middleName"] = data["middleName"];
        if (data.ContainsKey("phone")) peopleData["phone"] = data["phone"];
        if (data.ContainsKey("email")) peopleData["email"] = data["email"];
        if (data.ContainsKey("comment")) peopleData["comment"] = data["comment"];
        if (data.ContainsKey("birthDate")) peopleData["birthDate"] = data["birthDate"];
        
        // ID профессии
        if (data.ContainsKey("professionId")) professionId = data["professionId"]?.ToString();
        
        bool isEdit = modal.GetCurrentOperation().ToLower().Contains("редактирование");
        
        if (isEdit)
        {
            // Редактирование - обновляем существующие записи
            HandleEdit(modal, empData, peopleData, professionId, onSuccess);
        }
        else
        {
            // Добавление - создаем новые записи
            HandleAdd(modal, empData, peopleData, professionId, onSuccess);
        }
    }
    
    private void HandleAdd(TableEditController modal, Dictionary<string, object> empData, Dictionary<string, object> peopleData, string professionId, System.Action onSuccess)
    {
        // Получаем ID отдела из начальных данных
        string departmentId = modal.GetInitialData()?.ContainsKey("departmentId") == true ? modal.GetInitialData()["departmentId"]?.ToString() : null;
        
        // Сначала создаем запись в peoples
        actionsButtons.StartCoroutine(actionsButtons.Api().AddRecordWithResponse("peoples", peopleData, (okPeople, createdPeople) => {
            if (okPeople && createdPeople != null)
            {
                // Получаем ID созданной записи (может быть в поле "id" или "insertId")
                string peopleId = null;
                if (createdPeople.ContainsKey("id"))
                    peopleId = createdPeople["id"].ToString();
                else if (createdPeople.ContainsKey("insertId"))
                    peopleId = createdPeople["insertId"].ToString();
                
                // Логируем для отладки
                Debug.Log($"Создана запись в peoples: {JsonConvert.SerializeObject(createdPeople)}");
                Debug.Log($"Полученный peopleId: {peopleId}");
                Debug.Log($"Данные для создания peoples: {JsonConvert.SerializeObject(peopleData)}");
                
                // Добавляем peopleId к данным сотрудника
                if (!string.IsNullOrEmpty(peopleId))
                    empData["peopleId"] = peopleId;
                
                // Создаем запись сотрудника
                Debug.Log($"Создаем сотрудника с данными: {JsonConvert.SerializeObject(empData)}");
                actionsButtons.StartCoroutine(actionsButtons.Api().AddRecordWithResponse("employees", empData, (okEmp, createdEmp) => {
                    if (okEmp && createdEmp != null)
                    {
                        // Получаем ID созданного сотрудника (может быть в поле "id" или "insertId")
                        string empId = null;
                        if (createdEmp.ContainsKey("id"))
                            empId = createdEmp["id"].ToString();
                        else if (createdEmp.ContainsKey("insertId"))
                            empId = createdEmp["insertId"].ToString();
                            
                        Debug.Log($"Создан сотрудник: {JsonConvert.SerializeObject(createdEmp)}");
                        Debug.Log($"ID сотрудника: {empId}");
                        
                        // Создаем связь с отделом
                        if (!string.IsNullOrEmpty(departmentId) && !string.IsNullOrEmpty(empId))
                        {
                            var deptLink = new Dictionary<string, object>();
                            deptLink["employeeId"] = empId;
                            deptLink["departmentId"] = departmentId;
                            
                            Debug.Log($"Создаем связь с отделом: {JsonConvert.SerializeObject(deptLink)}");
                            actionsButtons.StartCoroutine(actionsButtons.Api().AddRecord("employee_departments", deptLink, (okDeptLink) => {
                                // После создания связи с отделом создаем связь с профессией, если выбрана
                                if (!string.IsNullOrEmpty(professionId))
                                {
                                    var profLink = new Dictionary<string, object>();
                                    profLink["employeeId"] = empId;
                                    profLink["professionId"] = professionId;
                                    
                                    actionsButtons.StartCoroutine(actionsButtons.Api().AddRecord("employees_professions", profLink, (okProfLink) => {
                                        Debug.Log("Сотрудник успешно создан со всеми связями");
                                        actionsButtons.ReloadCurrentUI();
                                        onSuccess?.Invoke();
                                    }));
                                }
                                else
                                {
                                    Debug.Log("Сотрудник успешно создан без профессии");
                                    actionsButtons.ReloadCurrentUI();
                                    onSuccess?.Invoke();
                                }
                            }));
                        }
                        else
                        {
                            // Если нет отдела, создаем только связь с профессией
                            if (!string.IsNullOrEmpty(professionId))
                            {
                                var profLink = new Dictionary<string, object>();
                                profLink["employeeId"] = empId;
                                profLink["professionId"] = professionId;
                                
                                actionsButtons.StartCoroutine(actionsButtons.Api().AddRecord("employees_professions", profLink, (okProfLink) => {
                                    Debug.Log("Сотрудник успешно создан без отдела, но с профессией");
                                    actionsButtons.ReloadCurrentUI();
                                    onSuccess?.Invoke();
                                }));
                            }
                            else
                            {
                                Debug.Log("Сотрудник успешно создан без отдела и без профессии");
                                actionsButtons.ReloadCurrentUI();
                                onSuccess?.Invoke();
                            }
                        }
                    }
                    else
                    {
                        Debug.LogError($"Не удалось создать employees. okEmp={okEmp}. Payload: {JsonConvert.SerializeObject(empData)} Response: {JsonConvert.SerializeObject(createdEmp)}");
                    }
                }));
            }
            else
            {
                Debug.LogError($"Не удалось создать peoples. okPeople={okPeople}. Payload: {JsonConvert.SerializeObject(peopleData)} Response: {JsonConvert.SerializeObject(createdPeople)}");
            }
        }));
    }
    
    private void HandleEdit(TableEditController modal, Dictionary<string, object> empData, Dictionary<string, object> peopleData, string professionId, System.Action onSuccess)
    {
        // Получаем ID записи для редактирования
        string employeeId = modal.GetInitialData()?.ContainsKey("id") == true ? modal.GetInitialData()["id"]?.ToString() : null;
        string peopleId = modal.GetInitialData()?.ContainsKey("peopleId") == true ? modal.GetInitialData()["peopleId"]?.ToString() : null;
        
        if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(peopleId))
        {
            Debug.LogError("Не удалось получить ID для редактирования");
            return;
        }
        
        // Обновляем запись в peoples
        actionsButtons.StartCoroutine(actionsButtons.Api().UpdateRecord("peoples", peopleId, peopleData, (okPeople) => {
            if (okPeople)
            {
                // Обновляем запись сотрудника
                actionsButtons.StartCoroutine(actionsButtons.Api().UpdateRecord("employees", employeeId, empData, (okEmp) => {
                    if (okEmp)
                    {
                        // Обновляем связи с отделом и профессией
                        // При редактировании пока не обновляем отдел, только профессию
                        UpdateProfessionLink(employeeId, professionId, onSuccess);
                    }
                }));
            }
        }));
    }
    

    
    private void UpdateProfessionLink(string employeeId, string professionId, System.Action onSuccess)
    {
        // Сначала удаляем существующие связи
        actionsButtons.StartCoroutine(actionsButtons.Api().DeleteRecord("employees_professions", employeeId, (okDelete) => {
            // Создаем новую связь, если выбрана профессия
            if (!string.IsNullOrEmpty(professionId))
            {
                var profLink = new Dictionary<string, object>();
                profLink["employeeId"] = employeeId;
                profLink["professionId"] = professionId;
                
                actionsButtons.StartCoroutine(actionsButtons.Api().AddRecord("employees_professions", profLink, (okAdd) => {
                    Debug.Log("Профессия сотрудника успешно обновлена");
                    actionsButtons.ReloadCurrentUI();
                    onSuccess?.Invoke();
                }));
            }
            else
            {
                Debug.Log("Профессия сотрудника успешно удалена");
                actionsButtons.ReloadCurrentUI();
                onSuccess?.Invoke();
            }
        }));
    }
    
    /// <summary>
    /// Загружает данные человека по ID
    /// </summary>
    private async Task<Dictionary<string, object>> LoadPeopleData(string peopleId)
    {
        try
        {
            string url = $"http://localhost:3000/api/database/peoples/{peopleId}";
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                string token = PlayerPrefs.GetString("token", "");
                if (!string.IsNullOrEmpty(token))
                    www.SetRequestHeader("Authorization", "Bearer " + token);
                
                var op = www.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    var rows = Newtonsoft.Json.JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(www.downloadHandler.text);
                    if (rows != null && rows.Count > 0)
                        return rows[0];
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading people data: {ex.Message}");
        }
        return null;
    }
    
    /// <summary>
    /// Загружает данные профессии для сотрудника
    /// </summary>
    private async Task<Dictionary<string, object>> LoadProfessionData(string employeeId)
    {
        try
        {
            string url = $"http://localhost:3000/api/employees_professions/get/{employeeId}";
            Debug.Log($"Загружаем профессию для сотрудника {employeeId} по URL: {url}");
            
            using (var www = UnityEngine.Networking.UnityWebRequest.Get(url))
            {
                string token = PlayerPrefs.GetString("token", "");
                if (!string.IsNullOrEmpty(token))
                    www.SetRequestHeader("Authorization", "Bearer " + token);
                
                var op = www.SendWebRequest();
                while (!op.isDone) await Task.Yield();
                
                if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.Log($"Ответ API для профессии: {www.downloadHandler.text}");
                    
                    try
                    {
                        // API возвращает объект, а не массив
                        var response = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                        Debug.Log($"Десериализованный ответ: {JsonConvert.SerializeObject(response)}");
                        
                        if (response != null && response.ContainsKey("professions"))
                        {
                            Debug.Log($"Найдено поле professions: {JsonConvert.SerializeObject(response["professions"])}");
                            
                            // Используем JObject для более надежного парсинга вложенных объектов
                            var jsonResponse = Newtonsoft.Json.Linq.JObject.Parse(www.downloadHandler.text);
                            var professionsToken = jsonResponse.SelectToken("professions");
                            
                            if (professionsToken != null && professionsToken.Type == Newtonsoft.Json.Linq.JTokenType.Object)
                            {
                                var professionsObject = professionsToken.ToObject<Dictionary<string, object>>();
                                if (professionsObject != null && professionsObject.ContainsKey("id"))
                                {
                                    var result = new Dictionary<string, object>();
                                    result["professionId"] = professionsObject["id"].ToString();
                                    Debug.Log($"Загружены данные профессии для сотрудника {employeeId}: {JsonConvert.SerializeObject(result)}");
                                    return result;
                                }
                            }
                        }
                        
                        Debug.Log($"Для сотрудника {employeeId} не найдено данных о профессии в ответе API");
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Ошибка парсинга ответа API: {ex.Message}");
                    }
                }
                else
                {
                    Debug.LogError($"Ошибка загрузки профессии: {www.error}");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading profession data: {ex.Message}");
        }
        return null;
    }
}
