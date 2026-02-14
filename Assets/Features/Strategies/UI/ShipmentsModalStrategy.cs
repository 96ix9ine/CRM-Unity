using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Newtonsoft.Json;

public class ShipmentsModalStrategy : IModalStrategy
{
    public string StrategyName => "отгрузки";

    private readonly ActionsButtonsMainMenu actions;

    public ShipmentsModalStrategy(ActionsButtonsMainMenu actionsButtons)
    {
        actions = actionsButtons;
    }

    public async void CreateFields(TableEditController modal, bool isEdit, Dictionary<string, object> data = null)
    {
        // Поля отгрузки по DTO
        var shipmentFields = new List<string>
        {
            "price","addedDate","shipmentDate","arrivalDate","specificationImage","comment",
            "licenseId","factoryId","transporterId","clientId"
        };

        // Разделитель и поля shipments
        modal.CreateTableNameSeparator("shipments", modal.GetFieldsContainer());
        await modal.CreateFieldsForTable("shipments", shipmentFields, modal.GetFieldsContainer(), isEdit, data);

        // Разделитель стенда и dropdown стенда (оставляем кастом до миграции в метаданные)
        modal.CreateTableNameSeparator("stands", modal.GetFieldsContainer());
        await modal.CreateStandDropdown(modal.GetFieldsContainer(), isEdit, data);
    }

    public void HandleSave(TableEditController modal, Dictionary<string, object> data, System.Action onSuccess)
    {
        // Извлекаем выбранный стенд
        string standId = data.ContainsKey("standId") ? (data["standId"]?.ToString() ?? null) : null;

        // Формируем payload для shipments (без standId)
        var shipmentData = new Dictionary<string, object>(data);
        shipmentData.Remove("standId");

        // Создаем отгрузку с стендом через один эндпоинт
        // Бэкенд сам создаст и shipment, и shipment_stands связь
        var fullData = new Dictionary<string, object>(shipmentData);
        if (!string.IsNullOrEmpty(standId))
        {
            fullData["standId"] = standId;
        }

        actions.StartCoroutine(actions.Api().PostJson("/shipments/create", fullData, (ok) => {
            actions.ReloadCurrentUI();
            onSuccess?.Invoke();
        }));
    }
}
