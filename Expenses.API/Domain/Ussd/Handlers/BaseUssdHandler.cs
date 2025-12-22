using Expenses.API.Domain.Ussd.Models;

namespace Expenses.API.Domain.Ussd.Handlers {
    /// <summary>
    /// Abstract base class for USSD handlers with multi-step flows.
    /// Provides common navigation functionality and step management.
    /// </summary>
    public abstract class BaseUssdHandler : IUssdMenuHandler {
        protected const string BackCommand = "#";
        protected const string MainMenuCommand = "##";

        /// <summary>
        /// The menu name that this handler manages.
        /// Must be implemented by derived classes.
        /// </summary>
        protected abstract UssdMenuName MenuName { get; }

        /// <summary>
        /// The total number of steps in this handler's flow.
        /// Override this property to specify the number of steps.
        /// </summary>
        protected abstract int TotalSteps { get; }

        /// <summary>
        /// The first step index. Default is 0.
        /// Override if your flow starts at a different index.
        /// </summary>
        protected virtual int FirstStep => 0;

        /// <summary>
        /// Main handler method that must be implemented by derived classes.
        /// </summary>
        public abstract Task<UssdHandlerResult> HandleAsync(UssdRequest request, UssdState state);

        /// <summary>
        /// Gets the next step in the flow.
        /// </summary>
        protected int GetNextStep(int currentStep) {
            if (currentStep >= 0 && currentStep < TotalSteps - 1) {
                return currentStep + 1;
            }
            return currentStep;
        }

        /// <summary>
        /// Gets the previous step in the flow.
        /// </summary>
        protected int GetPreviousStep(int currentStep) {
            if (currentStep > FirstStep) {
                return currentStep - 1;
            }
            return FirstStep;
        }

        /// <summary>
        /// Checks if the current step is the first step.
        /// </summary>
        protected bool IsFirstStep(int step) {
            return step == FirstStep;
        }

        /// <summary>
        /// Checks if the current step is the last step.
        /// </summary>
        protected bool IsLastStep(int step) {
            return step == TotalSteps - 1;
        }

        /// <summary>
        /// Gets the navigation options string based on the current step.
        /// Shows both Back and Main Menu if step > FirstStep, only Main Menu if step is FirstStep.
        /// </summary>
        protected string GetNavigationOptions(int currentStep) {
            if (currentStep <= FirstStep) {
                // At first step or step 0, only show Main Menu (no back option)
                return $"\n\n{MainMenuCommand} Main Menu";
            }
            
            // At subsequent steps, show both Back and Main Menu
            return $"\n\n{BackCommand} Back\n{MainMenuCommand} Main Menu";
        }

        /// <summary>
        /// Handles navigation commands (# for back, ## for main menu).
        /// Returns null if not a navigation command or if navigation was handled internally.
        /// When returning to main menu, sets state and returns null so caller can re-route.
        /// </summary>
        protected UssdHandlerResult? HandleNavigation(UssdRequest request, UssdState state, int currentStep) {
            var input = request.Input.Trim();

            if (input == MainMenuCommand) {
                // Return to main menu - set state and return null to trigger re-routing
                state.CurrentMenu = UssdMenuName.MainMenu.ToString();
                state.CurrentStep = 0;
                ClearTransactionData(state);
                return null; // Caller should re-route to MainMenu
            }

            if (input == BackCommand) {
                if (IsFirstStep(currentStep)) {
                    // At first step, back goes to main menu
                    state.CurrentMenu = UssdMenuName.MainMenu.ToString();
                    state.CurrentStep = 0;
                    ClearTransactionData(state);
                    return null; // Caller should re-route to MainMenu
                } else {
                    // Navigate back to previous step
                    state.CurrentStep = GetPreviousStep(currentStep);
                    ClearCurrentStepData(state, currentStep);
                    return null; // Caller should re-prompt at new step
                }
            }

            return null; // Not a navigation command
        }

        /// <summary>
        /// Handles common pre-processing for a step: empty input check and navigation.
        /// Returns a result if the step should return immediately (showing prompt or navigating).
        /// Returns null if the step should continue with its specific validation logic.
        /// </summary>
        protected UssdHandlerResult? HandleStepPreprocessing(
            UssdRequest request, 
            UssdState state, 
            int currentStep,
            string prompt) {
            
            // If no input provided (session restored or re-entry), show the prompt
            if (string.IsNullOrWhiteSpace(request.Input)) {
                var navigationOptions = GetNavigationOptions(currentStep);
                return new UssdHandlerResult(
                    UssdResponse.Continue($"{prompt}{navigationOptions}"),
                    state
                );
            }
            
            // Check for navigation commands (this may modify state)
            HandleNavigation(request, state, currentStep);
            
            // If navigation changed the step, show the prompt for the new step
            if (state.CurrentStep != currentStep) {
                // Step changed (user went back) - show prompt for the new step
                var nextPrompt = GetPromptForStep(state.CurrentStep);
                var navOptions = GetNavigationOptions(state.CurrentStep);
                return new UssdHandlerResult(
                    UssdResponse.Continue($"{nextPrompt}{navOptions}"),
                    state
                );
            }
            
            // Check if menu changed (user navigated away)
            if (state.CurrentMenu != MenuName.ToString()) {
                // Menu changed (user went to main menu) - return empty response
                // MainMenuHandler will detect this and show the main menu
                return new UssdHandlerResult(UssdResponse.Continue(""), state);
            }

            // Continue with step-specific logic
            return null;
        }

        /// <summary>
        /// Clears data for the current step when navigating back.
        /// Must be implemented by derived classes to clear step-specific data.
        /// </summary>
        protected abstract void ClearCurrentStepData(UssdState state, int step);

        /// <summary>
        /// Clears all handler-related data from the state.
        /// Must be implemented by derived classes to clear handler-specific data.
        /// </summary>
        protected abstract void ClearTransactionData(UssdState state);

        /// <summary>
        /// Gets the prompt text for a given step.
        /// Performs bounds checking and delegates to GetPromptForStepInternal for actual prompt retrieval.
        /// </summary>
        protected string GetPromptForStep(int step) {
            if (step < 0 || step >= TotalSteps) {
                return "Continue:";
            }
            
            return GetPromptForStepInternal(step);
        }

        /// <summary>
        /// Gets the prompt text for a valid step (within bounds).
        /// Must be implemented by derived classes to provide step-specific prompts.
        /// </summary>
        protected abstract string GetPromptForStepInternal(int step);
    }
}
