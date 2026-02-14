# Unity Task Buttons System

Новая система кнопок для взятия и завершения задач в Unity CRM.

## Обзор

Система состоит из 4 основных компонентов:

1. **TaskButtonsUI.cs** - UI компонент для управления видимостью и состоянием кнопок
2. **TaskButtonsController.cs** - Контроллер логики операций с задачами
3. **TaskApiService.cs** - Сервис для работы с API задач
4. **TaskButtonsManager.cs** - Менеджер интеграции с существующим UI

## Установка и настройка

### 1. Размещение компонентов

Создайте пустой GameObject `TaskButtonsSystem` и добавьте на него компоненты:
- `TaskButtonsUI`
- `TaskButtonsController`
- `TaskButtonsManager`
- `TaskApiService` (будет создан автоматически при необходимости)

### 2. Настройка в инспекторе

#### TaskButtonsUI:
- **Take Task Button**: Перетащите кнопку "Взять задачу"
- **Complete Task Button**: Перетащите кнопку "Завершить задачу"
- **Task Controller**: Ссылка на TaskButtonsController

#### TaskButtonsController:
- **Task UI**: Ссылка на TaskButtonsUI
- **API Service**: Ссылка на существующий TableApiService
- **List Items**: Ссылка на ListItems компонент
- **Tree View**: Ссылка на DatabaseTreeView (если используется)

#### TaskButtonsManager:
- **Task UI**: Ссылка на TaskButtonsUI
- **Task Controller**: Ссылка на TaskButtonsController
- **Actions Buttons**: Ссылка на ActionsButtonsMainMenu (для интеграции)
- **Take Task Button**: Ссылка на кнопку взятия задачи
- **Complete Task Button**: Ссылка на кнопку завершения задачи

### 3. Интеграция с существующим UI

#### Вариант 1: Через TaskButtonsManager
```csharp
// При смене таблицы на current_tasks
TaskButtonsManager manager = FindObjectOfType<TaskButtonsManager>();
manager.OnTableChanged("current_tasks");

// При обновлении данных
manager.RefreshTaskButtons();
```

#### Вариант 2: Прямое управление
```csharp
TaskButtonsUI ui = FindObjectOfType<TaskButtonsUI>();
ui.SetCurrentTable("current_tasks");

TaskButtonsController controller = FindObjectOfType<TaskButtonsController>();
controller.RefreshUI();
```

## Логика работы кнопок

### Кнопка "Взять задачу"
- **Показывается когда**:
  - Текущая таблица = `current_tasks`
  - Выбрана задача или подзадача
  - Состояние задачи = "Новая" (ID = 1)
  - Роль пользователя ≠ Директор/Администратор/Test

- **Действие**: Отправляет POST запрос на `/current_tasks/start/{taskId}`
- **Результат**: Задача переходит в состояние "Выполняется"

### Кнопка "Завершить задачу"
- **Показывается когда**:
  - Текущая таблица = `current_tasks`
  - Выбрана подзадача (stand_tasks)
  - Состояние задачи = "Выполняется" (ID = 2)
  - Достаточно компонентов для выполнения

- **Действие**: Отправляет POST запрос на `/stand_tasks/complete/{taskId}`
- **Результат**: Подзадача отмечается как завершенная

## Состояния задач

Система поддерживает следующие состояния задач:
- **1** - "Новая" (можно взять задачу)
- **2** - "Выполняется" (можно завершить подзадачу)
- **3** - "Завершена" (завершенные задачи)
- **4** - "Отменена" (отмененные задачи)

## API запросы

### Взятие задачи
```
POST /current_tasks/start/{taskId}
Authorization: Bearer {token}
```

### Завершение подзадачи
```
POST /stand_tasks/complete/{taskId}
Authorization: Bearer {token}
```

### Получение дерева задач
```
GET /current_tasks/myTree      # задачи текущего сотрудника
GET /current_tasks/tree        # все задачи (для админов)
Authorization: Bearer {token}
```

## Замена старой логики

### Что нужно убрать из ActionsButtonsMainMenu.cs:

1. **Удалить переменные** (строки 29-30):
```csharp
public Button takeTaskButton;
public Button completeTaskButton;
```

2. **Удалить обработчики** из метода Start() (строки 55-58):
```csharp
if (takeTaskButton != null)
    takeTaskButton.onClick.AddListener(OnTakeTaskButton);
if (completeTaskButton != null)
    completeTaskButton.onClick.AddListener(OnCompleteTaskButton);
```

3. **Удалить методы**:
- `OnTakeTaskButton()` (строки 693-720)
- `OnCompleteTaskButton()` (строки 732-758)
- `StartTaskRequest()` (строки 760-781)
- `CompleteStandTaskRequest()` (строки 783-838)

4. **Упростить UpdateActionButtonsState()**:
- Убрать логику для `showTakeTaskButton` и `showCompleteTaskButton`
- Убрать установку `takeTaskButton.gameObject.SetActive()` и `completeTaskButton.gameObject.SetActive()`

### Интеграция с existing ActionsButtonsMainMenu:

```csharp
// В методе SetTable() добавить:
if (tableName == "current_tasks")
{
    var taskManager = FindObjectOfType<TaskButtonsManager>();
    if (taskManager != null)
        taskManager.EnableTaskButtons();
}
else
{
    var taskManager = FindObjectOfType<TaskButtonsManager>();
    if (taskManager != null)
        taskManager.DisableTaskButtons();
}
```

## Преимущества новой системы

1. **Модульность**: Каждый компонент имеет четкую зону ответственности
2. **Переиспользование**: Можно легко использовать в других частях приложения
3. **Тестируемость**: Компоненты легко тестировать по отдельности
4. **Расширяемость**: Легко добавлять новый функционал
5. **Совместимость**: Работает вместе с существующим кодом
6. **Централизованное управление**: Вся логика задач в одном месте

## Отладка

Для отладки используйте следующие методы:

```csharp
// Проверка наличия токена
bool hasToken = TaskApiService.Instance.HasAuthToken();

// Проверка прав пользователя
bool canTakeTasks = TaskApiService.Instance.CanUserTakeTasks();

// Получение роли пользователя
string role = TaskApiService.Instance.GetUserRole();

// Принудительное обновление UI
FindObjectOfType<TaskButtonsManager>()?.RefreshTaskButtons();
```

## Логирование

Система автоматически логирует:
- Успешные операции с задачами
- Ошибки API запросов
- Отсутствие необходимых компонентов
- Попытки запрещенных действий