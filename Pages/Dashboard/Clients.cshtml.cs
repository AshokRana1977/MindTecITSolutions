using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MindtecFinance.Models;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class ClientsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ClientsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Company> Companies { get; set; } = new();

        [BindProperty]
        public Company InputCompany { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadCompaniesAsync();
            return Page();
        }

        // ──── AJAX: Get Company Details with Contacts ────
        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            Company? company = null;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Get Company
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
                    // Get Contacts
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

        // ──── POST: Save (Create or Update) ────
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(InputCompany.CompanyName))
            {
                ErrorMessage = "Company Name is required.";
                await LoadCompaniesAsync();
                return Page();
            }

            // Enforce single primary contact logic
            int primaryCount = 0;
            foreach (var c in InputCompany.Contacts)
            {
                if (c.PrimaryContact) primaryCount++;
            }

            if (primaryCount > 1)
            {
                ErrorMessage = "Only one primary contact is allowed.";
                await LoadCompaniesAsync();
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
                        if (InputCompany.CompanyId == 0)
                        {
                            // CREATE
                            string insComp = @"INSERT INTO CompanyMaster (CompanyName, CompanyPhone, CompanyEmail, CompanyAddress, CompanyWebsite) 
                                               VALUES (@Name, @Phone, @Email, @Addr, @Web);
                                               SELECT SCOPE_IDENTITY();";
                            using (var cmd = new SqlCommand(insComp, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Name", InputCompany.CompanyName);
                                cmd.Parameters.AddWithValue("@Phone", (object?)InputCompany.CompanyPhone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Email", (object?)InputCompany.CompanyEmail ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Addr", (object?)InputCompany.CompanyAddress ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Web", (object?)InputCompany.CompanyWebsite ?? DBNull.Value);
                                InputCompany.CompanyId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }
                        }
                        else
                        {
                            // UPDATE
                            string updComp = @"UPDATE CompanyMaster SET CompanyName=@Name, CompanyPhone=@Phone, CompanyEmail=@Email, 
                                               CompanyAddress=@Addr, CompanyWebsite=@Web WHERE CompanyId=@Id";
                            using (var cmd = new SqlCommand(updComp, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Id", InputCompany.CompanyId);
                                cmd.Parameters.AddWithValue("@Name", InputCompany.CompanyName);
                                cmd.Parameters.AddWithValue("@Phone", (object?)InputCompany.CompanyPhone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Email", (object?)InputCompany.CompanyEmail ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Addr", (object?)InputCompany.CompanyAddress ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Web", (object?)InputCompany.CompanyWebsite ?? DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Remove existing contacts to re-insert (Simpler for this POC, though ideally we'd track IDs)
                            string delCont = "DELETE FROM ContactDetail WHERE CompanyId = @Id";
                            using (var cmd = new SqlCommand(delCont, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Id", InputCompany.CompanyId);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Insert Contacts
                        foreach (var contact in InputCompany.Contacts)
                        {
                            if (string.IsNullOrWhiteSpace(contact.Name)) continue;

                            string insCont = @"INSERT INTO ContactDetail (CompanyId, Name, Title, Email, Phone, PrimaryContact) 
                                               VALUES (@CompId, @Name, @Title, @Email, @Phone, @Primary)";
                            using (var cmd = new SqlCommand(insCont, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@CompId", InputCompany.CompanyId);
                                cmd.Parameters.AddWithValue("@Name", contact.Name);
                                cmd.Parameters.AddWithValue("@Title", (object?)contact.Title ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Email", (object?)contact.Email ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Phone", (object?)contact.Phone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Primary", contact.PrimaryContact);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        trans.Commit();
                        SuccessMessage = $"Company '{InputCompany.CompanyName}' saved successfully.";
                    }
                    catch (System.Exception ex)
                    {
                        trans.Rollback();
                        ErrorMessage = "Error saving company: " + ex.Message;
                    }
                }
            }

            await LoadCompaniesAsync();
            return Page();
        }

        private async Task LoadCompaniesAsync()
        {
            Companies.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Join to get primary contact name if exists
                string query = @"SELECT C.CompanyId, C.CompanyName, C.CompanyPhone, C.CompanyEmail, C.CompanyAddress,
                                (SELECT TOP 1 Name FROM ContactDetail WHERE CompanyId = C.CompanyId AND PrimaryContact = 1) as PrimaryContactName
                                FROM CompanyMaster C ORDER BY C.CompanyName";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Companies.Add(new Company
                        {
                            CompanyId = reader.GetInt32(0),
                            CompanyName = reader.GetString(1),
                            CompanyPhone = reader.IsDBNull(2) ? null : reader.GetString(2),
                            CompanyEmail = reader.IsDBNull(3) ? null : reader.GetString(3),
                            CompanyAddress = reader.IsDBNull(4) ? null : reader.GetString(4),
                            PrimaryContactName = reader.IsDBNull(5) ? "No Primary" : reader.GetString(5)
                        });
                    }
                }
            }
        }
    }
}
