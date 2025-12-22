# Expenses API

A .NET 10.0 Web API application for managing expenses and transactions, built with Entity Framework Core and SQL Server.

## Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download) or later
- SQL Server (LocalDB, Express, or full instance)
- Visual Studio 2022, VS Code, or any IDE with .NET support

> 📖 **For detailed installation and setup commands**, see [INSTALLATION.md](INSTALLATION.md) - includes project creation, solution files, package management, and more.

## Project Structure

```
Expenses.API/
├── Controllers/          # API Controllers
│   ├── TransactionController.cs
│   └── WeatherForecastController.cs
├── Domain/              # Domain models and services
│   ├── Transaction/
│   │   ├── Dto/         # Data Transfer Objects
│   │   ├── Services/    # Business logic services
│   │   └── Transaction.cs
│   └── User/
├── Framework/           # Framework extensions and infrastructure
│   ├── Data/           # DbContext and data access
│   └── Extensions/     # Dependency injection extensions
├── Migrations/         # Entity Framework migrations
├── Properties/         # Launch settings
├── Shared/             # Shared models and utilities
└── Program.cs          # Application entry point
```

## Getting Started

### 1. Clone the Repository

```bash
git clone <repository-url>
cd Expenses.API
```

### 2. Configure Database Connection

Update the connection string in `appsettings.json` or `appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=YOUR_SERVER;Initial Catalog=asp_db_transactions;Integrated Security=True;Encrypt=False;Trust Server Certificate=True"
  }
}
```

### 3. Apply Database Migrations

```bash
dotnet ef database update
```

### 4. Restore Dependencies

```bash
dotnet restore
```

## Package Management (NuGet)

> 💡 **See [INSTALLATION.md](INSTALLATION.md) for complete package management guide and all available commands.**

### Installing Packages via Command Line

The recommended way to install NuGet packages is using the command line:

```bash
dotnet add package PackageName
```

**Examples:**
```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Swashbuckle.AspNetCore
dotnet add package Newtonsoft.Json
```

This command automatically:
- Downloads the package from NuGet
- Adds it to your `.csproj` file
- Restores dependencies

### Installing Packages via UI

#### Visual Studio
1. Right-click your project in **Solution Explorer**
2. Select **"Manage NuGet Packages..."**
3. Browse or search for packages
4. Click **Install**

Alternatively, use the **Package Manager Console**:
- Go to **Tools → NuGet Package Manager → Package Manager Console**
- Run: `Install-Package PackageName`

#### Visual Studio Code
Install the **"NuGet Package Manager"** extension (by jmrog):
1. Open Extensions (Ctrl+Shift+X)
2. Search for "NuGet Package Manager"
3. Install the extension
4. Use the extension's UI to browse and install packages

#### Rider
1. Right-click your project
2. Select **"Manage NuGet Packages..."**
3. Browse and install packages through the UI

### Other Useful Package Commands

**List installed packages:**
```bash
dotnet list package
```

**Update a specific package:**
```bash
dotnet add package PackageName --version X.X.X
```

**Remove a package:**
```bash
dotnet remove package PackageName
```

**Restore packages:**
```bash
dotnet restore
```

## Running the Application

### Using dotnet watch (Recommended for Development)

The `dotnet watch` command enables **hot reload** functionality, automatically applying code changes without a full restart.

#### Run with HTTPS Profile (Default - Opens Swagger)

```bash
dotnet watch run --launch-profile https
```

This will:
- Start the application on both `https://localhost:7044` and `http://localhost:5096`
- Automatically open your browser to the Swagger UI
- Enable hot reload for code changes

#### Run with HTTP Profile Only

```bash
dotnet watch run --launch-profile http
```

This will:
- Start the application on `http://localhost:5096` only
- Not open the browser automatically

#### Run without specifying profile

```bash
dotnet watch run
```

**Note:** Without specifying a profile, `dotnet watch` uses the first profile in `launchSettings.json` (currently "http").

### Using dotnet run

```bash
dotnet run
```

This runs the application without hot reload. Changes require manual restart.

### Using Visual Studio

1. Open `Expenses.API.slnx` in Visual Studio
2. Select the desired launch profile from the dropdown (http or https)
3. Press F5 or click Run

## Launch Profiles

The application includes two launch profiles configured in `Properties/launchSettings.json`:

### HTTPS Profile
- **URLs:** `https://localhost:7044` and `http://localhost:5096`
- **Browser:** Opens automatically to Swagger UI
- **Use case:** Development with HTTPS support and API documentation

### HTTP Profile
- **URL:** `http://localhost:5096` only
- **Browser:** Does not open automatically
- **Use case:** Simple HTTP-only development

**Important:** Unlike Visual Studio, `dotnet watch run` and `dotnet run` only use **one profile at a time**. The HTTPS profile already includes both HTTP and HTTPS URLs, so using it gives you access to both protocols.

## Hot Reload

`dotnet watch run` automatically enables hot reload in .NET 6+ (including .NET 10.0). Changes are applied automatically without a full application restart.

### What Gets Hot Reloaded (No Restart Required)
- ✅ Changes to Razor views (.cshtml, .razor)
- ✅ Changes to CSS, JavaScript, and static files
- ✅ Changes to controller actions and methods
- ✅ Changes to middleware configuration
- ✅ Changes to dependency injection registrations
- ✅ Changes to configuration files (appsettings.json)

### What Requires Full Restart
- ❌ Changes to `Program.cs` structure
- ❌ Changes to project file (.csproj)
- ❌ Adding/removing NuGet packages
- ❌ Changes to launchSettings.json
- ❌ Changes requiring full recompilation

### Hot Reload Indicators

When you make a change, you'll see output like:

```
watch : File changed: Program.cs
watch : Hot reload of changes succeeded.
```

Or if a restart is needed:

```
watch : File changed: Program.cs
watch : Restarting application...
```

### Disable Hot Reload

If you want to always restart instead of using hot reload:

```bash
dotnet watch --no-hot-reload run
```

## API Documentation

When running with the HTTPS profile, Swagger UI is automatically available at:

- **Swagger UI:** `https://localhost:7044/swagger`
- **Swagger JSON:** `https://localhost:7044/swagger/v1/swagger.json`

Swagger is only enabled in the Development environment.

## Technologies Used

- **.NET 10.0** - Framework
- **ASP.NET Core Web API** - Web framework
- **Entity Framework Core 10.0.1** - ORM
- **SQL Server** - Database
- **Swashbuckle.AspNetCore 10.0.1** - Swagger/OpenAPI documentation

## Development Tips

1. **Always use `dotnet watch run`** during development for hot reload
2. **Use the HTTPS profile** to access Swagger documentation easily
3. **Check the console output** to see if hot reload succeeded or if a restart was needed
4. **Update connection strings** in `appsettings.Development.json` for local development

## Troubleshooting

### Port Already in Use

If you get a port conflict error, either:
- Stop the application using that port
- Change the port in `launchSettings.json`

### Database Connection Issues

- Verify SQL Server is running
- Check the connection string in `appsettings.json`
- Ensure the database exists or migrations have been applied

### Hot Reload Not Working

- Ensure you're using `dotnet watch run` (not just `dotnet run`)
- Some changes require a full restart (see Hot Reload section above)
- Check the console output for hot reload status

## License

[Add your license information here]

