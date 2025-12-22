# Quick Start Guide

## 1. Start Redis (if not running)
```bash
redis-server
```

## 2. Run the Test Console
```bash
cd D:\c#\Expenses.API\Expenses.TestConsole
dotnet run
```

## 3. Test a Simple Flow

When prompted:
1. Select option `1` (Run USSD Test Session)
2. Enter phone number: `+1234567890`
3. The main menu will appear

### Example: Add an Expense Transaction

Follow this sequence:
```
Your input: 1          # Add transaction
Your input: 2          # Expense
Your input: 50.00      # Amount
Your input: Coffee     # Description
Your input: Food       # Category
Your input: 1          # Confirm (Yes)
```

You should see:
```
Response Type: END
-------------------------------------------
✓ Transaction saved successfully!

Transaction Details:
Type: Expense
Amount: $50.00
Description: Coffee
Category: Food
-------------------------------------------
```

## 4. Verify in Database

You can verify the transaction was saved by:
- Checking the API's transaction list
- Running a SQL query: `SELECT * FROM Transactions ORDER BY CreatedAt DESC`
- Using the USSD menu option 2 (Transaction history)

## 5. Clear States (Optional)

After testing, you can clear all session states:
1. Select option `2` from the main menu
2. Confirm with `y`

This will clear all USSD states from Redis.

---

## Quick Automated Test Script (Optional)

If you want to create automated tests, you can modify `Program.cs` to include predefined test scenarios. Example:

```csharp
// Add this method to Program.cs for automated testing
async Task RunAutomatedTest()
{
    Console.WriteLine("Running automated test...\n");
    
    var phoneNumber = "+9999999999";
    var sessionId = Guid.NewGuid().ToString();
    
    // Sequence: Main Menu → Add Transaction → Expense → Amount → Description → Category → Confirm
    var inputs = new[] { "", "1", "2", "25.50", "Test Coffee", "Food", "1" };
    
    foreach (var input in inputs)
    {
        var request = new UssdRequest
        {
            PhoneNumber = phoneNumber,
            SessionId = sessionId,
            Input = input
        };
        
        var response = await ProcessUssdRequest(request);
        Console.WriteLine($"Input: '{input}'");
        Console.WriteLine($"Response: {response.Message}\n");
        Console.WriteLine("---\n");
        
        if (response.Type == "END")
            break;
    }
    
    Console.WriteLine("Automated test completed!\n");
}
```

Then add it to the menu:
```csharp
Console.WriteLine("4. Run Automated Test");

case "4":
    await RunAutomatedTest();
    break;
```
