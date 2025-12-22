using Expenses.API.Domain.Transaction;
using Expenses.API.Domain.Transaction.Dto;
using Expenses.API.Domain.Transaction.Services;
using Expenses.API.Domain.Ussd.Handlers;
using Expenses.API.Domain.Ussd.Models;
using Expenses.API.Domain.Ussd.Services;
using Microsoft.AspNetCore.Mvc;

namespace Expenses.API.Controllers {
    [Route("api/[controller]")]
    [ApiController]
    public class UssdController(UssdStateService stateService, MainMenuHandler mainMenuHandler, TransactionService transactionService) : ControllerBase {

        [HttpPost]
        [ProducesResponseType(typeof(UssdResponse), 200)]
        public async Task<IActionResult> ProcessUssdRequest([FromBody] UssdRequest request) {
            if (string.IsNullOrWhiteSpace(request.PhoneNumber)) {
                return BadRequest(new { error = "Phone number is required" });
            }

            // Get or create state
            var state = await stateService.GetStateAsync(request.PhoneNumber);
            
            if (state == null) {
                // New session - initialize state
                state = new UssdState {
                    PhoneNumber = request.PhoneNumber,
                    SessionId = request.SessionId,
                    CurrentMenu = "MainMenu",
                    CurrentStep = 0
                };
            } else {
                // Update session ID if provided
                if (!string.IsNullOrWhiteSpace(request.SessionId)) {
                    state.SessionId = request.SessionId;
                }
            }

            // Route and handle request through MainMenuHandler
            var result = await mainMenuHandler.HandleAsync(request, state);
            
            // Get the updated state from the result
            var updatedState = result.UpdatedState;
            var response = result.Response;

            // Save updated state (unless session ended)
            if (response.Type == "CON") {
                await stateService.SaveStateAsync(request.PhoneNumber, updatedState);
            } else {
                // Clear state when session ends
                await stateService.ClearStateAsync(request.PhoneNumber);
            }

            return Ok(response);
        }

        [HttpGet("balance")]
        [ProducesResponseType(typeof(BalanceResult), 200)]
        public async Task<IActionResult> calculateBalance([FromQuery] TransactionFilters? filters = null) {
            var balanceResult = await transactionService.calculateBalance(filters);
            return Ok(balanceResult);
        }
    }
}
