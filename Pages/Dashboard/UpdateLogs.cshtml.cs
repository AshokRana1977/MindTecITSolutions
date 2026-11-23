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
    public class UpdateLogsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public UpdateLogsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Ticket CurrentTicket { get; set; } = new();
        public List<TicketLog> Logs { get; set; } = new();
        public List<string> AppUsers { get; set; } = new();

        [BindProperty]
        public TicketLog NewLog { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (id <= 0) return RedirectToPage("UpdateTicket");

            await LoadTicketDataAsync(id);
            if (CurrentTicket.TicketID == 0) return NotFound();

            await LoadLogsAsync(id);
            await LoadActiveUsersAsync();

            NewLog.Ticketid = id;
            NewLog.Status = CurrentTicket.Status;
            NewLog.AssignedTo = CurrentTicket.AssignedTo;

            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (string.IsNullOrWhiteSpace(NewLog.Actionlog))
            {
                ErrorMessage = "Action log cannot be empty.";
                await LoadTicketDataAsync(NewLog.Ticketid);
                await LoadLogsAsync(NewLog.Ticketid);
                await LoadActiveUsersAsync();
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                using (var trans = conn.BeginTransaction())
                {
                    try
                    {
                        // 1. Insert Log
                        string logQuery = @"INSERT INTO Ticket_logs (Ticketid, Actionlog, logAT, logBy, AssignedTo, Status) 
                                            VALUES (@Tid, @Log, GETDATE(), @User, @AssignedTo, @Status)";
                        using (var cmd = new SqlCommand(logQuery, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Tid", NewLog.Ticketid);
                            cmd.Parameters.AddWithValue("@Log", NewLog.Actionlog);
                            cmd.Parameters.AddWithValue("@User", User.Identity?.Name ?? "System");
                            cmd.Parameters.AddWithValue("@AssignedTo", (object?)NewLog.AssignedTo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Status", (object?)NewLog.Status ?? "Open");
                            await cmd.ExecuteNonQueryAsync();
                        }

                        // 2. Update Ticket
                        string ticketQuery = @"UPDATE Tickets SET Status = @Status, AssignedTo = @AssignedTo WHERE TicketID = @Tid";
                        using (var cmd = new SqlCommand(ticketQuery, conn, trans))
                        {
                            cmd.Parameters.AddWithValue("@Status", (object?)NewLog.Status ?? "Open");
                            cmd.Parameters.AddWithValue("@AssignedTo", (object?)NewLog.AssignedTo ?? DBNull.Value);
                            cmd.Parameters.AddWithValue("@Tid", NewLog.Ticketid);
                            await cmd.ExecuteNonQueryAsync();
                        }

                        trans.Commit();
                        SuccessMessage = "Action log saved and ticket updated!";
                    }
                    catch (Exception ex)
                    {
                        trans.Rollback();
                        ErrorMessage = "Error saving log: " + ex.Message;
                    }
                }
            }

            await LoadTicketDataAsync(NewLog.Ticketid);
            await LoadLogsAsync(NewLog.Ticketid);
            await LoadActiveUsersAsync();
            NewLog.Actionlog = ""; // Clear for next entry

            return Page();
        }

        private async Task LoadTicketDataAsync(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"SELECT T.TicketID, T.CompanyID, T.RaiseDate, T.Priority, T.Description, T.Summary, T.RaisedBy, T.AssignedTo, T.Status, C.CompanyName 
                                 FROM Tickets T 
                                 INNER JOIN CompanyMaster C ON T.CompanyID = C.CompanyId 
                                 WHERE T.TicketID = @Id";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            CurrentTicket = new Ticket
                            {
                                TicketID = reader.GetInt32(0),
                                CompanyID = reader.GetInt32(1),
                                RaiseDate = reader.GetDateTime(2),
                                Priority = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Description = reader.IsDBNull(4) ? null : reader.GetString(4),
                                Summary = reader.IsDBNull(5) ? null : reader.GetString(5),
                                RaisedBy = reader.IsDBNull(6) ? null : reader.GetString(6),
                                AssignedTo = reader.IsDBNull(7) ? null : reader.GetString(7),
                                Status = reader.IsDBNull(8) ? "Open" : reader.GetString(8),
                                CompanyName = reader.GetString(9)
                            };
                        }
                    }
                }
            }
        }

        private async Task LoadLogsAsync(int ticketId)
        {
            Logs.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = "SELECT Logid, Ticketid, Actionlog, logAT, logBy, AssignedTo, Status FROM Ticket_logs WHERE Ticketid = @Id ORDER BY logAT DESC";
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", ticketId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            Logs.Add(new TicketLog
                            {
                                Logid = reader.GetInt32(0),
                                Ticketid = reader.GetInt32(1),
                                Actionlog = reader.IsDBNull(2) ? "" : reader.GetString(2),
                                LogAT = reader.GetDateTime(3),
                                LogBy = reader.IsDBNull(4) ? "" : reader.GetString(4),
                                AssignedTo = reader.IsDBNull(5) ? "" : reader.GetString(5),
                                Status = reader.IsDBNull(6) ? "" : reader.GetString(6)
                            });
                        }
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
