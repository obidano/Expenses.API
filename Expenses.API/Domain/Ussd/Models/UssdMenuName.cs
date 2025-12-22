namespace Expenses.API.Domain.Ussd.Models {
    /// <summary>
    /// Enumeration of USSD menu names to avoid string errors.
    /// </summary>
    public enum UssdMenuName {
        MainMenu,
        AddTransaction,
        TransactionHistory,
        AccountBalance
    }
}
