using Expenses.API.Domain.Transaction;
using Expenses.API.Domain.Transaction.Dto;
using Expenses.API.Domain.Transaction.Services;
using Expenses.API.framework.Data;
using Expenses.API.Shared.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace Expenses.API.Controllers {

    [Route("api/[controller]")]
    [ApiController]
    public class TransactionController(TransactionService service) : ControllerBase {

        [HttpPost]
        [ProducesResponseType(typeof(Transaction), 200)]
        public async Task<IActionResult> createTransaction([FromBody] CreateTransaction payload) {
            var transaction = await service.createTransaction(payload);
            return CreatedAtAction(
                nameof(getTransactionById),
                new { id = transaction.Id },
                transaction
                );
        }

        [HttpGet]
        [ProducesResponseType(typeof(PagedResult<Transaction>), 200)]
        public async Task<IActionResult> getAllTransactions([FromQuery] TransactionFilters? filters = null) {
            // Set default pagination if not provided
            filters ??= new TransactionFilters { Page = 1, PageSize = 10 };
            
            // Validate and normalize pagination parameters (best practice: set reasonable defaults and limits)
            if (filters.Page < 1) filters.Page = 1;
            if (filters.PageSize < 1) filters.PageSize = 10;
            if (filters.PageSize > 100) filters.PageSize = 100; // Max page size limit to prevent performance issues
            
            var (data, totalCount) = await service.getAllTransactions(filters);
            
            var result = new PagedResult<Transaction>(
                data: data,
                totalCount: totalCount,
                pageNumber: filters.Page,
                pageSize: filters.PageSize
            );
            
            return Ok(result);
        }

        [HttpGet("categories")]
        [ProducesResponseType(typeof(List<string>), 200)]
        public IActionResult getCategories() {
            return Ok(TransactionConstants.Categories);
        }

        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Transaction), 200)]
        public async Task<IActionResult> getTransactionById(string id) {
            var transaction = await service.getTransactionById(id);
            if (transaction == null) return NotFound("Transaction not found");
            return Ok(transaction);
        }

        [HttpGet("get-by-id")]
        [ProducesResponseType(typeof(Transaction), 200)]
        public async Task<IActionResult> getTransactionByIdV2([FromQuery] TransactionFilters filters) {
            //var id = HttpContext.Request.Query["id"].ToString();
            //var transaction = context.Transactions.FirstOrDefault(t => t.Id == id);
            var transaction = await service.getTransactionById(filters);
            if (transaction == null) return NotFound("Transaction not found");
            return Ok(transaction);
        }

        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Transaction), 200)]
        public async Task<IActionResult> updateTransaction(string id, [FromBody] UpdateTransaction payload) {
            var transaction = await service.updateTransaction(id, payload);
            if (transaction == null) return NotFound("Transaction not found");


            return Ok(transaction);
        }

        [HttpDelete("Delete/{id}")]
        [ProducesResponseType(typeof(void), 204)]
        public async Task<IActionResult> deleteTransaction(string id) {
            var transaction = await service.deleteTransaction(id);
            if(transaction == null) return NotFound("Transaction not found");
            return NoContent();
        }
    }
}
