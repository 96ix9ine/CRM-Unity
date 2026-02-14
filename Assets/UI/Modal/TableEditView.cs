using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Представление модального окна редактирования таблицы
/// Содержит только ссылки на UI элементы без бизнес-логики
/// </summary>
public class TableEditView : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject m_ModalPanel;         // Панель модального окна
    [SerializeField] private TMP_Text m_TitleText;           // Заголовок окна
    [SerializeField] private Transform m_FieldsContainer;     // Контейнер для полей ввода
    [SerializeField] private Button m_SaveButton;             // Кнопка сохранения
    [SerializeField] private Button m_CancelButton;           // Кнопка отмены
    [SerializeField] private GameObject m_FieldPrefab;        // Префаб поля ввода (InputField + Label)
    [SerializeField] private GameObject m_DateFieldPrefab;    // Префаб поля ввода даты с календарем
    [SerializeField] private GameObject m_DatePickerPrefab;   // Префаб календаря
    [SerializeField] private GameObject m_ModalTableNamePrefab; // Префаб для названия таблицы в модалке

    #region Public Properties
    
    public GameObject ModalPanel => m_ModalPanel;
    public TMP_Text TitleText => m_TitleText;
    public Transform FieldsContainer => m_FieldsContainer;
    public Button SaveButton => m_SaveButton;
    public Button CancelButton => m_CancelButton;
    public GameObject FieldPrefab => m_FieldPrefab;
    public GameObject DateFieldPrefab => m_DateFieldPrefab;
    public GameObject DatePickerPrefab => m_DatePickerPrefab;
    public GameObject ModalTableNamePrefab => m_ModalTableNamePrefab;
    
    #endregion

    #region Public Methods

    /// <summary>
    /// Показывает модальное окно
    /// </summary>
    public void Show()
    {
        if (m_ModalPanel != null)
            m_ModalPanel.SetActive(true);
    }

    /// <summary>
    /// Скрывает модальное окно
    /// </summary>
    public void Hide()
    {
        if (m_ModalPanel != null)
            m_ModalPanel.SetActive(false);
    }

    /// <summary>
    /// Устанавливает заголовок модального окна
    /// </summary>
    public void SetTitle(string title)
    {
        if (m_TitleText != null)
            m_TitleText.text = title;
    }

    /// <summary>
    /// Очищает контейнеры полей
    /// </summary>
    public void ClearFields()
    {
        ClearContainer(m_FieldsContainer);
    }

    /// <summary>
    /// Создает новый элемент поля в указанном контейнере
    /// </summary>
    public GameObject CreateFieldItem(Transform container)
    {
        if (m_FieldPrefab == null || container == null) return null;
        
        var fieldObj = Instantiate(m_FieldPrefab, container);
        fieldObj.SetActive(true);
        return fieldObj;
    }

    /// <summary>
    /// Привязывает обработчик к кнопке сохранения
    /// </summary>
    public void BindSaveButton(Action onClick)
    {
        if (m_SaveButton != null)
        {
            m_SaveButton.onClick.RemoveAllListeners();
            m_SaveButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    /// <summary>
    /// Привязывает обработчик к кнопке отмены
    /// </summary>
    public void BindCancelButton(Action onClick)
    {
        if (m_CancelButton != null)
        {
            m_CancelButton.onClick.RemoveAllListeners();
            m_CancelButton.onClick.AddListener(() => onClick?.Invoke());
        }
    }

    /// <summary>
    /// Создает разделитель таблиц в модальном окне
    /// </summary>
    public GameObject CreateTableNameSeparator(string tableName, Transform container)
    {
        if (m_ModalTableNamePrefab == null || container == null)
        {
            Debug.LogWarning("ModalTableNamePrefab не назначен или контейнер null!");
            return null;
        }
        
        var separator = Instantiate(m_ModalTableNamePrefab, container);
        separator.SetActive(true);
        
        var tableNameContainer = separator.GetComponent<ModalTableNameContainer>();
        if (tableNameContainer != null)
        {
            tableNameContainer.SetTableName(tableName);
        }
        
        return separator;
    }

    // Утилита: найти первый PhotoPickerController на сцене (простая интеграция без лишних связей)
    public PhotoPickerController FindPhotoPicker()
    {
        return GameObject.FindObjectOfType<PhotoPickerController>(true);
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Очищает указанный контейнер от всех дочерних элементов кроме первого (эталона)
    /// </summary>
    private void ClearContainer(Transform container)
    {
        if (container == null) return;

        // Скрываем эталон
        if (container.childCount > 0)
            container.GetChild(0).gameObject.SetActive(false);

        // Удаляем все клоны, кроме эталона
        for (int i = container.childCount - 1; i > 0; i--)
            Destroy(container.GetChild(i).gameObject);
    }

    #endregion
}
