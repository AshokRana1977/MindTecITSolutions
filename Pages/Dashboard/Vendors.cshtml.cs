using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using MindtecFinance.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class VendorsModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public VendorsModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<Vendor> Vendors { get; set; } = new();

        [BindProperty]
        public Vendor InputVendor { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            await LoadVendorsAsync();
            return Page();
        }

        // ──── AJAX: Get Vendor Details with Contacts & Websites ────
        public async Task<IActionResult> OnGetDetailsAsync(int id)
        {
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;
            Vendor? vendor = null;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Get Vendor
                string vendQuery = "SELECT VendorId, VendorName, Phone, Email, Address FROM VendorMaster WHERE VendorId = @Id";
                using (var cmd = new SqlCommand(vendQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            vendor = new Vendor
                            {
                                VendorId = reader.GetInt32(0),
                                VendorName = reader.GetString(1),
                                Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                                Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                Address = reader.IsDBNull(4) ? null : reader.GetString(4)
                            };
                        }
                    }
                }

                if (vendor != null)
                {
                    // Get Contacts
                    string contQuery = "SELECT ContactId, Name, Title, Email, Phone, PrimaryContact FROM VendorContact WHERE VendorId = @Id ORDER BY PrimaryContact DESC, Name ASC";
                    using (var cmd = new SqlCommand(contQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                vendor.Contacts.Add(new VendorContact
                                {
                                    ContactId = reader.GetInt32(0),
                                    Name = reader.GetString(1),
                                    Title = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                                    Phone = reader.IsDBNull(4) ? null : reader.GetString(4),
                                    PrimaryContact = reader.GetBoolean(5)
                                });
                            }
                        }
                    }

                    // Get Websites
                    string webQuery = "SELECT WebsiteId, WebsiteUrl, UserName, Password FROM VendorWebsite WHERE VendorId = @Id ORDER BY WebsiteId ASC";
                    using (var cmd = new SqlCommand(webQuery, conn))
                    {
                        cmd.Parameters.AddWithValue("@Id", id);
                        using (var reader = await cmd.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                vendor.Websites.Add(new VendorWebsite
                                {
                                    WebsiteId = reader.GetInt32(0),
                                    WebsiteUrl = reader.GetString(1),
                                    UserName = reader.IsDBNull(2) ? null : reader.GetString(2),
                                    Password = reader.IsDBNull(3) ? null : reader.GetString(3)
                                });
                            }
                        }
                    }
                    
                    return new JsonResult(vendor);
                }
            }

            return new NotFoundResult();
        }

        // ──── POST: Save (Create or Update) ────
        public async Task<IActionResult> OnPostSaveAsync()
        {
            if (string.IsNullOrWhiteSpace(InputVendor.VendorName))
            {
                ErrorMessage = "Vendor Name is required.";
                await LoadVendorsAsync();
                return Page();
            }

            // Enforce single primary contact logic
            int primaryCount = 0;
            if (InputVendor.Contacts != null)
            {
                foreach (var c in InputVendor.Contacts)
                {
                    if (c.PrimaryContact) primaryCount++;
                }
            }

            if (primaryCount > 1)
            {
                ErrorMessage = "Only one primary contact is allowed.";
                await LoadVendorsAsync();
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
                        if (InputVendor.VendorId == 0)
                        {
                            // CREATE
                            string insVend = @"INSERT INTO VendorMaster (VendorName, Phone, Email, Address) 
                                               VALUES (@Name, @Phone, @Email, @Addr);
                                               SELECT SCOPE_IDENTITY();";
                            using (var cmd = new SqlCommand(insVend, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Name", InputVendor.VendorName);
                                cmd.Parameters.AddWithValue("@Phone", (object?)InputVendor.Phone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Email", (object?)InputVendor.Email ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Addr", (object?)InputVendor.Address ?? DBNull.Value);
                                InputVendor.VendorId = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                            }
                        }
                        else
                        {
                            // UPDATE
                            string updVend = @"UPDATE VendorMaster SET VendorName=@Name, Phone=@Phone, Email=@Email, 
                                               Address=@Addr WHERE VendorId=@Id";
                            using (var cmd = new SqlCommand(updVend, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Id", InputVendor.VendorId);
                                cmd.Parameters.AddWithValue("@Name", InputVendor.VendorName);
                                cmd.Parameters.AddWithValue("@Phone", (object?)InputVendor.Phone ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Email", (object?)InputVendor.Email ?? DBNull.Value);
                                cmd.Parameters.AddWithValue("@Addr", (object?)InputVendor.Address ?? DBNull.Value);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Remove existing contacts
                            string delCont = "DELETE FROM VendorContact WHERE VendorId = @Id";
                            using (var cmd = new SqlCommand(delCont, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Id", InputVendor.VendorId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // Remove existing websites
                            string delWeb = "DELETE FROM VendorWebsite WHERE VendorId = @Id";
                            using (var cmd = new SqlCommand(delWeb, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@Id", InputVendor.VendorId);
                                await cmd.ExecuteNonQueryAsync();
                            }
                        }

                        // Insert Contacts
                        if (InputVendor.Contacts != null)
                        {
                            foreach (var contact in InputVendor.Contacts)
                            {
                                if (string.IsNullOrWhiteSpace(contact.Name)) continue;

                                string insCont = @"INSERT INTO VendorContact (VendorId, Name, Title, Email, Phone, PrimaryContact) 
                                                   VALUES (@VendId, @Name, @Title, @Email, @Phone, @Primary)";
                                using (var cmd = new SqlCommand(insCont, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@VendId", InputVendor.VendorId);
                                    cmd.Parameters.AddWithValue("@Name", contact.Name);
                                    cmd.Parameters.AddWithValue("@Title", (object?)contact.Title ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Email", (object?)contact.Email ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Phone", (object?)contact.Phone ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Primary", contact.PrimaryContact);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        // Insert Websites
                        if (InputVendor.Websites != null)
                        {
                            foreach (var website in InputVendor.Websites)
                            {
                                if (string.IsNullOrWhiteSpace(website.WebsiteUrl)) continue;

                                string insWeb = @"INSERT INTO VendorWebsite (VendorId, WebsiteUrl, UserName, Password) 
                                                   VALUES (@VendId, @Url, @User, @Pass)";
                                using (var cmd = new SqlCommand(insWeb, conn, trans))
                                {
                                    cmd.Parameters.AddWithValue("@VendId", InputVendor.VendorId);
                                    cmd.Parameters.AddWithValue("@Url", website.WebsiteUrl);
                                    cmd.Parameters.AddWithValue("@User", (object?)website.UserName ?? DBNull.Value);
                                    cmd.Parameters.AddWithValue("@Pass", (object?)website.Password ?? DBNull.Value);
                                    await cmd.ExecuteNonQueryAsync();
                                }
                            }
                        }

                        trans.Commit();
                        SuccessMessage = $"Vendor '{InputVendor.VendorName}' saved successfully.";
                    }
                    catch (System.Exception ex)
                    {
                        trans.Rollback();
                        ErrorMessage = "Error saving vendor: " + ex.Message;
                    }
                }
            }

            await LoadVendorsAsync();
            return Page();
        }

        private async Task LoadVendorsAsync()
        {
            Vendors.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                
                // Join to get primary contact name if exists
                string query = @"SELECT V.VendorId, V.VendorName, V.Phone, V.Email, V.Address,
                                (SELECT TOP 1 Name FROM VendorContact WHERE VendorId = V.VendorId AND PrimaryContact = 1) as PrimaryContactName
                                FROM VendorMaster V ORDER BY V.VendorName";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Vendors.Add(new Vendor
                        {
                            VendorId = reader.GetInt32(0),
                            VendorName = reader.GetString(1),
                            Phone = reader.IsDBNull(2) ? null : reader.GetString(2),
                            Email = reader.IsDBNull(3) ? null : reader.GetString(3),
                            Address = reader.IsDBNull(4) ? null : reader.GetString(4),
                            PrimaryContactName = reader.IsDBNull(5) ? "No Primary" : reader.GetString(5)
                        });
                    }
                }
            }
        }
    }
}
