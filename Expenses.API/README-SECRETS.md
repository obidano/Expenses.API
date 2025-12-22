# Production Secrets Configuration

This project uses environment variables to store sensitive production secrets (passwords, connection strings, etc.) instead of committing them to version control.

## Setup Instructions

### For Local Development/Testing:

Set environment variables before running the application:

**PowerShell:**
```powershell
$env:SQL_PASSWORD="YourSQLPassword"
$env:REDIS_PASSWORD="YourRedisPassword"
$env:ASPNETCORE_ENVIRONMENT="Production"
dotnet run
```

**Command Prompt (CMD):**
```cmd
set SQL_PASSWORD=YourSQLPassword
set REDIS_PASSWORD=YourRedisPassword
set ASPNETCORE_ENVIRONMENT=Production
dotnet run
```

**Linux/Mac:**
```bash
export SQL_PASSWORD="YourSQLPassword"
export REDIS_PASSWORD="YourRedisPassword"
export ASPNETCORE_ENVIRONMENT="Production"
dotnet run
```

### For Azure App Service Deployment:

1. Go to Azure Portal → Your App Service → Configuration → Application settings
2. Add the following Application Settings:
   - `SQL_PASSWORD`: Your SQL Server password
   - `REDIS_PASSWORD`: Your Redis access key
   - `ASPNETCORE_ENVIRONMENT`: `Production`

### For Azure Container Instances/Apps:

Set environment variables in your deployment configuration:
- `SQL_PASSWORD`
- `REDIS_PASSWORD`
- `ASPNETCORE_ENVIRONMENT=Production`

### For GitHub Actions/CI/CD:

Add secrets to your GitHub repository:
1. Go to Repository → Settings → Secrets and variables → Actions
2. Add secrets:
   - `SQL_PASSWORD`
   - `REDIS_PASSWORD`
3. Use them in your workflow:
```yaml
env:
  SQL_PASSWORD: ${{ secrets.SQL_PASSWORD }}
  REDIS_PASSWORD: ${{ secrets.REDIS_PASSWORD }}
```

## Connection String Placeholders

The `appsettings.Production.json` file uses placeholders that are replaced at runtime:
- `{SQL_PASSWORD}` → Replaced with `SQL_PASSWORD` environment variable
- `{REDIS_PASSWORD}` → Replaced with `REDIS_PASSWORD` environment variable

## Security Best Practices

✅ **DO:**
- Use environment variables for secrets
- Use Azure Key Vault for production (recommended)
- Keep `appsettings.Production.json` in git (without secrets)
- Use placeholders in configuration files

❌ **DON'T:**
- Commit passwords or secrets to git
- Hardcode secrets in source code
- Share secrets in plain text

## Alternative: Azure Key Vault

For better security, consider using Azure Key Vault:
1. Store secrets in Azure Key Vault
2. Configure your app to use Key Vault references
3. Use Managed Identity for authentication
