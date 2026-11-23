using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using MindtecFinance.Models;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class FinancialsModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public FinancialsModel(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection")!;
        }

        [BindProperty]
        public FinancialTransaction Transaction { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string DateFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? StartDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public DateTime? EndDate { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SharedFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortColumn { get; set; }

        [BindProperty(SupportsGet = true)]
        public string SortDirection { get; set; }

        public List<FinancialTransaction> RecentTransactions { get; set; } = new();
        public List<AppUser> ActiveUsers { get; set; } = new();
        public decimal MonthlyIncome { get; set; }
        public decimal MonthlyExpense { get; set; }

        public async Task OnGetAsync()
        {
            RecentTransactions = new List<FinancialTransaction>();
            ActiveUsers = new List<AppUser>();
            MonthlyIncome = 0;
            MonthlyExpense = 0;

            if (string.IsNullOrEmpty(DateFilter)) DateFilter = "CurrentMonth";
            if (string.IsNullOrEmpty(SharedFilter)) SharedFilter = "All";

            if (string.IsNullOrEmpty(SortColumn)) SortColumn = "Date";
            if (string.IsNullOrEmpty(SortDirection)) SortDirection = "DESC";

            string orderByClause = "ORDER BY t.Date DESC";
            var validColumns = new[] { "Date", "Category", "Description", "Type", "Shared", "Amount", "OwnerName", "PaidByName", "SharedContributionDone" };
            if (validColumns.Contains(SortColumn, StringComparer.OrdinalIgnoreCase))
            {
                string direction = SortDirection == "ASC" ? "ASC" : "DESC";
                string sortCol = SortColumn;
                if (SortColumn == "OwnerName") sortCol = "u1.UserName";
                else if (SortColumn == "PaidByName") sortCol = "u2.UserName";
                else sortCol = "t." + SortColumn;
                orderByClause = $"ORDER BY {sortCol} {direction}";
            }

            string whereClause = " WHERE 1=1 ";
            if (DateFilter == "CurrentMonth")
            {
                whereClause += " AND MONTH(t.Date) = @Month AND YEAR(t.Date) = @Year ";
            }
            else if (DateFilter == "Custom")
            {
                if (StartDate.HasValue) whereClause += " AND t.Date >= @StartDate ";
                if (EndDate.HasValue) whereClause += " AND t.Date <= @EndDate ";
            }

            if (SharedFilter == "Shared")
            {
                whereClause += " AND t.Shared = 1 ";
            }
            else if (SharedFilter == "Individual")
            {
                whereClause += " AND t.Shared = 0 ";
            }

            Action<SqlCommand> addParams = (cmd) => 
            {
                if (DateFilter == "CurrentMonth")
                {
                    cmd.Parameters.AddWithValue("@Month", DateTime.Now.Month);
                    cmd.Parameters.AddWithValue("@Year", DateTime.Now.Year);
                }
                else if (DateFilter == "Custom")
                {
                    if (StartDate.HasValue) cmd.Parameters.AddWithValue("@StartDate", StartDate.Value);
                    if (EndDate.HasValue) cmd.Parameters.AddWithValue("@EndDate", EndDate.Value);
                }
            };

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                // Fetch active users
                using (SqlCommand cmd = new SqlCommand("SELECT Id, UserName FROM AppUsers WHERE Status = 'active' AND UserName NOT IN ('SA', 'Admin')", connection))
                using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        ActiveUsers.Add(new AppUser { Id = reader.GetInt32(0), UserName = reader.GetString(1) });
                    }
                }

                // Fetch recent transactions
                string topTransactionsQuery = $@"
                    SELECT t.Id, t.Date, t.Type, t.Category, t.Amount, t.Description, t.Shared,
                           t.OwnerId, t.PaidById, t.SharedContributionDone,
                           u1.UserName AS OwnerName, u2.UserName AS PaidByName
                    FROM Transactions t
                    LEFT JOIN AppUsers u1 ON t.OwnerId = u1.Id
                    LEFT JOIN AppUsers u2 ON t.PaidById = u2.Id
                    {whereClause} {orderByClause}";

                using (SqlCommand cmd = new SqlCommand(topTransactionsQuery, connection))
                {
                    addParams(cmd);
                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            RecentTransactions.Add(new FinancialTransaction
                            {
                                Id = reader.GetInt32(0),
                                Date = reader.GetDateTime(1),
                                Type = reader.GetString(2),
                                Category = reader.GetString(3),
                                Amount = reader.GetDecimal(4),
                                Description = reader.IsDBNull(5) ? string.Empty : reader.GetString(5),
                                Shared = reader.IsDBNull(6) ? false : reader.GetBoolean(6),
                                OwnerId = reader.IsDBNull(7) ? (int?)null : reader.GetInt32(7),
                                PaidById = reader.IsDBNull(8) ? (int?)null : reader.GetInt32(8),
                                SharedContributionDone = reader.IsDBNull(9) ? false : reader.GetBoolean(9),
                                OwnerName = reader.IsDBNull(10) ? string.Empty : reader.GetString(10),
                                PaidByName = reader.IsDBNull(11) ? string.Empty : reader.GetString(11)
                            });
                        }
                    }
                }

                // Calculate Monthly Income
                string incomeQuery = $"SELECT SUM(t.Amount) FROM Transactions t {whereClause} AND t.Type = 'Income'";
                using (SqlCommand cmd = new SqlCommand(incomeQuery, connection))
                {
                    addParams(cmd);
                    var result = await cmd.ExecuteScalarAsync();
                    MonthlyIncome = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }

                // Calculate Monthly Expense
                string expenseQuery = $"SELECT SUM(t.Amount) FROM Transactions t {whereClause} AND t.Type = 'Expense'";
                using (SqlCommand cmd = new SqlCommand(expenseQuery, connection))
                {
                    addParams(cmd);
                    var result = await cmd.ExecuteScalarAsync();
                    MonthlyExpense = result != DBNull.Value ? Convert.ToDecimal(result) : 0;
                }
            }
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "INSERT INTO Transactions (Date, Type, Category, Amount, Description, Shared, OwnerId, PaidById, SharedContributionDone) VALUES (@Date, @Type, @Category, @Amount, @Description, @Shared, @OwnerId, @PaidById, @SharedContributionDone)";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Date", Transaction.Date);
                    cmd.Parameters.AddWithValue("@Type", Transaction.Type);
                    cmd.Parameters.AddWithValue("@Category", Transaction.Category);
                    cmd.Parameters.AddWithValue("@Amount", Transaction.Amount);
                    cmd.Parameters.AddWithValue("@Description", Transaction.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Shared", Transaction.Shared);
                    cmd.Parameters.AddWithValue("@OwnerId", Transaction.OwnerId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaidById", Transaction.PaidById ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SharedContributionDone", Transaction.SharedContributionDone);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage(new 
            { 
                DateFilter, 
                StartDate = StartDate?.ToString("yyyy-MM-dd"), 
                EndDate = EndDate?.ToString("yyyy-MM-dd"), 
                SharedFilter, 
                SortColumn, 
                SortDirection 
            });
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                string query = "UPDATE Transactions SET Date=@Date, Type=@Type, Category=@Category, Amount=@Amount, Description=@Description, Shared=@Shared, OwnerId=@OwnerId, PaidById=@PaidById, SharedContributionDone=@SharedContributionDone WHERE Id=@Id";
                using (SqlCommand cmd = new SqlCommand(query, connection))
                {
                    cmd.Parameters.AddWithValue("@Id", Transaction.Id);
                    cmd.Parameters.AddWithValue("@Date", Transaction.Date);
                    cmd.Parameters.AddWithValue("@Type", Transaction.Type);
                    cmd.Parameters.AddWithValue("@Category", Transaction.Category);
                    cmd.Parameters.AddWithValue("@Amount", Transaction.Amount);
                    cmd.Parameters.AddWithValue("@Description", Transaction.Description ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@Shared", Transaction.Shared);
                    cmd.Parameters.AddWithValue("@OwnerId", Transaction.OwnerId ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@PaidById", Transaction.PaidById ?? (object)DBNull.Value);
                    cmd.Parameters.AddWithValue("@SharedContributionDone", Transaction.SharedContributionDone);
                    await cmd.ExecuteNonQueryAsync();
                }
            }
            return RedirectToPage(new 
            { 
                DateFilter, 
                StartDate = StartDate?.ToString("yyyy-MM-dd"), 
                EndDate = EndDate?.ToString("yyyy-MM-dd"), 
                SharedFilter, 
                SortColumn, 
                SortDirection 
            });
        }
    }
}
