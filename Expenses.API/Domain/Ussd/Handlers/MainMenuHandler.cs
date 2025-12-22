using Expenses.API.Domain.Transaction.Services;
using Expenses.API.Domain.Ussd.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Expenses.API.Domain.Ussd.Handlers {
    public class MainMenuHandler(IServiceProvider serviceProvider, ILogger<MainMenuHandler> logger) : IUssdMenuHandler {

        public async Task<UssdHandlerResult> HandleAsync(UssdRequest request, UssdState state) {
            try
            {
            // If not in main menu, route to the appropriate handler
            // Each handler is responsible for its own submenus/steps
            if (state.CurrentMenu != UssdMenuName.MainMenu.ToString()) {
                var result = await RouteToHandlerAsync(request, state);
                
                // If handler navigated back to main menu (state changed), show main menu immediately
                if (result.UpdatedState.CurrentMenu == UssdMenuName.MainMenu.ToString()) {
                    var menuRequest = new UssdRequest {
                        PhoneNumber = request.PhoneNumber,
                        Input = "",
                        SessionId = request.SessionId
                    };
                    return await HandleAsync(menuRequest, result.UpdatedState);
                }
                
                return result;
            }

            // Main menu logic - show menu or route based on input
            if (string.IsNullOrWhiteSpace(request.Input)) {
                var menu = "Welcome to Expenses App\n\n" +
                          "1. Add transaction\n" +
                          "2. Transaction history\n" +
                          "3. Account balance\n\n" +
                          "Select an option:";
                
                return new UssdHandlerResult(UssdResponse.Continue(menu), state);
            }

            // Route to selected menu handler
            var input = request.Input.Trim();
            state.CurrentMenu = input switch {
                "1" => UssdMenuName.AddTransaction.ToString(),
                "2" => UssdMenuName.TransactionHistory.ToString(),
                "3" => UssdMenuName.AccountBalance.ToString(),
                _ => UssdMenuName.MainMenu.ToString()
            };

            // Reset step when navigating to new menu
            state.CurrentStep = 0;

            // If invalid option, show error
            if (state.CurrentMenu == UssdMenuName.MainMenu.ToString()) {
                return new UssdHandlerResult(UssdResponse.Continue("Invalid option. Please select 1, 2, or 3:"), state);
            }

            // Route to the selected handler with empty input (menu selection shouldn't be passed to submenu)
            var emptyRequest = new UssdRequest {
                PhoneNumber = request.PhoneNumber,
                Input = "",
                SessionId = request.SessionId
            };
            return await RouteToHandlerAsync(emptyRequest, state);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, 
                    "Exception in MainMenuHandler.HandleAsync. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}, CurrentMenu: {CurrentMenu}, CurrentStep: {CurrentStep}, Input: {Input}",
                    request.PhoneNumber, 
                    request.SessionId, 
                    state.CurrentMenu, 
                    state.CurrentStep, 
                    request.Input);
                
                return new UssdHandlerResult(
                    UssdResponse.End("An error occurred. Please dial again to restart.\n\nThank you for using Expenses App!"),
                    state
                );
            }
        }

        private async Task<UssdHandlerResult> RouteToHandlerAsync(UssdRequest request, UssdState state) {
            // Each handler is responsible for routing to its own submenus/steps
            IUssdMenuHandler handler = state.CurrentMenu switch {
                var menu when menu == UssdMenuName.AddTransaction.ToString() => new AddTransactionHandler(
                    serviceProvider.GetRequiredService<TransactionService>(),
                    serviceProvider.GetRequiredService<ILogger<AddTransactionHandler>>()),
                var menu when menu == UssdMenuName.TransactionHistory.ToString() => new TransactionHistoryHandler(
                    serviceProvider.GetRequiredService<TransactionService>(),
                    serviceProvider.GetRequiredService<ILogger<TransactionHistoryHandler>>()),
                var menu when menu == UssdMenuName.AccountBalance.ToString() => new AccountBalanceHandler(
                    serviceProvider.GetRequiredService<TransactionService>(),
                    serviceProvider.GetRequiredService<ILogger<AccountBalanceHandler>>()),
                _ => this // Fallback to main menu
            };

            return await handler.HandleAsync(request, state);
        }
    }
}
