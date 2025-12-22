using Expenses.API.Domain.Transaction;
using Expenses.API.Domain.Transaction.Services;
using Expenses.API.Domain.Ussd.Models;
using Microsoft.Extensions.Logging;

namespace Expenses.API.Domain.Ussd.Handlers {
    public class TransactionHistoryHandler(TransactionService transactionService, ILogger<TransactionHistoryHandler> logger) : BaseUssdHandler {
        // Visual formatting constants
        private const string VisualDivider = "─────────────────────";

        // Pagination constants
        private const int TransactionsPerPage = 5;
        private const string NextPageCommand = "*";
        private const string PreviousPageCommand = "**";

        // Error messages
        private static class ErrorMessages {
            public const string GenericError = "An error occurred. Please dial again to restart.\n\nThank you for using Expenses App!";
            public const string TransactionNotFound = "Transaction not found.\n\nThank you for using Expenses App!";
            public const string TransactionNotFoundWithHistory = "Transaction not found.";
            public const string TransactionLoadError = "Error loading transaction details.\n\nPlease try again later.";
            public const string AlreadyOnLastPage = "Already on last page.";
            public const string AlreadyOnFirstPage = "Already on first page.";
            public const string InvalidOption = "Invalid option.";
            public const string NoTransactionsFound = "No transactions found.";
        }

        // Step name enum
        private enum StepName {
            TransactionList,
            TransactionDetail
        }

        // Step registry - defines the order of steps
        private static readonly StepName[] StepOrder = {
            StepName.TransactionList,        // Automatically becomes step 0
            StepName.TransactionDetail       // Automatically becomes step 1
        };

        // Step constants - derived from StepOrder array position
        private static int StepTransactionList => Array.IndexOf(StepOrder, StepName.TransactionList);
        private static int StepTransactionDetail => Array.IndexOf(StepOrder, StepName.TransactionDetail);

        // Override base class properties
        protected override UssdMenuName MenuName => UssdMenuName.TransactionHistory;
        protected override int TotalSteps => StepOrder.Length;

        public override async Task<UssdHandlerResult> HandleAsync(UssdRequest request, UssdState state) {
            try {
                // If menu changed to MainMenu (from navigation commands), return immediately
                if (state.CurrentMenu == UssdMenuName.MainMenu.ToString()) {
                    return new UssdHandlerResult(UssdResponse.Continue(""), state);
                }

                // Ensure state is TransactionHistoryState
                var historyState = EnsureTransactionHistoryState(state);

                // Route to appropriate step handler
                if (historyState.CurrentStep == StepTransactionList) {
                    return await HandleTransactionListStep(request, historyState);
                }

                if (historyState.CurrentStep == StepTransactionDetail) {
                    return await HandleTransactionDetailStep(request, historyState);
                }

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.GenericError),
                    historyState
                );
            } catch (Exception ex) {
                logger.LogError(ex,
                    "Exception in TransactionHistoryHandler.HandleAsync. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}, CurrentMenu: {CurrentMenu}, CurrentStep: {CurrentStep}, Input: {Input}",
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

        /// <summary>
        /// Ensures the state is of type TransactionHistoryState.
        /// Converts from UssdState if needed (first entry to menu).
        /// </summary>
        private TransactionHistoryState EnsureTransactionHistoryState(UssdState state) {
            if (state is TransactionHistoryState existingState) {
                return existingState;
            }

            // First entry to TransactionHistory menu
            return new TransactionHistoryState {
                PhoneNumber = state.PhoneNumber,
                SessionId = state.SessionId,
                CurrentMenu = state.CurrentMenu,
                CurrentStep = state.CurrentStep,
                CreatedAt = state.CreatedAt,
                LastUpdated = state.LastUpdated,
                TransactionPage = 0,
                SelectedTransactionId = null
            };
        }

        // Returns the prompt text for the Transaction List step
        private string GetTransactionListPrompt() {
            return "Transaction History:";
        }

        private async Task<UssdHandlerResult> HandleTransactionListStep(UssdRequest request, TransactionHistoryState state) {
            var currentStep = StepTransactionList;

            // Handle empty input - show current page
            if (string.IsNullOrWhiteSpace(request.Input)) {
                return await ShowTransactionListPage(state, currentStep);
            }

            var input = request.Input.Trim();

            // Handle navigation commands first (# for back, ## for main menu)
            HandleNavigation(request, state, currentStep);

            // If navigation changed the step or menu
            if (state.CurrentStep != currentStep || state.CurrentMenu != MenuName.ToString()) {
                return await HandleTransactionListNavigationChange(state, currentStep);
            }

            // Load transactions for pagination and selection handling
            var (transactions, totalCount) = await LoadTransactions(state);

            // Handle pagination commands
            var paginationResult = await HandleTransactionListPagination(input, state, currentStep, transactions, totalCount);
            if (paginationResult != null) {
                return paginationResult;
            }

            // Handle transaction selection
            if (TryParseTransactionSelection(input, state, out var transactionId)) {
                state.SelectedTransactionId = transactionId;
                return await ProcessStepCompletion(currentStep, state);
            }

            // Invalid input
            return ShowTransactionSelectionError(state, currentStep, transactions, totalCount);
        }

        // Returns the prompt text for the Transaction Detail step
        private string GetTransactionDetailPrompt() {
            return "Transaction Details:";
        }

        /// <summary>
        /// Displays the current page of transactions with navigation options.
        /// </summary>
        private async Task<UssdHandlerResult> ShowTransactionListPage(TransactionHistoryState state, int currentStep) {
            var (transactions, totalCount) = await LoadTransactions(state);
            CacheDisplayedTransactions(transactions, state);
            var navigationOptions = GetNavigationOptionsForTransactionList(currentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{GetTransactionListPrompt()}\n{FormatTransactionList(transactions, state.TransactionPage, totalCount, currentStep)}{navigationOptions}"),
                state
            );
        }

        /// <summary>
        /// Handles navigation changes in the transaction list step (back or main menu).
        /// </summary>
        private async Task<UssdHandlerResult> HandleTransactionListNavigationChange(TransactionHistoryState state, int currentStep) {
            // Reset pagination when leaving this step
            state.TransactionPage = 0;

            if (state.CurrentStep != currentStep) {
                var nextPrompt = GetPromptForStep(state.CurrentStep);
                var navOptions = GetNavigationOptions(state.CurrentStep);
                return new UssdHandlerResult(
                    UssdResponse.Continue($"{nextPrompt}{navOptions}"),
                    state
                );
            }

            if (state.CurrentMenu != MenuName.ToString()) {
                return new UssdHandlerResult(UssdResponse.Continue(""), state);
            }

            // Fallback
            return await ShowTransactionListPage(state, currentStep);
        }

        /// <summary>
        /// Handles pagination commands (next/previous page).
        /// Returns null if input is not a pagination command.
        /// </summary>
        private async Task<UssdHandlerResult?> HandleTransactionListPagination(
            string input, 
            TransactionHistoryState state, 
            int currentStep,
            List<Transaction.Transaction> transactions,
            int totalCount) {
            
            var totalPages = GetTotalPages(totalCount);

            if (input == NextPageCommand) {
                if (state.TransactionPage < totalPages - 1) {
                    state.TransactionPage++;
                    return await ShowTransactionListPage(state, currentStep);
                }
                // Already on last page
                var navOpts = GetNavigationOptionsForTransactionList(currentStep);
                return new UssdHandlerResult(
                    UssdResponse.Continue($"{ErrorMessages.AlreadyOnLastPage}\n\n{GetTransactionListPrompt()}\n{FormatTransactionList(transactions, state.TransactionPage, totalCount, currentStep)}{navOpts}"),
                    state
                );
            }

            if (input == PreviousPageCommand) {
                if (state.TransactionPage > 0) {
                    state.TransactionPage--;
                    return await ShowTransactionListPage(state, currentStep);
                }
                // Already on first page
                var navOpts = GetNavigationOptionsForTransactionList(currentStep);
                return new UssdHandlerResult(
                    UssdResponse.Continue($"{ErrorMessages.AlreadyOnFirstPage}\n\n{GetTransactionListPrompt()}\n{FormatTransactionList(transactions, state.TransactionPage, totalCount, currentStep)}{navOpts}"),
                    state
                );
            }

            return null; // Not a pagination command
        }

        /// <summary>
        /// Tries to parse and validate transaction selection input.
        /// </summary>
        private bool TryParseTransactionSelection(string input, TransactionHistoryState state, out string transactionId) {
            transactionId = string.Empty;

            if (!int.TryParse(input, out int selection)) {
                return false;
            }

            return state.DisplayedTransactions.TryGetValue(selection, out transactionId);
        }

        /// <summary>
        /// Returns error response for invalid transaction selection.
        /// </summary>
        private UssdHandlerResult ShowTransactionSelectionError(
            TransactionHistoryState state,
            int currentStep,
            List<Transaction.Transaction> transactions,
            int totalCount) {
            
            string errorMessage;
            if (state.DisplayedTransactions.Count > 0) {
                var validNumbers = string.Join(", ", state.DisplayedTransactions.Keys.OrderBy(k => k));
                errorMessage = $"{ErrorMessages.InvalidOption} Valid selections: {validNumbers}";
            } else {
                errorMessage = ErrorMessages.InvalidOption;
            }

            var navOpts = GetNavigationOptionsForTransactionList(currentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{errorMessage}\n\n{GetTransactionListPrompt()}\n{FormatTransactionList(transactions, state.TransactionPage, totalCount, currentStep)}{navOpts}"),
                state
            );
        }

        private async Task<UssdHandlerResult> HandleTransactionDetailStep(UssdRequest request, TransactionHistoryState state) {
            var currentStep = StepTransactionDetail;

            // Load transaction first to check if it exists
            var transaction = await LoadTransactionById(state.SelectedTransactionId);
            if (transaction == null) {
                // Transaction not found - go back to list with error message
                state.SelectedTransactionId = null;
                state.CurrentStep = GetPreviousStep(currentStep);
                
                var (transactions, totalCount) = await LoadTransactions(state);
                var navigationOptions = GetNavigationOptionsForTransactionList(state.CurrentStep);
                return new UssdHandlerResult(
                    UssdResponse.Continue($"{ErrorMessages.TransactionNotFoundWithHistory}\n\n{GetTransactionListPrompt()}\n{FormatTransactionList(transactions, state.TransactionPage, totalCount, state.CurrentStep)}{navigationOptions}"),
                    state
                );
            }

            // For empty input and non-empty input, handle the same way
            // ProcessStepCompletion will determine if this is the last step and END appropriately
            if (string.IsNullOrWhiteSpace(request.Input)) {
                // First entry - go directly to completion (which will END since this is last step)
                return await ProcessStepCompletion(currentStep, state, transaction);
            }

            // For non-empty input, use standard preprocessing to handle navigation
            var prompt = $"{GetTransactionDetailPrompt()}\n{FormatTransactionDetail(transaction)}";
            var preprocessResult = HandleStepPreprocessing(request, state, currentStep, prompt);
            if (preprocessResult != null) {
                return preprocessResult;
            }

            // Any other input completes the step via ProcessStepCompletion
            return await ProcessStepCompletion(currentStep, state, transaction);
        }

        /// <summary>
        /// Centralized method to process step completion.
        /// This is the ONLY method that should call CompleteTransactionHistory.
        /// Automatically ends session if at the final step, otherwise proceeds to next step.
        /// </summary>
        private async Task<UssdHandlerResult> ProcessStepCompletion(int currentStep, TransactionHistoryState state, Transaction.Transaction? transaction = null) {
            // If this is the final step, end the session
            if (IsLastStep(currentStep)) {
                // For the final step, we need the transaction data
                if (transaction == null) {
                    transaction = await LoadTransactionById(state.SelectedTransactionId);
                }
                return await CompleteTransactionHistory(state, transaction);
            }

            // Otherwise, proceed to the next step
            var nextStep = GetNextStep(currentStep);
            state.CurrentStep = nextStep;

            // Check if the next step is the final step - if so, complete and END instead of CONTINUE
            if (IsLastStep(nextStep)) {
                // For the final step, we need the transaction data
                if (transaction == null) {
                    transaction = await LoadTransactionById(state.SelectedTransactionId);
                }
                return await CompleteTransactionHistory(state, transaction);
            }

            // For non-final steps, get the prompt dynamically and continue
            var nextStepPrompt = GetPromptForStep(nextStep);
            var navOptions = GetNavigationOptions(state.CurrentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{nextStepPrompt}{navOptions}"),
                state
            );
        }

        /// <summary>
        /// Completes the transaction history flow and ends the session.
        /// Similar to SubmitTransaction in AddTransactionHandler.
        /// </summary>
        private async Task<UssdHandlerResult> CompleteTransactionHistory(TransactionHistoryState state, Transaction.Transaction? transaction) {
            try {
                // Transaction should be passed in, but handle null case
                if (transaction == null) {
                    transaction = await LoadTransactionById(state.SelectedTransactionId);
                }
                
                if (transaction == null) {
                    return new UssdHandlerResult(
                        UssdResponse.End(ErrorMessages.TransactionNotFound),
                        state
                    );
                }

                // Clear transaction data from state (similar to AddTransactionHandler after submission)
                ClearTransactionData(state);
                state.CurrentStep = 0;

                return new UssdHandlerResult(
                    UssdResponse.End($"{GetTransactionDetailPrompt()}\n{FormatTransactionDetail(transaction)}\n\nThank you for using Expenses App!"),
                    state
                );
            } catch (Exception ex) {
                logger.LogError(ex,
                    "Exception in TransactionHistoryHandler.CompleteTransactionHistory. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}, SelectedTransactionId: {SelectedTransactionId}",
                    state.PhoneNumber,
                    state.SessionId,
                    state.SelectedTransactionId);

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.TransactionLoadError),
                    state
                );
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
                StepName.TransactionList => GetTransactionListPrompt(),
                StepName.TransactionDetail => GetTransactionDetailPrompt(),
                _ => "Continue:"
            };
        }

        /// <summary>
        /// Loads transactions from the database with pagination.
        /// Returns (transactions list, total count).
        /// </summary>
        private async Task<(List<Transaction.Transaction> Transactions, int TotalCount)> LoadTransactions(TransactionHistoryState state) {
            var filters = new TransactionFilters {
                Page = state.TransactionPage + 1,    // Convert 0-based to 1-based
                PageSize = TransactionsPerPage,       // Only fetch 3 transactions
                SortDescending = true                 // Newest first
            };

            return await transactionService.getAllTransactions(filters);
        }

        /// <summary>
        /// Loads a single transaction by ID from the database.
        /// </summary>
        private async Task<Transaction.Transaction?> LoadTransactionById(string? transactionId) {
            if (string.IsNullOrWhiteSpace(transactionId)) {
                return null;
            }
            return await transactionService.getTransactionById(transactionId);
        }

        /// <summary>
        /// Formats a list of transactions for display with pagination indicators.
        /// </summary>
        private string FormatTransactionList(List<Transaction.Transaction> transactions, int currentPage, int totalCount, int currentStep) {
            if (transactions.Count == 0) {
                return ErrorMessages.NoTransactionsFound;
            }

            var transactionList = BuildTransactionListForPage(transactions, currentPage);
            var paginationIndicators = GetTransactionListPaginationIndicators(currentPage, totalCount, currentStep);
            
            if (string.IsNullOrEmpty(paginationIndicators)) {
                return transactionList.TrimEnd();
            }
            
            return $"{transactionList}\n\n{paginationIndicators}".TrimEnd();
        }

        /// <summary>
        /// Builds the numbered transaction list for a specific page.
        /// </summary>
        private string BuildTransactionListForPage(List<Transaction.Transaction> transactions, int currentPage) {
            var list = "";
            var startIndex = currentPage * TransactionsPerPage;
            
            for (int i = 0; i < transactions.Count; i++) {
                var tx = transactions[i];
                var displayNumber = startIndex + i + 1;
                list += $"{displayNumber}. {tx.Type}: ${tx.Amount:F2}";
                if (i < transactions.Count - 1) {
                    list += "\n";
                }
            }
            
            return list;
        }

        /// <summary>
        /// Gets pagination indicators (next/previous page) if multiple pages exist.
        /// Also includes # Back option to return to previous step.
        /// </summary>
        private string GetTransactionListPaginationIndicators(int currentPage, int totalCount, int currentStep) {
            var totalPages = GetTotalPages(totalCount);
            
            var indicators = "";
            
            // Add pagination indicators if multiple pages exist
            if (totalPages > 1) {
                if (currentPage < totalPages - 1) {
                    indicators += $"{NextPageCommand} Next page\n";
                }
                if (currentPage > 0) {
                    indicators += $"{PreviousPageCommand} Previous page\n";
                }
            }
            
            // Add # Back option to return to previous step (not previous page)
            // Always show # Back - if on first step, it goes to main menu (handled by base class)
            // Pagination indicators end with \n, so # Back will appear on a new line
            indicators += $"{BackCommand} Back";

            return indicators.TrimEnd();
        }

        /// <summary>
        /// Formats a single transaction for detail view.
        /// </summary>
        private string FormatTransactionDetail(Transaction.Transaction transaction) {
            return $"{VisualDivider}\n" +
                   $"Type: {transaction.Type}\n" +
                   $"Category: {transaction.Category}\n" +
                   $"Amount: ${transaction.Amount:F2}\n" +
                   $"Description: {transaction.Description}\n" +
                   $"Date: {transaction.CreatedAt:MMM dd, yyyy h:mm tt}";
        }

        private int GetTotalPages(int totalCount) {
            return (int)Math.Ceiling((double)totalCount / TransactionsPerPage);
        }

        /// <summary>
        /// Gets navigation options specifically for the transaction list step.
        /// The # Back option is now included in pagination indicators to avoid duplication.
        /// This method returns empty string since navigation is handled in GetTransactionListPaginationIndicators.
        /// </summary>
        private string GetNavigationOptionsForTransactionList(int currentStep) {
            // Navigation (# Back) is now included in pagination indicators
            // No additional navigation options needed here
            return "";
        }

        /// <summary>
        /// Clears data for the current step when navigating back.
        /// </summary>
        protected override void ClearCurrentStepData(UssdState state, int step) {
            if (state is not TransactionHistoryState historyState) {
                return;
            }

            if (step < 0 || step >= StepOrder.Length) {
                return;
            }

            var stepName = StepOrder[step];

            switch (stepName) {
                case StepName.TransactionList:
                    historyState.TransactionPage = 0;
                    historyState.DisplayedTransactions.Clear();
                    break;
                case StepName.TransactionDetail:
                    historyState.SelectedTransactionId = null;
                    break;
            }
        }

        /// <summary>
        /// Clears all transaction history-related data from the state.
        /// </summary>
        protected override void ClearTransactionData(UssdState state) {
            if (state is not TransactionHistoryState historyState) {
                return;
            }

            historyState.TransactionPage = 0;
            historyState.SelectedTransactionId = null;
            historyState.DisplayedTransactions.Clear();
        }

        /// <summary>
        /// Caches displayed transactions for later selection.
        /// Maps display number to transaction ID.
        /// </summary>
        private void CacheDisplayedTransactions(List<Transaction.Transaction> transactions, TransactionHistoryState state) {
            var startIndex = state.TransactionPage * TransactionsPerPage;
            
            for (int i = 0; i < transactions.Count; i++) {
                var displayNumber = startIndex + i + 1;
                state.DisplayedTransactions[displayNumber] = transactions[i].Id;
            }
        }
    }
}
