# Installation & Setup Guide

This guide covers all the essential commands for creating, setting up, and managing .NET projects.

## Table of Contents

- [Understanding Solutions and Projects](#understanding-solutions-and-projects)
- [Creating New Projects](#creating-new-projects)
- [Solution Files](#solution-files)
- [Package Management](#package-management)
- [Project Setup Commands](#project-setup-commands)

## Understanding Solutions and Projects

### What is a Project?

A **project** (`.csproj` file) is the fundamental unit of work in .NET. It represents:
- A single application, library, or component
- Contains source code files, resources, and configuration
- Defines dependencies (NuGet packages, other projects)
- Has its own build output (DLL, EXE, etc.)

**Example:** A Web API project, a class library, a console application.

### What is a Solution?

A **solution** (`.sln` or `.slnx` file) is a container that organizes one or more related projects. It:
- Groups multiple projects together
- Manages project references and build order
- Provides a way to work with multiple projects as a unit
- Is primarily used by IDEs (Visual Studio, Rider) for organization

**Example:** A solution might contain:
- A Web API project
- A class library project (shared code)
- A test project
- A frontend project

### Solution vs Project: When Do You Need Each?

#### Single Project (No Solution Needed)
- **Simple applications** with a single project
- **Command-line development** - you can work directly with the project
- **Small projects** that don't need organization

**You can work without a solution file:**
```bash
dotnet new webapi -n MyApi
cd MyApi
dotnet run
```

#### Multiple Projects (Solution Recommended)
- **Multi-project applications** (API + Library + Tests)
- **Visual Studio development** - VS works better with solutions
- **Team collaboration** - solutions help organize complex codebases
- **Project references** - easier to manage dependencies between projects

**Recommended structure:**
```
MySolution/
├── MySolution.sln          # Solution file
├── MyApi/                  # Web API project
│   └── MyApi.csproj
├── MyLibrary/              # Shared library
│   └── MyLibrary.csproj
└── MyApi.Tests/            # Test project
    └── MyApi.Tests.csproj
```

### Recommended Schema

#### ✅ **Recommended: Solution First, Then Projects**

**Best Practice:** Create the solution first, then add projects to it.

```bash
# 1. Create solution first
dotnet new sln -n MySolution

# 2. Create projects
dotnet new webapi -n MyApi
dotnet new classlib -n MyLibrary

# 3. Add projects to solution
dotnet sln add MyApi/MyApi.csproj
dotnet sln add MyLibrary/MyLibrary.csproj
```

**Why this approach?**
- ✅ Better organization from the start
- ✅ Easier to add more projects later
- ✅ Works seamlessly with Visual Studio
- ✅ Clear project structure
- ✅ Better for team collaboration

#### ⚠️ **Alternative: Project First (Then Solution)**

You can create a project first and add a solution later:

```bash
# 1. Create project first
dotnet new webapi -n MyApi

# 2. Create solution later
dotnet new sln -n MySolution

# 3. Add existing project to solution
dotnet sln add MyApi/MyApi.csproj
```

**When to use:**
- Quick prototyping
- Single project that might grow later
- You're not sure if you'll need multiple projects

### Summary

| Aspect | Project | Solution |
|--------|---------|----------|
| **File** | `.csproj` | `.sln` or `.slnx` |
| **Purpose** | Contains code and builds output | Organizes multiple projects |
| **Required?** | ✅ Yes (always needed) | ❌ No (optional, but recommended for multi-project) |
| **Can work without?** | ❌ No | ✅ Yes (for single projects) |
| **Recommended for** | All .NET applications | Multi-project applications, Visual Studio |

**Bottom Line:**
- **Single project?** You can skip the solution file and work directly with the project.
- **Multiple projects or using Visual Studio?** Create a solution first, then add projects to it.
- **Best practice:** Start with a solution if you might add more projects later (tests, libraries, etc.).

## Creating New Projects

### Basic Syntax

```bash
dotnet new <template> -n <project-name>
```

The `-n` flag (short for `--name`) specifies the project name.

### Common Project Templates

#### Web API (ASP.NET Core)
```bash
dotnet new webapi -n MyApi
```

#### Web App (MVC/Razor Pages)
```bash
dotnet new webapp -n MyWebApp
```

#### Console Application
```bash
dotnet new console -n MyConsoleApp
```

#### Class Library
```bash
dotnet new classlib -n MyLibrary
```

#### Blazor Server App
```bash
dotnet new blazorserver -n MyBlazorApp
```

#### Blazor WebAssembly App
```bash
dotnet new blazorwasm -n MyBlazorApp
```

#### Minimal API
```bash
dotnet new web -n MyMinimalApi
```

### Useful Template Options

**Specify framework version:**
```bash
dotnet new webapi -n MyApi -f net10.0
```

**Create without top-level statements (Program.cs with Main method):**
```bash
dotnet new webapi -n MyApi --use-program-main
```

**Create with no HTTPS (HTTP only):**
```bash
dotnet new webapi -n MyApi --no-https
```

**Create with OpenAPI/Swagger enabled:**
```bash
dotnet new webapi -n MyApi --use-openapi
```

### List Available Templates

To see all available templates:
```bash
dotnet new list
```

To see templates for a specific type:
```bash
dotnet new list web
```

## Solution Files

### Creating a Solution File

**Important:** `dotnet new` only creates the project file (`.csproj`), not a solution file. You need to create the solution separately.

```bash
dotnet new sln -n SolutionName
```

This creates a `.sln` file (solution file) in the current directory.

**Other options:**
```bash
# Create solution with same name as current directory
dotnet new sln

# Create solution in specific directory
dotnet new sln --output MySolution
```

### Adding Projects to Solution

```bash
# Add a project to the solution
dotnet sln add ProjectName/ProjectName.csproj
```

Or if you're in the project directory:
```bash
dotnet sln add ProjectName.csproj
```

### Complete Workflow: Create Solution + Project

**Note:** Both commands run in the same directory. The solution file and project folder will be created side-by-side.

```bash
# 1. Create the solution (creates Expenses.API.sln in current directory)
dotnet new sln -n Expenses.API

# 2. Create the project (creates Expenses.API/ folder with project inside)
dotnet new webapi -n Expenses.API

# 3. Add project to solution
dotnet sln add Expenses.API/Expenses.API.csproj
```

**Resulting structure:**
```
CurrentDirectory/
├── Expenses.API.sln              ← Solution file (.sln format)
└── Expenses.API/                 ← Project folder
    └── Expenses.API.csproj       ← Project file
```

### About .sln vs .slnx

- **`.sln`** = Traditional solution format (created by `dotnet new sln`)
  - Text-based format
  - Works with Visual Studio, VS Code, Rider, and all CLI tools
  - Universal and widely supported
  - **This is what `dotnet new sln` creates**

- **`.slnx`** = New Visual Studio 2022 format
  - XML/JSON-based format (more human-readable)
  - Introduced in Visual Studio 2022
  - Created automatically by Visual Studio 2022 or via migration
  - **NOT created by `dotnet new sln` command**

### Converting .sln to .slnx

**Yes!** You can convert an existing `.sln` file to `.slnx` using the migrate command:

**Requirements:**
- .NET SDK version 9.0.200 or later

**Convert command:**
```bash
dotnet sln migrate YourSolution.sln
```

This command:
- Reads your existing `.sln` file
- Creates a new `.slnx` file with the same name
- Preserves all project references and configurations

**Example:**
```bash
# Convert Expenses.API.sln to Expenses.API.slnx
dotnet sln migrate Expenses.API.sln
```

**Important Notes:**
- The original `.sln` file remains - you'll have both files
- It's recommended to remove the old `.sln` file after migration to avoid confusion
- Both formats work, but `.slnx` is the newer, preferred format in Visual Studio 2022

### Converting in Visual Studio

You can also convert in Visual Studio 2022:
1. Right-click the solution node in **Solution Explorer**
2. Select **File → Save Solution As...**
3. In the "Save as type" dropdown, choose **XML Solution File (*.slnx)**
4. Click **Save**

## Package Management

### Installing Packages via Command Line

The recommended way to install NuGet packages:

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

## Project Setup Commands

### Restore Dependencies

```bash
dotnet restore
```

### Build Project

```bash
dotnet build
```

### Run Project

```bash
dotnet run
```

### Run with Hot Reload (Watch Mode)

```bash
dotnet watch run
```

### Run with Specific Launch Profile

```bash
dotnet watch run --launch-profile https
dotnet run --launch-profile https
```

### Clean Build Artifacts

```bash
dotnet clean
```

### Publish Project

```bash
dotnet publish -c Release
```

### Entity Framework Commands

**Install EF Core tools (if not already installed):**
```bash
dotnet tool install --global dotnet-ef
```

**Create a migration:**
```bash
dotnet ef migrations add MigrationName
```

**Apply migrations to database:**
```bash
dotnet ef database update
```

**Remove last migration:**
```bash
dotnet ef migrations remove
```

**List migrations:**
```bash
dotnet ef migrations list
```

## Complete Project Setup Example

Here's a complete example of setting up a new Web API project from scratch:

```bash
# 1. Create solution
dotnet new sln -n Expenses.API

# 2. Create Web API project
dotnet new webapi -n Expenses.API -f net10.0

# 3. Add project to solution
dotnet sln add Expenses.API/Expenses.API.csproj

# 4. Navigate to project directory
cd Expenses.API

# 5. Install Entity Framework Core tools (if needed)
dotnet tool install --global dotnet-ef

# 6. Install required packages
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Swashbuckle.AspNetCore

# 7. Restore dependencies
dotnet restore

# 8. Build the project
dotnet build

# 9. Run the project
dotnet watch run --launch-profile https
```

## Quick Reference

| Command | Description |
|---------|-------------|
| `dotnet new <template> -n <name>` | Create new project |
| `dotnet new sln -n <name>` | Create new solution (.sln format) |
| `dotnet sln add <project>` | Add project to solution |
| `dotnet sln migrate <solution.sln>` | Convert .sln to .slnx format |
| `dotnet add package <name>` | Install NuGet package |
| `dotnet remove package <name>` | Remove NuGet package |
| `dotnet restore` | Restore dependencies |
| `dotnet build` | Build project |
| `dotnet run` | Run project |
| `dotnet watch run` | Run with hot reload |
| `dotnet clean` | Clean build artifacts |
| `dotnet ef migrations add <name>` | Create EF migration |
| `dotnet ef database update` | Apply migrations |


