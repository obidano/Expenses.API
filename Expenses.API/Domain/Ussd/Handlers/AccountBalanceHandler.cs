using Expenses.API.Domain.Transaction.Services;
using Expenses.API.Domain.Ussd.Models;
using Microsoft.Extensions.Logging;

namespace Expenses.API.Domain.Ussd.Handlers {
    public class AccountBalanceHandler(TransactionService transactionService, ILogger<AccountBalanceHandler> logger) : BaseUssdHandler {
        // Visual formatting constants
        private const string VisualDivider = "─────────────────────";

        // Error messages
        private static class ErrorMessages {
            public const string GenericError = "An error occurred. Please dial again to restart.\n\nThank you for using Expenses App!";
            public const string BalanceLoadError = "Error loading account balance.\n\nPlease try again later.";
        }

        // Step name enum
        private enum StepName {
            BalanceDisplay
        }

        // Step registry - defines the order of steps
        private static readonly StepName[] StepOrder = {
            StepName.BalanceDisplay        // Automatically becomes step 0
        };

        // Step constants - derived from StepOrder array position
        private static int StepBalanceDisplay => Array.IndexOf(StepOrder, StepName.BalanceDisplay);

        // Override base class properties
        protected override UssdMenuName MenuName => UssdMenuName.AccountBalance;
        protected override int TotalSteps => StepOrder.Length;

        public override async Task<UssdHandlerResult> HandleAsync(UssdRequest request, UssdState state) {
            try {
                // If menu changed to MainMenu (from navigation commands), return immediately
                if (state.CurrentMenu == UssdMenuName.MainMenu.ToString()) {
                    return new UssdHandlerResult(UssdResponse.Continue(""), state);
                }

                // Route to appropriate step handler
                if (state.CurrentStep == StepBalanceDisplay) {
                    return await HandleBalanceDisplayStep(request, state);
                }

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.GenericError),
                    state
                );
            } catch (Exception ex) {
                logger.LogError(ex,
                    "Exception in AccountBalanceHandler.HandleAsync. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}, CurrentMenu: {CurrentMenu}, CurrentStep: {CurrentStep}, Input: {Input}",
                    request.PhoneNumber,
                    request.SessionId,
                    state.CurrentMenu,
                    state.CurrentStep,
                    request.Input);

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.GenericError),
                    state
                );
            }
        }

        // Returns the prompt text for the Balance Display step
        private string GetBalanceDisplayPrompt() {
            return "Account Balance:";
        }

        private async Task<UssdHandlerResult> HandleBalanceDisplayStep(UssdRequest request, UssdState state) {
            var currentStep = StepBalanceDisplay;

            // Load balance data first
            var balanceResult = await LoadBalance();
            if (balanceResult == null) {
                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.BalanceLoadError),
                    state
                );
            }

            // For empty input and non-empty input, handle the same way
            // ProcessStepCompletion will determine if this is the last step and END appropriately
            if (string.IsNullOrWhiteSpace(request.Input)) {
                // First entry - go directly to completion (which will END since this is last step)
                return await ProcessStepCompletion(currentStep, state, balanceResult);
            }

            // For non-empty input, use standard preprocessing to handle navigation
            var prompt = $"{GetBalanceDisplayPrompt()}\n{FormatBalanceDisplay(balanceResult)}";
            var preprocessResult = HandleStepPreprocessing(request, state, currentStep, prompt);
            if (preprocessResult != null) {
                return preprocessResult;
            }

            // Any other input completes the step via ProcessStepCompletion
            return await ProcessStepCompletion(currentStep, state, balanceResult);
        }

        /// <summary>
        /// Centralized method to process step completion.
        /// This is the ONLY method that should call CompleteAccountBalance.
        /// Automatically ends session if at the final step, otherwise proceeds to next step.
        /// </summary>
        private async Task<UssdHandlerResult> ProcessStepCompletion(int currentStep, UssdState state, Transaction.Dto.BalanceResult balanceResult) {
            // If this is the final step, end the session
            if (IsLastStep(currentStep)) {
                return await CompleteAccountBalance(state, balanceResult);
            }

            // Otherwise, proceed to the next step (not applicable here since we only have one step)
            var nextStep = GetNextStep(currentStep);
            state.CurrentStep = nextStep;

            // Check if the next step is the final step - if so, complete and END
            if (IsLastStep(nextStep)) {
                return await CompleteAccountBalance(state, balanceResult);
            }

            var nextStepPrompt = GetPromptForStep(nextStep);
            var navOptions = GetNavigationOptions(state.CurrentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{nextStepPrompt}{navOptions}"),
                state
            );
        }

        /// <summary>
        /// Completes the account balance flow and ends the session.
        /// Similar to CompleteTransactionHistory in TransactionHistoryHandler.
        /// </summary>
        private Task<UssdHandlerResult> CompleteAccountBalance(UssdState state, Transaction.Dto.BalanceResult balanceResult) {
            try {
                // Clear state
                ClearTransactionData(state);
                state.CurrentStep = 0;

                return Task.FromResult(new UssdHandlerResult(
                    UssdResponse.End($"{GetBalanceDisplayPrompt()}\n{FormatBalanceDisplay(balanceResult)}\n\nThank you for using Expenses App!"),
                    state
                ));
            } catch (Exception ex) {
                logger.LogError(ex,
                    "Exception in AccountBalanceHandler.CompleteAccountBalance. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}",
                    state.PhoneNumber,
                    state.SessionId);

                return Task.FromResult(new UssdHandlerResult(
                    UssdResponse.End("Error loading account balance.\n\nPlease try again later."),
                    state
                ));
            }
        }

        /// <summary>
        /// Gets the prompt text for a valid step number.
        /// Implements the abstract method from BaseUssdHandler.
        /// </summary>
        protected override string GetPromptForStepInternal(int step) {
            var stepName = StepOrder[step];
            return GetPromptForStepName(stepName);
        }

        /// <summary>
        /// Dynamically gets the prompt text for a given step by calling its prompt method.
        /// </summary>
        private string GetPromptForStepName(StepName stepName) {
            return stepName switch {
                StepName.BalanceDisplay => GetBalanceDisplayPrompt(),
                _ => "Continue:"
            };
        }

        /// <summary>
        /// Loads the balance from the database.
        /// </summary>
        private async Task<Transaction.Dto.BalanceResult?> LoadBalance() {
            try {
                return await transactionService.calculateBalance();
            } catch (Exception ex) {
                logger.LogError(ex, "Error loading balance in AccountBalanceHandler.LoadBalance");
                return null;
            }
        }

        /// <summary>
        /// Formats the balance result for display.
        /// </summary>
        private string FormatBalanceDisplay(Transaction.Dto.BalanceResult balanceResult) {
            return $"{VisualDivider}\n" +
                   $"Current Balance: ${balanceResult.Balance:F2}\n" +
                   $"Total Income: ${balanceResult.TotalIncome:F2}\n" +
                   $"Total Expense: ${balanceResult.TotalExpense:F2}\n" +
                   $"Transactions: {balanceResult.TransactionCount}";
        }

        /// <summary>
        /// Clears data for the current step when navigating back.
        /// </summary>
        protected override void ClearCurrentStepData(UssdState state, int step) {
            // No state data to clear for balance display
            // This handler doesn't store any data in the state
        }

        /// <summary>
        /// Clears all account balance-related data from the state.
        /// </summary>
        protected override void ClearTransactionData(UssdState state) {
            // No transaction data to clear for balance display
            // This handler doesn't store any data in the state
        }
    }
}
