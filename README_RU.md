# ProjectManager

Веб-приложение для управления проектами, разработанное на ASP.NET Core 8.

## 🚀 Начало работы

Вы можете легко запустить этот проект с помощью Docker или настроить его локально на своем компьютере.

### Вариант 1: Запуск с Docker (Рекомендуется)

Этот метод требует установленного **Docker Desktop**. Он автоматически настроит приложение и базу данных SQL Server.

**Требования:**
-   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

**Как запустить:**
1.  Откройте терминал в корневой папке проекта.
2.  Выполните следующую команду:
    ```bash
    docker-compose up -d --build
    ```
3.  Приложение будет доступно по адресу [http://localhost:8080](http://localhost:8080).

**Примечание:** При первом запуске SQL Server может потребоваться около минуты для старта. Если приложение не подключится сразу, оно автоматически повторит попытку.

---

### Вариант 2: Локальная разработка (без Docker)

Если вы хотите запустить проект через Visual Studio или .NET CLI, вам понадобятся .NET SDK и локальный экземпляр SQL Server.

**Требования:**
-   [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
-   **Visual Studio 2022** (или VS Code)
-   **SQL Server** (LocalDB или SQL Express)

**Настройка:**
1.  Откройте файл `ProjectManager.Web/appsettings.json`.
2.  Обновите строку подключения `DefaultConnection`, чтобы она указывала на вашу локальную базу данных:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagerDB;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```
3.  Запустите приложение с помощью Visual Studio (F5) или команды `dotnet run`.
