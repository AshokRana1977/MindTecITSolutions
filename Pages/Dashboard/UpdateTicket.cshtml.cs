using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MindtecFinance.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class UpdateTicketModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public UpdateTicketModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty(SupportsGet = true)]
        public string Filter { get; set; } = "Mine";

        [BindProperty(SupportsGet = true)]
        public int? SearchId { get; set; }

        public List<Ticket> Tickets { get; set; } = new();

        public async Task<IActionResult> OnGetAsync()
        {
            string userName = User.Identity?.Name ?? "";
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                string whereClause = " WHERE 1=1";
                var parameters = new List<SqlParameter>();

                if (SearchId.HasValue)
                {
                    whereClause += " AND T.TicketID = @SearchId";
                    parameters.Add(new SqlParameter("@SearchId", SearchId.Value));
                }

                if (Filter == "Mine")
                {
                    whereClause += " AND T.AssignedTo = @UserName";
                    parameters.Add(new SqlParameter("@UserName", userName));
                }

                // Join with CompanyMaster to get company names
                string query = @$"SELECT T.TicketID, T.CompanyID, T.RaiseDate, T.Priority, T.Description, T.Summary, T.RaisedBy, C.CompanyName, T.Status, T.AssignedTo 
                                 FROM Tickets T 
                                 INNER JOIN CompanyMaster C ON T.CompanyID = C.CompanyId 
                                 {whereClause}
                                 ORDER BY T.RaiseDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddRange(parameters.ToArray());
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Tickets.Add(new Ticket
                            {
                                TicketID = reader.GetInt32(0),
                                CompanyID = reader.GetInt32(1),
                                RaiseDate = reader.GetDateTime(2),
                                Priority = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Summary = reader.IsDBNull(5) ? null : reader.GetString(5),
                                RaisedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
                                CompanyName = reader.GetString(7),
                                Status = reader.IsDBNull(8) ? "Open" : reader.GetString(8),
                                AssignedTo = reader.IsDBNull(9) ? "Unassigned" : reader.GetString(9)
                            });
                        }
                    }
                }
            }
            return Page();
        }
    }
}
