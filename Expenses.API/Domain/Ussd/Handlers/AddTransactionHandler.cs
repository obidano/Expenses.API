using Expenses.API.Domain.Transaction;
using Expenses.API.Domain.Transaction.Dto;
using Expenses.API.Domain.Transaction.Services;
using Expenses.API.Domain.Ussd.Models;
using Microsoft.Extensions.Logging;

namespace Expenses.API.Domain.Ussd.Handlers {
    public class AddTransactionHandler(TransactionService transactionService, ILogger<AddTransactionHandler> logger) : BaseUssdHandler {
        // Visual formatting constants
        private const string VisualDivider = "─────────────────────";

        // Pagination constants
        private const int CategoriesPerPage = 5;
        private const string NextPageCommand = "*";
        private const string PreviousPageCommand = "**";

        // Error messages
        private static class ErrorMessages {
            public const string InvalidTransactionType = "Invalid option. Please enter 1 for Income or 2 for Expense:";
            public const string InvalidAmount = "Invalid amount. Please enter a positive number:";
            public const string EmptyDescription = "Description cannot be empty. Please enter a description:";
            public const string TransactionCreationFailed = "Error creating transaction.\n\nPlease try again later.";
            public const string GenericError = "An error occurred. Please dial again to restart.\n\nThank you for using Expenses App!";
        }

        // Step name enum
        private enum StepName {
            TransactionType,
            Category,
            Amount,
            Description,
            Confirmation
        }

        // Step registry - defines the order of steps (can be reordered easily)
        // To reorder steps, just change the array order - step numbers update automatically
        // Step numbers are automatically assigned: first item = 0, second = 1, etc.
        private static readonly StepName[] StepOrder = {
            StepName.TransactionType,        // Automatically becomes step 0
            StepName.Category,               // Automatically becomes step 1
            StepName.Amount,                 // Automatically becomes step 2
            StepName.Description,            // Automatically becomes step 3
            StepName.Confirmation            // Automatically becomes step 4
        };

        // Step constants - derived from StepOrder array position (truly dynamic!)
        private static int StepTransactionType => Array.IndexOf(StepOrder, StepName.TransactionType);
        private static int StepCategory => Array.IndexOf(StepOrder, StepName.Category);
        private static int StepAmount => Array.IndexOf(StepOrder, StepName.Amount);
        private static int StepDescription => Array.IndexOf(StepOrder, StepName.Description);
        private static int StepConfirmation => Array.IndexOf(StepOrder, StepName.Confirmation);

        // Override base class properties
        protected override UssdMenuName MenuName => UssdMenuName.AddTransaction;
        protected override int TotalSteps => StepOrder.Length;

        public override async Task<UssdHandlerResult> HandleAsync(UssdRequest request, UssdState state) {
            try {
                // If menu changed to MainMenu (from navigation commands), return immediately
                // The MainMenuHandler will detect this and show the menu
                if (state.CurrentMenu == UssdMenuName.MainMenu.ToString()) {
                    return new UssdHandlerResult(UssdResponse.Continue(""), state);
                }

                // Ensure state is AddTransactionState
                var transactionState = EnsureAddTransactionState(state);

                // Use if-else chain instead of switch since step values are computed at runtime
                if (transactionState.CurrentStep == StepTransactionType) {
                    return await HandleTransactionType(request, transactionState);
                }

                if (transactionState.CurrentStep == StepCategory) {
                    return await HandleCategoryStep(request, transactionState);
                }

                if (transactionState.CurrentStep == StepAmount) {
                    return await HandleAmountStep(request, transactionState);
                }

                if (transactionState.CurrentStep == StepDescription) {
                    return await HandleDescriptionStep(request, transactionState);
                }

                if (transactionState.CurrentStep == StepConfirmation) {
                    return await HandleConfirmationStep(request, transactionState);
                }

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.GenericError),
                    transactionState
                );
            } catch (Exception ex) {
                logger.LogError(ex,
                    "Exception in AddTransactionHandler.HandleAsync. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}, CurrentMenu: {CurrentMenu}, CurrentStep: {CurrentStep}, Input: {Input}, TransactionType: {TransactionType}, TransactionCategory: {TransactionCategory}, TransactionAmount: {TransactionAmount}",
                    request.PhoneNumber,
                    request.SessionId,
                    state.CurrentMenu,
                    state.CurrentStep,
                    request.Input,
                    state is AddTransactionState ats ? ats.TransactionType : null,
                    state is AddTransactionState ats2 ? ats2.TransactionCategory : null,
                    state is AddTransactionState ats3 ? ats3.TransactionAmount : null);

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.GenericError),
                    state
                );
            }
        }

        /// <summary>
        /// Ensures the state is of type AddTransactionState.
        /// Converts from UssdState if needed (first entry to menu).
        /// </summary>
        private AddTransactionState EnsureAddTransactionState(UssdState state) {
            if (state is AddTransactionState existingState) {
                return existingState;
            }

            // This should only happen on first entry to AddTransaction menu
            // Convert UssdState to AddTransactionState
            return new AddTransactionState {
                PhoneNumber = state.PhoneNumber,
                SessionId = state.SessionId,
                CurrentMenu = state.CurrentMenu,
                CurrentStep = state.CurrentStep,
                CreatedAt = state.CreatedAt,
                LastUpdated = state.LastUpdated,
                // Transaction properties will be null (first entry)
                TransactionType = null,
                TransactionCategory = null,
                TransactionAmount = null,
                TransactionDescription = null
            };
        }



        // Returns the prompt text for the Transaction Type step
        private string GetTransactionTypePrompt() {
            return "Select transaction type:\n1. Income\n2. Expense";
        }

        private async Task<UssdHandlerResult> HandleTransactionType(UssdRequest request, AddTransactionState state) {
            var currentStep = StepTransactionType;

            // Handle common preprocessing (empty input, navigation)
            var preprocessResult = HandleStepPreprocessing(request, state, currentStep, GetTransactionTypePrompt());
            if (preprocessResult != null) {
                return preprocessResult;
            }

            var input = request.Input.Trim();

            if (input == "1") {
                state.TransactionType = "Income";
            } else if (input == "2") {
                state.TransactionType = "Expense";
            } else {
                return ReturnError(state, currentStep, ErrorMessages.InvalidTransactionType);
            }

            return await ProcessStepCompletion(currentStep, state);
        }

        

        // Returns the prompt text for the Category step
        private string GetCategoryPrompt(int page = 0) {
            return $"Enter category:\n{GetCategoryList(page)}";
        }

        private async Task<UssdHandlerResult> HandleCategoryStep(UssdRequest request, AddTransactionState state) {
            var currentStep = StepCategory;

            // Handle empty input - show current page
            if (string.IsNullOrWhiteSpace(request.Input)) {
                return ShowCategoryPage(state, currentStep);
            }

            var input = request.Input.Trim();

            // Handle navigation commands first (# for back, ## for main menu)
            HandleNavigation(request, state, currentStep);

            // If navigation changed the step or menu
            if (state.CurrentStep != currentStep || state.CurrentMenu != MenuName.ToString()) {
                return HandleCategoryNavigationChange(state, currentStep);
            }

            // Handle pagination commands
            var paginationResult = HandleCategoryPagination(input, state, currentStep);
            if (paginationResult != null) {
                return paginationResult;
            }

            // Handle category selection
            if (TryParseCategorySelection(input, out var category)) {
                state.TransactionCategory = category;
                state.CategoryPage = 0; // Reset pagination
                return await ProcessStepCompletion(currentStep, state);
            }

            // Invalid input
            return ShowCategoryError(state, currentStep);
        }

        // Returns the prompt text for the Amount step
        private string GetAmountPrompt() {
            return "Enter amount:";
        }

        private async Task<UssdHandlerResult> HandleAmountStep(UssdRequest request, AddTransactionState state) {
            var currentStep = StepAmount;

            // Handle common preprocessing (empty input, navigation)
            var preprocessResult = HandleStepPreprocessing(request, state, currentStep, GetAmountPrompt());
            if (preprocessResult != null) {
                return preprocessResult;
            }

            if (!double.TryParse(request.Input.Trim(), out double amount) || amount <= 0) {
                return ReturnError(state, currentStep, ErrorMessages.InvalidAmount);
            }

            state.TransactionAmount = amount;

            return await ProcessStepCompletion(currentStep, state);
        }

           // Returns the prompt text for the Description step
        private string GetDescriptionPrompt() {
            return "Enter description:";
        }

        private async Task<UssdHandlerResult> HandleDescriptionStep(UssdRequest request, AddTransactionState state) {
            var currentStep = StepDescription;

            // Handle common preprocessing (empty input, navigation)
            var preprocessResult = HandleStepPreprocessing(request, state, currentStep, GetDescriptionPrompt());
            if (preprocessResult != null) {
                return preprocessResult;
            }

            var description = request.Input.Trim();

            if (string.IsNullOrWhiteSpace(description)) {
                return ReturnError(state, currentStep, ErrorMessages.EmptyDescription);
            }

            state.TransactionDescription = description;

            return await ProcessStepCompletion(currentStep, state);
        }

        // Returns the prompt text for the Confirmation step
        private string GetConfirmationPrompt() {
            return "Transaction Confirmation:";
        }

        private async Task<UssdHandlerResult> HandleConfirmationStep(UssdRequest request, AddTransactionState state) {
            var currentStep = StepConfirmation;

            // Create the transaction first
            var transaction = await CreateTransaction(state);
            if (transaction == null) {
                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.TransactionCreationFailed),
                    state
                );
            }

            // For empty input, go directly through ProcessStepCompletion
            if (string.IsNullOrWhiteSpace(request.Input)) {
                return await ProcessStepCompletion(currentStep, state, transaction);
            }

            // For non-empty input, use standard preprocessing to handle navigation
            var prompt = $"{GetConfirmationPrompt()}\n{FormatTransactionConfirmation(transaction)}";
            var preprocessResult = HandleStepPreprocessing(request, state, currentStep, prompt);

            // Return preprocessing result if it handled navigation, otherwise complete the step
            return preprocessResult ?? await ProcessStepCompletion(currentStep, state, transaction);
        }



        /// <summary>
        /// Displays the current category page with navigation options.
        /// </summary>
        private UssdHandlerResult ShowCategoryPage(AddTransactionState state, int currentStep) {
            var navigationOptions = GetNavigationOptionsForCategory(currentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{GetCategoryPrompt(state.CategoryPage)}{navigationOptions}"),
                state
            );
        }

        /// <summary>
        /// Handles navigation changes in the category step (back or main menu).
        /// </summary>
        private UssdHandlerResult HandleCategoryNavigationChange(AddTransactionState state, int currentStep) {
            // Reset pagination when leaving this step
            state.CategoryPage = 0;

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

            // Shouldn't reach here, but return safe default
            return ShowCategoryPage(state, currentStep);
        }

        /// <summary>
        /// Handles category pagination commands (next/previous page).
        /// Returns null if input is not a pagination command.
        /// </summary>
        private UssdHandlerResult? HandleCategoryPagination(string input, AddTransactionState state, int currentStep) {
            var totalPages = GetTotalPages();

            if (input == NextPageCommand) {
                if (state.CategoryPage < totalPages - 1) {
                    state.CategoryPage++;
                }
                return ShowCategoryPage(state, currentStep);
            }

            if (input == PreviousPageCommand) {
                if (state.CategoryPage > 0) {
                    state.CategoryPage--;
                }
                return ShowCategoryPage(state, currentStep);
            }

            return null; // Not a pagination command
        }

        /// <summary>
        /// Tries to parse and validate category selection input.
        /// </summary>
        private bool TryParseCategorySelection(string input, out string category) {
            category = string.Empty;

            if (!int.TryParse(input, out int categoryIndex)) {
                return false;
            }

            if (categoryIndex < 1 || categoryIndex > TransactionConstants.Categories.Count) {
                return false;
            }

            category = TransactionConstants.Categories[categoryIndex - 1];
            return true;
        }

        /// <summary>
        /// Returns error response for invalid category selection.
        /// </summary>
        private UssdHandlerResult ShowCategoryError(AddTransactionState state, int currentStep) {
            var navOpts = GetNavigationOptionsForCategory(currentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"Invalid option. Please enter a number between 1 and {TransactionConstants.Categories.Count}:\n{GetCategoryList(state.CategoryPage)}{navOpts}"),
                state
            );
        }

        

     
        
        /// <summary>
        /// Centralized method to process step completion.
        /// This is the ONLY method that should call CompleteAddTransaction.
        /// Automatically ends session if at the final step, otherwise proceeds to next step.
        /// </summary>
        private async Task<UssdHandlerResult> ProcessStepCompletion(int currentStep, AddTransactionState state, Transaction.Transaction? transaction = null) {
            // If this is the final step, end the session
            if (IsLastStep(currentStep)) {
                // Transaction should be passed for Confirmation step
                return await CompleteAddTransaction(state, transaction);
            }

            // Otherwise, proceed to the next step
            var nextStep = GetNextStep(currentStep);
            state.CurrentStep = nextStep;

            // Check if the next step is the final step - if so, complete and END
            if (IsLastStep(nextStep)) {
                // For Confirmation step, create the transaction
                if (transaction == null) {
                    transaction = await CreateTransaction(state);
                }
                return await CompleteAddTransaction(state, transaction);
            }

            // Get the prompt for the next step dynamically by calling the appropriate method
            var nextStepPrompt = GetPromptForStep(nextStep);

            var navOptions = GetNavigationOptions(state.CurrentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{nextStepPrompt}{navOptions}"),
                state
            );
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
        /// Each step handler owns its own prompt logic.
        /// Note: For Category step, this returns page 0 by default when called from base class.
        /// The HandleCategory method manages pagination internally.
        /// </summary>
        private string GetPromptForStepName(StepName stepName) {
            return stepName switch {
                StepName.TransactionType => GetTransactionTypePrompt(),
                StepName.Category => GetCategoryPrompt(0), // Default to first page for base class calls
                StepName.Amount => GetAmountPrompt(),
                StepName.Description => GetDescriptionPrompt(),
                StepName.Confirmation => GetConfirmationPrompt(),
                _ => "Continue:"
            };
        }

        /// <summary>
        /// Creates a transaction in the database.
        /// Returns null if creation fails.
        /// </summary>
        private async Task<Transaction.Transaction?> CreateTransaction(AddTransactionState state) {
            try {
                var createTransaction = new CreateTransaction {
                    Type = state.TransactionType!,
                    Category = state.TransactionCategory!,
                    Amount = state.TransactionAmount!.Value,
                    Description = state.TransactionDescription ?? ""
                };

                return await transactionService.createTransaction(createTransaction);
            } catch (Exception ex) {
                logger.LogError(ex, "Error creating transaction in AddTransactionHandler.CreateTransaction");
                return null;
            }
        }

        /// <summary>
        /// Formats the transaction confirmation for display.
        /// </summary>
        private string FormatTransactionConfirmation(Transaction.Transaction transaction) {
            return $"{VisualDivider}\n" +
                   $"Type: {transaction.Type}\n" +
                   $"Category: {transaction.Category}\n" +
                   $"Amount: ${transaction.Amount:F2}\n" +
                   $"Description: {transaction.Description}";
        }

        /// <summary>
        /// Completes the add transaction flow and ends the session.
        /// ONLY called by ProcessStepCompletion.
        /// </summary>
        private async Task<UssdHandlerResult> CompleteAddTransaction(AddTransactionState state, Transaction.Transaction? transaction) {
            try {
                // Transaction should be passed in, but handle null case defensively
                if (transaction == null) {
                    transaction = await CreateTransaction(state);
                }

                if (transaction == null) {
                    return new UssdHandlerResult(
                        UssdResponse.End(ErrorMessages.TransactionCreationFailed),
                        state
                    );
                }

                // Clear transaction data from state
                ClearTransactionData(state);
                state.CurrentStep = 0;

                return new UssdHandlerResult(
                    UssdResponse.End($"Transaction created successfully!\n\n{FormatTransactionConfirmation(transaction)}\n\nThank you for using Expenses App!"),
                    state
                );
            } catch (Exception ex) {
                logger.LogError(ex,
                    "Exception in AddTransactionHandler.CompleteAddTransaction. PhoneNumber: {PhoneNumber}, SessionId: {SessionId}, TransactionType: {TransactionType}, TransactionCategory: {TransactionCategory}, TransactionAmount: {TransactionAmount}, TransactionDescription: {TransactionDescription}",
                    state.PhoneNumber,
                    state.SessionId,
                    state.TransactionType,
                    state.TransactionCategory,
                    state.TransactionAmount,
                    state.TransactionDescription);

                return new UssdHandlerResult(
                    UssdResponse.End(ErrorMessages.TransactionCreationFailed),
                    state
                );
            }
        }

        private string GetCategoryList(int page = 0) {
            var categoryList = BuildCategoryListForPage(page);
            var paginationIndicators = GetPaginationIndicators(page);

            if (string.IsNullOrEmpty(paginationIndicators)) {
                return categoryList.TrimEnd();
            }

            return $"{categoryList}\n{paginationIndicators}".TrimEnd();
        }

        /// <summary>
        /// Builds the numbered category list for a specific page.
        /// </summary>
        private string BuildCategoryListForPage(int page) {
            var startIndex = page * CategoriesPerPage;
            var endIndex = Math.Min(startIndex + CategoriesPerPage, TransactionConstants.Categories.Count);

            var list = "";
            for (int i = startIndex; i < endIndex; i++) {
                var displayNumber = i + 1; // Continuous numbering: 1-10 across all pages
                list += $"{displayNumber}. {TransactionConstants.Categories[i]}\n";
            }

            return list;
        }

        /// <summary>
        /// Gets pagination indicators (next/previous page) if multiple pages exist.
        /// </summary>
        private string GetPaginationIndicators(int page) {
            var totalPages = GetTotalPages();
            if (totalPages <= 1) {
                return "";
            }

            var indicators = "";
            if (page < totalPages - 1) {
                indicators += $"{NextPageCommand} Next page\n";
            }
            if (page > 0) {
                indicators += $"{PreviousPageCommand} Previous page\n";
            }

            return indicators.TrimEnd();
        }

        private int GetTotalPages() {
            return (int)Math.Ceiling((double)TransactionConstants.Categories.Count / CategoriesPerPage);
        }

        /// <summary>
        /// Gets navigation options specifically for the category step.
        /// Hides the ## (Main Menu) option since pagination uses * and **.
        /// </summary>
        private string GetNavigationOptionsForCategory(int currentStep) {
            if (currentStep <= FirstStep) {
                return ""; // No navigation at first step
            }

            // Only show Back option, hide Main Menu (##) to avoid confusion with pagination
            return $"\n\n{BackCommand} Back";
        }

        /// <summary>
        /// Clears data for the current step when navigating back.
        /// Uses the dynamic step order to determine which data to clear.
        /// </summary>
        protected override void ClearCurrentStepData(UssdState state, int step) {
            // Cast to AddTransactionState to access transaction properties
            if (state is not AddTransactionState transactionState) {
                return;
            }

            // Find which step name corresponds to this step number
            if (step < 0 || step >= StepOrder.Length) {
                return; // Invalid step
            }

            var stepName = StepOrder[step];

            // Clear data based on the step name
            switch (stepName) {
                case StepName.TransactionType:
                    transactionState.TransactionType = null;
                    break;
                case StepName.Category:
                    transactionState.TransactionCategory = null;
                    transactionState.CategoryPage = 0; // Reset pagination
                    break;
                case StepName.Amount:
                    transactionState.TransactionAmount = null;
                    break;
                case StepName.Description:
                    transactionState.TransactionDescription = null;
                    break;
                case StepName.Confirmation:
                    // No data to clear for confirmation step (display only)
                    break;
            }
        }

        /// <summary>
        /// Clears all transaction-related data from the state.
        /// </summary>
        protected override void ClearTransactionData(UssdState state) {
            // Cast to AddTransactionState to access transaction properties
            if (state is not AddTransactionState transactionState) {
                return;
            }

            transactionState.TransactionType = null;
            transactionState.TransactionCategory = null;
            transactionState.TransactionAmount = null;
            transactionState.TransactionDescription = null;
            transactionState.CategoryPage = 0; // Reset pagination
        }

        /// <summary>
        /// Returns an error response with navigation options.
        /// </summary>
        private UssdHandlerResult ReturnError(AddTransactionState state, int currentStep, string errorMessage) {
            var navigationOptions = GetNavigationOptions(currentStep);
            return new UssdHandlerResult(
                UssdResponse.Continue($"{errorMessage}{navigationOptions}"),
                state
            );
        }
    }


}
