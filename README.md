# ProjectManager

A web-based project management application built with ASP.NET Core 8.

## 🚀 Getting Started

You can run this project easily using Docker, or set it up locally on your machine.

### Option 1: Run with Docker (Recommended)

This method requires **Docker Desktop** installed on your machine. It will automatically set up the application and a SQL Server database for you.

**Prerequisites:**
-   [Docker Desktop](https://www.docker.com/products/docker-desktop/)

**How to Run:**
1.  Open a terminal in the project root.
2.  Run the following command:
    ```bash
    docker-compose up -d --build
    ```
3.  The application will be available at [http://localhost:8080](http://localhost:8080).

**Note:** The first time you run this, it may take a minute for the SQL Server to start. If the app fails to connect immediately, it will retry automatically.

---

### Option 2: Local Development (Without Docker)

If you want to run the project via Visual Studio or the .NET CLI, you need to have the .NET SDK and a local SQL Server instance.

**Prerequisites:**
-   [.NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
-   **Visual Studio 2022** (or VS Code)
-   **SQL Server** (LocalDB or SQL Express)

**Configuration:**
1.  Open `ProjectManager.Web/appsettings.json`.
2.  Update the `DefaultConnection` string to point to your local database instance:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=ProjectManagerDB;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```
3.  Run the application using Visual Studio (F5) or `dotnet run`.

---

## 🔐 Authentication & User Setup

### 1. Admin Login
To start using the system, log in with the default administrator account:
- **Login:** `admin@test.com`
- **Password:** `Admin123!`

### 2. Creating Employees
1. Log in as **Admin**.
2. Navigate to the **Employees** section.
3. Click **Create New Employee**.
4. Fill in the details. **Important:** You must specify a **Temporary Password** for the employee here.
5. The **Email** address specified will be the employee's login.

### 3. Employee Login
Employees can log in using the **Email** and **Temporary Password** set by the Admin during creation.
