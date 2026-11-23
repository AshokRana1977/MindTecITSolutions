using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MindtecFinance.Models;
using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class CoConfigurationModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public CoConfigurationModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Company Company { get; set; } = new();
        public List<CoConfiguration> Configurations { get; set; } = new();
        public List<SelectListItem> ConfigTypes { get; set; } = new();

        [BindProperty]
        public CoConfiguration NewConfig { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ReturnUrl { get; set; }

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync(int companyId)
        {
            if (companyId <= 0) return RedirectToPage("/Dashboard/NewTicket");

            await LoadDataAsync(companyId);

            if (Company.CompanyId == 0) return RedirectToPage("/Dashboard/NewTicket");

            return Page();
        }

        public async Task<IActionResult> OnPostAddAsync()
        {
            if (NewConfig.CompanyId == 0 || string.IsNullOrWhiteSpace(NewConfig.ConfName))
            {
                ErrorMessage = "Name is required.";
                await LoadDataAsync(NewConfig.CompanyId);
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"INSERT INTO CoConfigurations (CompanyId, ConfigTypeId, ConfName, Description, Server, UserName, Password, CreatedBy, UpdatedBy) 
                                 VALUES (@CompId, @ConfigTypeId, @Name, @Desc, @Serv, @User, @Pass, @CreatedBy, @UpdatedBy)";
                
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@CompId", NewConfig.CompanyId);
                    cmd.Parameters.AddWithValue("@ConfigTypeId", (object?)NewConfig.ConfigTypeId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", NewConfig.ConfName);
                    cmd.Parameters.AddWithValue("@Desc", (object?)NewConfig.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Serv", (object?)NewConfig.Server ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@User", (object?)NewConfig.UserName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Pass", (object?)NewConfig.Password ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@CreatedBy", User.Identity?.Name ?? "Admin");
                    cmd.Parameters.AddWithValue("@UpdatedBy", User.Identity?.Name ?? "Admin");
                    
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage(new { companyId = NewConfig.CompanyId, returnUrl = ReturnUrl });
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (NewConfig.ConfigId == 0 || string.IsNullOrWhiteSpace(NewConfig.ConfName))
            {
                ErrorMessage = "Invalid configuration data.";
                await LoadDataAsync(NewConfig.CompanyId);
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"UPDATE CoConfigurations 
                                 SET ConfigTypeId = @ConfigTypeId, ConfName = @Name, Description = @Desc, Server = @Serv, 
                                     UserName = @User, Password = @Pass, UpdateAt = GETDATE(), UpdatedBy = @UpdatedBy 
                                 WHERE ConfigId = @Id";
                
                using (var cmd = new SqlCommand(query, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", NewConfig.ConfigId);
                    cmd.Parameters.AddWithValue("@ConfigTypeId", (object?)NewConfig.ConfigTypeId ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Name", NewConfig.ConfName);
                    cmd.Parameters.AddWithValue("@Desc", (object?)NewConfig.Description ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Serv", (object?)NewConfig.Server ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@User", (object?)NewConfig.UserName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Pass", (object?)NewConfig.Password ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UpdatedBy", User.Identity?.Name ?? "Admin");
                    
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            return RedirectToPage(new { companyId = NewConfig.CompanyId, returnUrl = ReturnUrl });
        }

        private async Task LoadDataAsync(int companyId)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Load Company
                string compQuery = "SELECT CompanyId, CompanyName, CompanyPhone, CompanyEmail, CompanyAddress, CompanyWebsite FROM CompanyMaster WHERE CompanyId = @Id";
                using (var cmd = new SqlCommand(compQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", companyId);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            Company = new Company
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

                if (Company.CompanyId != 0)
                {
                    // Load Contacts
                    string contQuery = "SELECT ContactID, Name, Title, Email, Phone, PrimaryContact FROM ContactDetail WHERE CompanyId = @Id ORDER BY PrimaryContact DESC, Name ASC";
                    using (var cmd = new SqlCommand(contQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", companyId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Company.Contacts.Add(new Contact
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

                    // Load Config Types for Dropdown
                    ConfigTypes.Clear();
                    string typesQuery = "SELECT Id, TypeName FROM ConfigTypes ORDER BY TypeName";
                    using (var cmd = new SqlCommand(typesQuery, conn))
                    {
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                ConfigTypes.Add(new SelectListItem 
                                { 
                                    Value = reader.GetInt32(0).ToString(), 
                                    Text = reader.GetString(1) 
                                });
                            }
                        }
                    }

                    // Load Configurations
                    Configurations.Clear();
                    string confQuery = @"SELECT c.ConfigId, c.CompanyId, c.ConfName, c.Description, c.Server, c.UserName, c.Password, 
                                                c.CreatedAt, c.CreatedBy, c.UpdateAt, c.UpdatedBy, c.ConfigTypeId, ct.TypeName 
                                         FROM CoConfigurations c
                                         LEFT JOIN ConfigTypes ct ON c.ConfigTypeId = ct.Id
                                         WHERE c.CompanyId = @Id ORDER BY c.CreatedAt DESC";
                    using (var cmd = new SqlCommand(confQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", companyId);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Configurations.Add(new CoConfiguration
                                {
                                    ConfigId = reader.GetInt32(0),
                                    CompanyId = reader.GetInt32(1),
                                    ConfName = reader.GetString(2),
                                    Description = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Server = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    UserName = reader.IsDBNull(5) ? null : reader.GetString(5),
                                    Password = reader.IsDBNull(6) ? null : reader.GetString(6),
                                    CreatedAt = reader.GetDateTime(7),
                                    CreatedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
                                    UpdateAt = reader.GetDateTime(9),
                                    UpdatedBy = reader.IsDBNull(10) ? null : reader.GetString(10),
                                    ConfigTypeId = reader.IsDBNull(11) ? null : reader.GetInt32(11),
                                    ConfigTypeName = reader.IsDBNull(12) ? null : reader.GetString(12)
                                });
                            }
                        }
                    }
                }
            }
        }
    }
}
