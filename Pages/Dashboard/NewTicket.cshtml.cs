using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MindtecFinance.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class NewTicketModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public NewTicketModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Company> Companies { get; set; } = new();
        public List<string> AppUsers { get; set; } = new();

        [BindProperty]
        public Ticket InputTicket { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCompaniesAsync();
            await LoadActiveUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            Company? company = null;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                string compQuery = "SELECT CompanyId, CompanyName, CompanyPhone, CompanyEmail, CompanyAddress, CompanyWebsite FROM CompanyMaster WHERE CompanyId = @Id";
                using (var cmd = new SqlCommand(compQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            company = new Company
                            {
                                CompanyId = reader.GetInt32(0),
                                CompanyName = reader.GetString(1),
                                CompanyPhone = reader.IsDBNull(2) ? null : reader.GetString(2),
                                CompanyEmail = reader.IsDBNull(3) ? null : reader.GetString(3),
                                CompanyAddress = reader.IsDBNull(4) ? null : reader.GetString(4),
                                CompanyWebsite = reader.IsDBNull(5) ? null : reader.GetString(5)
                            };
                        }
                    }
                }

                if (company != null)
                {
                    string contQuery = "SELECT ContactID, Name, Title, Email, Phone, PrimaryContact FROM ContactDetail WHERE CompanyId = @Id ORDER BY PrimaryContact DESC, Name ASC";
                    using (var cmd = new SqlCommand(contQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                company.Contacts.Add(new Contact
                                {
                                    ContactID = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    PrimaryContact = reader.GetBoolean(5)
                                });
                            }
                        }
                    }
                    return new JsonResult(company);
                }
            }

            return new NotFoundResult();
        }

        public async Task<IActionResult> OnGetTicketsAsync(int companyId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            var tickets = new List<Ticket>();

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                string query = @"SELECT T.TicketID, T.CompanyID, T.RaiseDate, T.Priority, T.Description, T.Summary, T.RaisedBy, C.CompanyName, T.Status, T.AssignedTo 
                                 FROM Tickets T 
                                 INNER JOIN CompanyMaster C ON T.CompanyID = C.CompanyId 
                                 WHERE T.CompanyID = @CompId
                                 ORDER BY T.RaiseDate DESC";

                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CompId", companyId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            tickets.Add(new Ticket
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
            return new JsonResult(tickets);
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (InputTicket.CompanyID == 0 || string.IsNullOrWhiteSpace(InputTicket.Summary) || string.IsNullOrWhiteSpace(InputTicket.AssignedTo))
            {
                ErrorMessage = "Please select a client, provide a summary, and assign to a user.";
                await LoadCompaniesAsync();
                await LoadActiveUsersAsync();
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"INSERT INTO Tickets (CompanyID, RaiseDate, Priority, Description, Summary, RaisedBy, AssignedTo, Status) 
                                 VALUES (@CompId, @Date, @Priority, @Desc, @Summ, @User, @AssignedTo, @Status)";
                
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CompId", InputTicket.CompanyID);
                    cmd.Parameters.AddWithValue("@Date", DateTime.Now);
                    cmd.Parameters.AddWithValue("@Priority", (object?)InputTicket.Priority ?? "Normal");
                    cmd.Parameters.AddWithValue("@Desc", (object?)InputTicket.Description ?? "");
                    cmd.Parameters.AddWithValue("@Summ", InputTicket.Summary);
                    cmd.Parameters.AddWithValue("@User", User.Identity?.Name ?? "Admin");
                    cmd.Parameters.AddWithValue("@AssignedTo", InputTicket.AssignedTo);
                    cmd.Parameters.AddWithValue("@Status", (object?)InputTicket.Status ?? "Open");
                    
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            SuccessMessage = "Ticket added successfully!";
            await LoadCompaniesAsync();
            await LoadActiveUsersAsync();
            return Page();
        }

        private async Task LoadCompaniesAsync()
        {
            Companies.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = "SELECT CompanyId, CompanyName FROM CompanyMaster ORDER BY CompanyName";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Companies.Add(new Company
                        {
                            CompanyId = reader.GetInt32(0),
                            CompanyName = reader.GetString(1)
                        });
                    }
                }
            }
        }
 
        private async Task LoadActiveUsersAsync()
        {
            AppUsers.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = "SELECT UserName FROM AppUsers WHERE Status = 'active' ORDER BY UserName";
                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        AppUsers.Add(reader.GetString(0));
                    }
                }
            }
        }
    }
}
