using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using FinTechLiteAPI.Data;
using FinTechLiteAPI.Models.DTOs;
using FinTechLiteAPI.Services;

namespace FinTechLiteAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TransactionsController : ControllerBase
    {
        private readonly ITransactionService _transactionService;
        private readonly AppDbContext _context;
        private readonly ILogger<TransactionsController> _logger;

        public TransactionsController(
            ITransactionService transactionService,
            AppDbContext context,
            ILogger<TransactionsController> logger)
        {
            _transactionService = transactionService;
            _context = context;
            _logger = logger;
        }

        [HttpPost("transfer")]
        public async Task<IActionResult> Transfer([FromBody] TransferRequest request)
        {
            var userId = GetUserId();

            // Get sender's account
            var fromAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (fromAccount == null)
                return NotFound(new { error = "Your account not found" });

            // Verify recipient exists
            var toAccount = await _context.Accounts
                .FirstOrDefaultAsync(a => a.AccountId == request.ToAccountId);

            if (toAccount == null)
                return NotFound(new { error = "Recipient account not found" });

            // Generate idempotency key if not provided
            var idempotencyKey = request.IdempotencyKey
                ?? $"{userId}:{request.ToAccountId}:{request.Amount}:{DateTime.UtcNow.Ticks}";

            _logger.LogInformation(
                "Transfer request: From {FromAccountId} to {ToAccountId} amount {Amount}",
                fromAccount.AccountId, request.ToAccountId, request.Amount);

            var result = await _transactionService.TransferMoneyAsync(
                fromAccount.AccountId,
                request.ToAccountId,
                request.Amount//,
                //idempotencyKey
            );

            if (!result.Success)
                return BadRequest(new { error = result.Error });

            return Ok(new
            {
                success = true,
                transactionId = result.TransactionId,
                message = "Transfer completed successfully"
            });
        }

        [HttpGet("{transactionId}")]
        public async Task<IActionResult> GetTransaction(Guid transactionId)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null)
                return NotFound(new { error = "Account not found" });

            var transaction = await _context.Transactions
                .Where(t => t.TransactionId == transactionId &&
                           (t.FromAccountId == account.AccountId ||
                            t.ToAccountId == account.AccountId))
                .Select(t => new
                {
                    transactionId = t.TransactionId,
                    fromAccountId = t.FromAccountId,
                    toAccountId = t.ToAccountId,
                    amount = t.Amount,
                    status = t.Status.ToString(),
                    createdAt = t.CreatedAt,
                    completedAt = t.CompletedAt,
                    failureReason = t.FailureReason
                })
                .FirstOrDefaultAsync();

            if (transaction == null)
                return NotFound(new { error = "Transaction not found" });

            return Ok(transaction);
        }

        [HttpGet("recent")]
        public async Task<IActionResult> GetRecentTransactions([FromQuery] int count = 10)
        {
            var userId = GetUserId();
            var account = await _context.Accounts
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (account == null)
                return NotFound(new { error = "Account not found" });

            var transactions = await _context.Transactions
                .Where(t => t.FromAccountId == account.AccountId ||
                           t.ToAccountId == account.AccountId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(count)
                .Select(t => new
                {
                    transactionId = t.TransactionId,
                    type = t.FromAccountId == account.AccountId ? "debit" : "credit",
                    amount = t.Amount,
                    status = t.Status.ToString(),
                    otherAccountId = t.FromAccountId == account.AccountId
                        ? t.ToAccountId
                        : t.FromAccountId,
                    createdAt = t.CreatedAt
                })
                .ToListAsync();

            return Ok(transactions);
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