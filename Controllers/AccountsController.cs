using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FinTechLiteAPI.Data;

namespace FinTechLiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AccountsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AccountsController> _logger;

        public AccountsController(AppDbContext context, ILogger<AccountsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("balance")]
        public async Task<IActionResult> GetBalance()
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null)
                return NotFound(new { error = "Account not found" });

            return Ok(new
            {
                accountId = account.AccountId,
                balance = account.Balance,
                lastUpdated = account.CreatedAt
            });
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetTransactionHistory(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null)
                return NotFound(new { error = "Account not found" });

            var query = _context.Transactions
                .Where(t => t.FromAccountId == account.AccountId ||
                           t.ToAccountId == account.AccountId);

            var totalCount = await query.CountAsync();

            var transactions = await query
                .OrderByDescending(t => t.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(t => new
                {
                    transactionId = t.TransactionId,
                    type = t.FromAccountId == account.AccountId ? "debit" : "credit",
                    amount = t.Amount,
                    status = t.Status.ToString(),
                    fromAccountId = t.FromAccountId,
                    toAccountId = t.ToAccountId,
                    createdAt = t.CreatedAt,
                    completedAt = t.CompletedAt,
                    failureReason = t.FailureReason
                })
                .ToListAsync();

            return Ok(new
            {
                transactions,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        [HttpGet("ledger")]
        public async Task<IActionResult> GetLedger(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 50)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null)
                return NotFound(new { error = "Account not found" });

            var query = _context.LedgerEntries
                .Where(e => e.AccountId == account.AccountId);

            var totalCount = await query.CountAsync();

            var entries = await query
                .OrderByDescending(e => e.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new
                {
                    entryId = e.EntryId,
                    transactionId = e.TransactionId,
                    debitAmount = e.DebitAmount,
                    creditAmount = e.CreditAmount,
                    balanceAfter = e.BalanceAfter,
                    createdAt = e.CreatedAt
                })
                .ToListAsync();

            return Ok(new
            {
                entries,
                page,
                pageSize,
                totalCount,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
            });
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
                throw new UnauthorizedAccessException("User ID not found in token");

            return Guid.Parse(userIdClaim);
        }
    }
}