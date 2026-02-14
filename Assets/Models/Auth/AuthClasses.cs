using System;
using UnityEngine;
using System.Collections.Generic;

namespace Auth.Models
{
    /// <summary>
    /// DTO для запроса авторизации пользователя.
    /// </summary>
    [Serializable]
    public class AuthRequest
    {
        public string userName;      // Имя пользователя (логин)
        public string password;      // Пароль пользователя
    }

    /// <summary>
    /// DTO для профессии сотрудника.
    /// </summary>
    [Serializable]
    public class ProfessionInfo
    {
        public int id;        // ID профессии
        public string title;  // Название профессии
    }

    /// <summary>
    /// DTO для профессии сотрудника (связь Employee-Profession).
    /// </summary>
    [Serializable]
    public class EmployeeProfessionInfo
    {
        public int id; // ID связи
        public EmployeeInfo employees;
        public ProfessionInfo professions;
    }

    /// <summary>
    /// DTO для сотрудника (Employee).
    /// </summary>
    [Serializable]
    public class EmployeeInfo
    {
        public int id;                // ID сотрудника
        public string dismissalDate;   // Дата увольнения
        public PeoplesInfo peoples;     // Связанный человек
        public List<EmployeeProfessionInfo> employeesProfessions; // Профессии сотрудника
        // Можно добавить другие поля, если есть
    }

    /// <summary>
    /// DTO для People (человека).
    /// </summary>
    [Serializable]
    public class PeoplesInfo
    {
        public int id;
        public string firstName;
        public string middleName;
        public string lastName;
        public string phone;
        public string email;
        public string comment;
        public string birthDate;
        // public List<EmployeeInfo> employees; // больше не нужно, если employees теперь в UserInfo
    }

    /// <summary>
    /// DTO с информацией о пользователе, возвращаемой с сервера.
    /// </summary>
    [Serializable]
    public class UserInfo
    {
        public int id;
        public string userName;
        public string password;
        public string passwordSalt;
        public EmployeeInfo employees;
        public EmployeeProfessionInfo employeeProfession;
    }

    /// <summary>
    /// DTO для ответа на авторизацию.
    /// </summary>
    [Serializable]
    public class AuthResponse
    {
        public string token;        // JWT токен
        public UserInfo user;       // Информация о пользователе
    }

    /// <summary>
    /// DTO для ошибок, возвращаемых сервером.
    /// </summary>
    [Serializable]
    public class ErrorResponse
    {
        public string message;   // Сообщение об ошибке
        public string error;     // Тип ошибки
        public int statusCode;   // HTTP статус
    }

    /// <summary>
    /// DTO для запроса регистрации пользователя.
    /// </summary>
    [Serializable]
    public class RegisterRequest
    {
        public string email;        // Email
        public string password;     // Пароль
        public string userName;     // Имя пользователя (логин)
        public string passwordSalt; // Хэшированный пароль
        public string phone;        // Телефон
        public string birthDate;    // Дата рождения
        public string firstName;    // Имя
        public string lastName;     // Фамилия
        public string middleName;   // Отчество
    }
}
