# USSD Controller Test Console

This console application allows you to test the USSD Controller functionality without needing to run the full API and make HTTP requests.

## Features

- **Interactive USSD Session Testing**: Simulates real USSD sessions with state management
- **Full Controller Logic**: Replicates the exact logic from `UssdController.cs`
- **State Management**: Uses Redis for session state persistence (same as the API)
- **Database Integration**: Connects to the same SQL Server database for transactions
- **Clear State Option**: Utility to clear all USSD states from Redis

## Prerequisites

1. **SQL Server**: Make sure your database is running and the connection string in `appsettings.json` is correct
2. **Redis**: Ensure Redis is running on `localhost:6379` (or update the connection string)
3. **.NET 10.0**: Required to run the application

## Configuration

Edit `appsettings.json` to match your environment:

```json
{
  "ConnectionStrings": {
    "Default": "Data Source=YOUR_SERVER;Initial Catalog=asp_db_transactions;...",
    "Redis": "localhost:6379"
  }
}
```

## How to Run

### Option 1: Using dotnet CLI

```bash
cd D:\c#\Expenses.API\Expenses.TestConsole
dotnet run
```

### Option 2: Using the executable

```bash
cd D:\c#\Expenses.API\Expenses.TestConsole\bin\Debug\net10.0
Expenses.TestConsole.exe
```

## Usage

When you run the console app, you'll see a menu with three options:

### 1. Run USSD Test Session

This option starts an interactive USSD session where you can:

1. Enter a phone number (e.g., `+1234567890`)
2. The app will show you the main menu:
   ```
   Welcome to Expenses App
   
   1. Add transaction
   2. Transaction history
   3. Account balance
   
   Select an option:
   ```
3. Enter your choice and follow the prompts
4. The session continues until you complete a flow or the session ends

**Example Session:**

```
Enter phone number: +1234567890

-------------------------------------------
Response Type: CON
-------------------------------------------
Welcome to Expenses App

1. Add transaction
2. Transaction history
3. Account balance

Select an option:
-------------------------------------------

Your input: 1

-------------------------------------------
Response Type: CON
-------------------------------------------
Add Transaction

Select transaction type:
1. Income
2. Expense
-------------------------------------------

Your input: 2
... (continues)
```

### 2. Clear All States (Redis)

This utility option clears all USSD session states from Redis. Useful for:
- Resetting test sessions
- Cleaning up after testing
- Starting fresh

The app will ask for confirmation before clearing.

### 3. Exit

Closes the console application.

## Testing Different Scenarios

### Test Case 1: Add Income Transaction
```
Phone: +1234567890
Input sequence: 1 → 1 → 500 → Salary → Food → 1
Expected: Transaction saved successfully
```

### Test Case 2: Add Expense Transaction
```
Phone: +1234567890
Input sequence: 1 → 2 → 50 → Groceries → Food → 1
Expected: Transaction saved successfully
```

### Test Case 3: Invalid Input
```
Phone: +1234567890
Input sequence: 9
Expected: Error message and prompt to select valid option
```

### Test Case 4: Back Navigation
```
Phone: +1234567890
Input sequence: 1 → 2 → 0 (back) → 1 (select different option)
Expected: Return to previous menu
```

## How It Works

The console app mimics the exact behavior of `UssdController.cs`:

1. **Dependency Injection**: Sets up the same services (DbContext, Redis, USSD handlers)
2. **Request Processing**: Creates `UssdRequest` objects based on user input
3. **State Management**: 
   - Retrieves or creates state for the phone number
   - Updates state through the `MainMenuHandler`
   - Saves state to Redis for CON responses
   - Clears state for END responses
4. **Response Handling**: Displays the response and determines if session should continue

## Troubleshooting

### Redis Connection Error
```
WARNING: Could not connect to Redis: ...
```
- Make sure Redis is running: `redis-server`
- Check the connection string in `appsettings.json`

### Database Connection Error
```
ERROR: Database connection string 'Default' is missing!
```
- Verify the connection string in `appsettings.json`
- Ensure SQL Server is running
- Test connection using SSMS or similar tool

### Handler Errors
If you encounter errors during USSD flow:
- Check that all handlers are properly registered in `DependencyInjection.cs`
- Ensure database migrations are up to date
- Verify that the transaction service can access the database

## Architecture

```
Program.cs (Console App)
    ↓
ProcessUssdRequest()  ← Replicates UssdController.ProcessUssdRequest()
    ↓
UssdStateService (Redis)
    ↓
MainMenuHandler
    ↓
    ├→ AddTransactionHandler
    ├→ TransactionHistoryHandler
    └→ AccountBalanceHandler
         ↓
    TransactionService (Database)
```

## Development Notes

- The console app references the `Expenses.API` project directly
- All business logic is shared with the API (no duplication)
- State management is identical to the production API
- Perfect for testing USSD flows without HTTP overhead
