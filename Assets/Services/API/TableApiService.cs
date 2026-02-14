using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.IO;
using Newtonsoft.Json;

/// <summary>
/// Сервис для работы с API таблиц
/// Обеспечивает CRUD операции с данными таблиц
/// Поддерживает приоритетное использование generateData endpoint
/// </summary>
public class TableApiService : MonoBehaviour
{
    [SerializeField] public string apiUrl = "http://localhost:3000/api";

    // Кэш соответствия nodeType -> tableName
    private Dictionary<string, string> _nodeTypeToTableCache;

    #region Helpers (Universal HTTP)
    /// <summary>
    /// Универсальный GET JSON. Путь относительный к apiUrl (начинать с "/").
    /// </summary>
    public IEnumerator GetJson<T>(string relativePath, Action<T> onResult)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(ComposeUrl(relativePath)))
        {
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                T data = default(T);
                try {
                    // Спец-обработка для string: вернуть сырой текст
                    if (typeof(T) == typeof(string))
                    {
                        object boxed = www.downloadHandler.text;
                        data = (T)boxed;
                    }
                    else
                    {
                        data = JsonConvert.DeserializeObject<T>(www.downloadHandler.text);
                    }
                }
                catch (Exception e) { Debug.LogError($"GetJson<{typeof(T).Name}> parse error: {e.Message}"); }
                onResult?.Invoke(data);
            }
            else
            {
                Debug.LogError($"GET {relativePath} failed: {www.responseCode} {www.error}");
                onResult?.Invoke(default(T));
            }
        }
    }

    /// <summary>
    /// Универсальный GET, возвращающий сырой ответ как строку без десериализации.
    /// </summary>
    public IEnumerator GetText(string relativePath, Action<string> onResult)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(ComposeUrl(relativePath)))
        {
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                onResult?.Invoke(www.downloadHandler.text);
            }
            else
            {
                Debug.LogError($"GET {relativePath} failed: {www.responseCode} {www.error}");
                onResult?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// Универсальный POST JSON. Возвращает десериализованный ответ TResp.
    /// </summary>
    public IEnumerator PostJson<TReq, TResp>(string relativePath, TReq body, Action<TResp> onResult)
    {
        string json = JsonConvert.SerializeObject(body);
        using (UnityWebRequest www = new UnityWebRequest(ComposeUrl(relativePath), "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                TResp data = default(TResp);
                try
                {
                    var txt = www.downloadHandler?.text;
                    if (!string.IsNullOrEmpty(txt))
                    {
                        // Если целевой тип — string, возвращаем сырой текст без десериализации
                        if (typeof(TResp) == typeof(string))
                        {
                            object boxed = txt;
                            data = (TResp)boxed;
                        }
                        else
                        {
                            data = JsonConvert.DeserializeObject<TResp>(txt);
                        }
                    }
                }
                catch (Exception e) { Debug.LogError($"PostJson parse error: {e.Message}"); }
                onResult?.Invoke(data);
            }
            else
            {
                Debug.LogError($"POST {relativePath} failed: {www.responseCode} {www.error}");
                onResult?.Invoke(default(TResp));
            }
        }
    }

    /// <summary>
    /// Упрощённый POST JSON: важно только, что запрос прошёл успешно (status 2xx).
    /// </summary>
    public IEnumerator PostJson(string relativePath, Dictionary<string, object> body, Action<bool> onResult)
    {
        yield return PostJson<Dictionary<string, object>, object>(relativePath, body, (resp) =>
        {
            onResult?.Invoke(true);
        });
    }

    /// <summary>
    /// POST JSON и вернуть ответ как словарь (если сервер его вернул).
    /// </summary>
    public IEnumerator PostJsonWithResponse(string relativePath, Dictionary<string, object> body, Action<bool, Dictionary<string, object>> onResult)
    {
        // Пытаемся распарсить ответ как словарь напрямую; если сервер вернёт пусто — отдадим null
        yield return PostJson<Dictionary<string, object>, Dictionary<string, object>>(relativePath, body, (dict) =>
        {
            onResult?.Invoke(true, dict);
        });
    }

    /// <summary>
    /// Универсальный PUT JSON. Возвращает success.
    /// </summary>
    public IEnumerator PutJson<TReq>(string relativePath, TReq body, Action<bool> onResult)
    {
        string json = JsonConvert.SerializeObject(body);
        using (UnityWebRequest www = new UnityWebRequest(ComposeUrl(relativePath), "PUT"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            onResult?.Invoke(www.result == UnityWebRequest.Result.Success);
        }
    }

    /// <summary>
    /// Универсальный DELETE. Возвращает success.
    /// </summary>
    public IEnumerator Delete(string relativePath, Action<bool> onResult)
    {
        using (UnityWebRequest www = UnityWebRequest.Delete(ComposeUrl(relativePath)))
        {
            www.downloadHandler = new DownloadHandlerBuffer();
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            onResult?.Invoke(www.result == UnityWebRequest.Result.Success);
        }
    }

    /// <summary>
    /// Удобный алиас для Delete.
    /// </summary>
    public IEnumerator DeleteRequest(string relativePath, Action<bool> onResult)
        => Delete(relativePath, onResult);

    /// <summary>
    /// GET bytes (для скачивания файлов).
    /// </summary>
    public IEnumerator GetBytes(string relativePath, Action<byte[]> onResult)
    {
        using (UnityWebRequest www = UnityWebRequest.Get(ComposeUrl(relativePath)))
        {
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
                onResult?.Invoke(www.downloadHandler.data);
            else
            {
                Debug.LogError($"GET bytes {relativePath} failed: {www.responseCode} {www.error}");
                onResult?.Invoke(null);
            }
        }
    }

    /// <summary>
    /// POST multipart/form-data (для загрузки файлов и форм).
    /// </summary>
    public IEnumerator PostForm(string relativePath, WWWForm form, Action<bool, string> onResult)
    {
        using (UnityWebRequest www = UnityWebRequest.Post(ComposeUrl(relativePath), form))
        {
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            bool ok = www.result == UnityWebRequest.Result.Success;
            onResult?.Invoke(ok, ok ? null : www.error);
        }
    }

    /// <summary>
    /// PATCH multipart/form-data (без файла — для смены targetType/targetId у изображения)
    /// </summary>
    public IEnumerator PatchForm(string relativePath, WWWForm form, Action<bool, string> onResult)
    {
        var url = ComposeUrl(relativePath);
        var req = new UnityWebRequest(url, "PATCH");
        // переносим тело формы
        byte[] body = form.data;
        req.uploadHandler = new UploadHandlerRaw(body);
        req.downloadHandler = new DownloadHandlerBuffer();
        // переносим заголовки формы (boundary и content-type)
        foreach (var kv in form.headers)
            req.SetRequestHeader(kv.Key, kv.Value);
        AttachAuthHeader(req);
        yield return req.SendWebRequest();
        bool ok = req.result == UnityWebRequest.Result.Success;
        onResult?.Invoke(ok, ok ? null : req.error);
    }

    /// <summary>
    /// Загрузка изображения на сервер: POST /images/upload с полями file, targetType, targetId
    /// </summary>
    public IEnumerator UploadImage(string targetType, string targetId, string fileName, byte[] fileBytes, Action<bool> onResult)
    {
        if (string.IsNullOrEmpty(targetType) || string.IsNullOrEmpty(targetId) || fileBytes == null || fileBytes.Length == 0)
        {
            Debug.LogError("UploadImage: некорректные аргументы");
            onResult?.Invoke(false);
            yield break;
        }
        var form = new WWWForm();
        form.AddField("targetType", targetType);
        form.AddField("targetId", targetId);
        var safeName = string.IsNullOrEmpty(fileName) ? "image.png" : fileName;
        form.AddBinaryData("file", fileBytes, safeName);
        yield return PostForm("/images/upload", form, (ok, err) => onResult?.Invoke(ok));
    }

    /// <summary>
    /// Удаление всех изображений по targetType/targetId
    /// </summary>
    public IEnumerator DeleteImagesByTarget(string targetType, string targetId, Action<bool> onResult)
    {
        yield return Delete($"/images/byTarget/{targetType}/{targetId}", onResult);
    }

    /// <summary>
    /// Обновить привязку существующего изображения по id через PATCH (без загрузки файла)
    /// </summary>
    public IEnumerator UpdateImageTarget(string imageId, string targetType, string targetId, Action<bool> onResult)
    {
        var form = new WWWForm();
        form.AddField("targetType", targetType);
        form.AddField("targetId", targetId);
        yield return PatchForm($"/images/{imageId}", form, (ok, _) => onResult?.Invoke(ok));
    }

    private string ComposeUrl(string relativePath)
    {
        if (string.IsNullOrEmpty(relativePath)) return apiUrl;
        if (relativePath.StartsWith("/")) return apiUrl + relativePath;
        return apiUrl + "/" + relativePath;
    }

    private void AttachAuthHeader(UnityWebRequest www)
    {
        string token = PlayerPrefs.GetString("token", "");
        if (!string.IsNullOrEmpty(token))
            www.SetRequestHeader("Authorization", "Bearer " + token);
    }
    #endregion

    /// <summary>
    /// Возвращает карту соответствия nodeType -> tableName с кэшированием
    /// </summary>
    public IEnumerator GetNodeTypesMap(Action<Dictionary<string, string>> onResult)
    {
        if (_nodeTypeToTableCache != null)
        {
            onResult?.Invoke(_nodeTypeToTableCache);
            yield break;
        }
        using (UnityWebRequest www = UnityWebRequest.Get($"{apiUrl}/database/nodeTypesMap"))
        {
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                www.SetRequestHeader("Authorization", "Bearer " + token);
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    _nodeTypeToTableCache = JsonConvert.DeserializeObject<Dictionary<string, string>>(www.downloadHandler.text);
                    Debug.Log("nodeTypesMap загружен: " + www.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError("Ошибка парсинга nodeTypesMap: " + e.Message);
                    _nodeTypeToTableCache = new Dictionary<string, string>();
                }
            }
            else
            {
                Debug.LogWarning("Не удалось загрузить nodeTypesMap: " + www.error);
                _nodeTypeToTableCache = new Dictionary<string, string>();
            }
            onResult?.Invoke(_nodeTypeToTableCache);
        }
    }

    /// <summary>
    /// Получает список колонок таблицы
    /// </summary>
    public IEnumerator GetColumns(string tableName, Action<List<string>> onResult)
    {
        yield return GetJson<List<string>>($"/database/{tableName}/columns", (cols) =>
        {
            if (cols == null) Debug.LogError("Error loading columns");
            onResult?.Invoke(cols);
        });
    }

    /// <summary>
    /// Получает все записи таблицы с приоритетом generateData
    /// Сначала пытается использовать generateData endpoint, если его нет - использует стандартный
    /// </summary>
    public IEnumerator GetRowsWithGenerateData(string tableName, Action<List<Dictionary<string, object>>> onResult)
    {
        bool handled = false;
        yield return GetJson<List<Dictionary<string, object>>>($"/{tableName}/generateData", (rows) =>
        {
            if (rows != null)
            {
                Debug.Log($"Успешно получены данные через generateData для таблицы {tableName}");
                onResult?.Invoke(rows);
                handled = true;
            }
        });

        if (handled) yield break;
        yield return GetJson<List<Dictionary<string, object>>>($"/database/{tableName}", (rows) =>
        {
            if (rows == null) Debug.LogError("Error loading rows");
            else Debug.Log($"Получены данные через стандартный метод для таблицы {tableName}");
            onResult?.Invoke(rows);
        });
    }

    /// <summary>
    /// Получает все записи таблицы (устаревший метод, используйте GetRowsWithGenerateData)
    /// </summary>
    public IEnumerator GetRows(string tableName, Action<List<Dictionary<string, object>>> onResult)
    {
        yield return StartCoroutine(GetRowsWithGenerateData(tableName, onResult));
    }

    /// <summary>
    /// Добавляет новую запись в таблицу
    /// </summary>
    public IEnumerator AddRecord(string tableName, Dictionary<string, object> data, Action<bool> onResult)
    {
        // Сначала пробуем entity-эндпоинт, затем fallback на /database
        yield return PostJsonTryEntityThenDatabase(tableName, data, (ok, _) => onResult?.Invoke(ok));
    }

    /// <summary>
    /// Добавляет новую запись и возвращает тело ответа (созданную запись/ид)
    /// </summary>
    public IEnumerator AddRecordWithResponse(string tableName, Dictionary<string, object> data, Action<bool, Dictionary<string, object>> onResult)
    {
        yield return PostJsonTryEntityThenDatabase(tableName, data, onResult);
    }

    /// <summary>
    /// Обновляет существующую запись в таблице
    /// </summary>
    public IEnumerator UpdateRecord(string tableName, object id, Dictionary<string, object> data, Action<bool> onResult)
    {
        // Сначала PUT на entity, затем fallback на /database
        bool done = false;
        yield return PutJson($"/{tableName}/{id}", data, (ok1) => { done = ok1; onResult?.Invoke(ok1); });
        if (done) yield break;
        yield return PutJson($"/database/{tableName}/{id}", data, (ok2) => onResult?.Invoke(ok2));
    }

    /// <summary>
    /// Удаляет запись из таблицы
    /// </summary>
    public IEnumerator DeleteRecord(string tableName, object id, Action<bool> onResult)
    {
        // Сначала DELETE на entity, затем fallback на /database
        bool done = false;
        yield return Delete($"/{tableName}/{id}", (ok1) => { done = ok1; onResult?.Invoke(ok1); });
        if (done) yield break;
        yield return Delete($"/database/{tableName}/{id}", onResult);
    }

    /// <summary>
    /// Удаляет запись из таблицы с очисткой внешних ключей (SET NULL)
    /// </summary>
    public IEnumerator DeleteRecordWithCleanup(string tableName, object id, Action<bool> onResult)
    {
        yield return Delete($"/database/{tableName}/{id}/cleanup", onResult);
    }

    public IEnumerator GetRowById(string tableName, string id, Action<Dictionary<string, object>> onResult)
    {
        // Некоторые endpoints возвращают массив с одной записью, другие — объект. Поддержим оба варианта.
        yield return GetText($"/database/{tableName}/{id}", (txt) =>
        {
            if (string.IsNullOrEmpty(txt)) { onResult?.Invoke(null); return; }
            try
            {
                var obj = JsonConvert.DeserializeObject<Dictionary<string, object>>(txt);
                if (obj != null) { onResult?.Invoke(obj); return; }
            }
            catch { /* попробуем как массив */ }

            try
            {
                var arr = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(txt);
                if (arr != null && arr.Count > 0) { onResult?.Invoke(arr[0]); return; }
            }
            catch (Exception e)
            {
                Debug.LogError($"GetRowById parse error: {e.Message}");
            }

            onResult?.Invoke(null);
        });
    }

    #region Convenience endpoints
    public IEnumerator GetLocalizationTables(Action<Dictionary<string, string>> onResult)
        => GetJson<Dictionary<string, string>>("/localization/tables", onResult);

    public IEnumerator GetLocalizationTableFields(string tableName, Action<Dictionary<string, string>> onResult)
        => GetJson<Dictionary<string, string>>($"/localization/tables/{tableName}/fields", onResult);

    public IEnumerator SearchTable(string tableName, string query, Action<List<Dictionary<string, object>>> onResult)
        => GetJson<List<Dictionary<string, object>>>($"/database/{tableName}/search?query={UnityWebRequest.EscapeURL(query)}", onResult);

    public IEnumerator GetFieldDistinctValues(string tableName, string fieldName, Action<List<Dictionary<string, object>>> onResult)
        => GetJson<List<Dictionary<string, object>>>($"/database/{tableName}/column/{fieldName}/values", onResult);

    public IEnumerator GetEntityTree(string tableName, Action<Newtonsoft.Json.Linq.JObject> onResult)
        => GetJson<Newtonsoft.Json.Linq.JObject>($"/{tableName}/tree", onResult);

    public IEnumerator StartTask(string taskId, Action<bool> onResult)
        => PostJson<string, object>($"/current_tasks/start/{taskId}", "", _ => onResult?.Invoke(true));

    public IEnumerator CompleteStandTask(string standTaskId, Action<bool> onResult)
        => PostJson<string, object>($"/stand_tasks/complete/{standTaskId}", "", _ => onResult?.Invoke(true));

    public IEnumerator GetFieldHints(Action<Dictionary<string, string>> onResult)
        => GetJson<Dictionary<string, string>>("/database/fieldHints", onResult);

    public IEnumerator GetNodeTypesMapCached(Action<Dictionary<string, string>> onResult)
        => GetNodeTypesMap(onResult);
    #endregion

    // ---- Internals for dual-path entity/database posting ----

    private IEnumerator PostJsonReturn(string relativePath, Dictionary<string, object> body, Action<bool, Dictionary<string, object>, string> onResult)
    {
        string json = JsonConvert.SerializeObject(body);
        using (UnityWebRequest www = new UnityWebRequest(ComposeUrl(relativePath), "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            AttachAuthHeader(www);
            yield return www.SendWebRequest();
            bool ok = www.result == UnityWebRequest.Result.Success;
            string txt = www.downloadHandler?.text;
            Dictionary<string, object> resp = null;
            if (!string.IsNullOrEmpty(txt))
            {
                try { resp = JsonConvert.DeserializeObject<Dictionary<string, object>>(txt); } catch { }
            }
            onResult?.Invoke(ok, resp, txt);
        }
    }

    private IEnumerator PostJsonTryEntityThenDatabase(string tableName, Dictionary<string, object> data, Action<bool, Dictionary<string, object>> onResult)
    {
        bool finished = false;
        Dictionary<string, object> result = null;
        bool ok = false;
        // Try entity
        yield return PostJsonReturn($"/{tableName}", data, (ok1, resp1, raw1) => { ok = ok1; result = resp1; finished = ok1; });
        if (ok)
        {
            onResult?.Invoke(true, result);
            yield break;
        }
        // Fallback: database
        yield return PostJsonReturn($"/database/{tableName}", data, (ok2, resp2, raw2) => { ok = ok2; result = resp2; });
        onResult?.Invoke(ok, result);
    }
} 