using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize]
    public class UsersModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public UsersModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<UserRow> Users { get; set; } = new();
        public List<UserTypeOption> UserTypeOptions { get; set; } = new();

        public class UserTypeOption
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
        }

        [BindProperty]
        public UserFormInput NewUser { get; set; } = new();

        [BindProperty]
        public UserFormInput EditUser { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        // ──── GET: Load all users ────
        public async Task OnGetAsync()
        {
            await LoadUsersAsync();
            await LoadUserTypesAsync();
        }

        // ──── POST: Create new user ────
        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUser.UserName) ||
                string.IsNullOrWhiteSpace(NewUser.Password) ||
                string.IsNullOrWhiteSpace(NewUser.Email))
            {
                ErrorMessage = "UserName, Password, and Email are required.";
                await LoadUsersAsync();
                await LoadUserTypesAsync();
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                // Check if username already exists
                string checkQuery = "SELECT COUNT(*) FROM AppUsers WHERE UserName = @UserName";
                using (var checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@UserName", NewUser.UserName);
                    int count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        ErrorMessage = $"Username '{NewUser.UserName}' already exists.";
                        await LoadUsersAsync();
                        await LoadUserTypesAsync();
                        return Page();
                    }
                }

                string insertQuery = @"INSERT INTO AppUsers (UserName, PasswordHash, UserTypeId, Email, Status)
                                       VALUES (@UserName, @Password, @UserTypeId, @Email, @Status)";

                byte[] hash;
                using (var sha = SHA256.Create())
                {
                    hash = sha.ComputeHash(Encoding.UTF8.GetBytes(NewUser.Password));
                }

                using (var cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@UserName", NewUser.UserName);
                    //cmd.Parameters.AddWithValue("@Password", NewUser.Password);
                    cmd.Parameters.Add("@Password", SqlDbType.VarBinary, 32).Value = hash;
                    cmd.Parameters.AddWithValue("@UserTypeId", NewUser.UserTypeId);
                    cmd.Parameters.AddWithValue("@Email", NewUser.Email);
                    cmd.Parameters.AddWithValue("@Status", NewUser.Status ?? "active");
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            SuccessMessage = $"User '{NewUser.UserName}' created successfully.";
            await LoadUsersAsync();
            await LoadUserTypesAsync();
            return Page();
        }

        // ──── POST: Edit existing user ────
        public async Task<IActionResult> OnPostEditAsync()
        {
            if (EditUser.Id == 0)
            {
                ErrorMessage = "Invalid user selected.";
                await LoadUsersAsync();
                await LoadUserTypesAsync();
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                // If password is blank, don't update it
                string updateQuery;
                if (string.IsNullOrWhiteSpace(EditUser.Password))
                {
                    updateQuery = @"UPDATE AppUsers SET UserName = @UserName, UserTypeId = @UserTypeId, 
                                    Email = @Email, Status = @Status WHERE Id = @Id";
                }
                else
                {
                    updateQuery = @"UPDATE AppUsers SET UserName = @UserName, PasswordHash = @Password, 
                                    UserTypeId = @UserTypeId, Email = @Email, Status = @Status WHERE Id = @Id";
                }

                using (var cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", EditUser.Id);
                    cmd.Parameters.AddWithValue("@UserName", EditUser.UserName);
                    cmd.Parameters.AddWithValue("@UserTypeId", EditUser.UserTypeId);
                    cmd.Parameters.AddWithValue("@Email", EditUser.Email);
                    cmd.Parameters.AddWithValue("@Status", EditUser.Status ?? "active");

                    if (!string.IsNullOrWhiteSpace(EditUser.Password))
                    {
                        byte[] hash;
                        using (var sha = SHA256.Create())
                        {
                            hash = sha.ComputeHash(Encoding.UTF8.GetBytes(EditUser.Password));
                        }
                        cmd.Parameters.Add("@Password", SqlDbType.VarBinary, 32).Value = hash;
                        //cmd.Parameters.AddWithValue("@Password", EditUser.Password);
                    }

                    await cmd.ExecuteNonQueryAsync();
                }
            }

            SuccessMessage = $"User '{EditUser.UserName}' updated successfully.";
            await LoadUsersAsync();
            await LoadUserTypesAsync();
            return Page();
        }

        // ──── Helper: Load all users ────
        private async Task LoadUsersAsync()
        {
            Users.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = @"SELECT AU.Id, AU.UserName, UT.TypeName, AU.Email, AU.Status, AU.UserTypeId 
                                FROM AppUsers AU 
                                JOIN UserTypes UT ON AU.UserTypeId = UT.Id 
                                WHERE AU.UserName <> 'SA' 
                                ORDER BY AU.Id";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        Users.Add(new UserRow
                        {
                            Id = reader.GetInt32(0),
                            UserName = reader.GetString(1),
                            UserType = reader.GetString(2),
                            Email = reader.GetString(3),
                            Status = reader.GetString(4),
                            UserTypeId = reader.GetInt32(5)
                        });
                    }
                }
            }
        }

        // ──── Helper: Load User Type Options ────
        private async Task LoadUserTypesAsync()
        {
            UserTypeOptions.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = "SELECT Id, TypeName FROM UserTypes ORDER BY TypeName";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        UserTypeOptions.Add(new UserTypeOption { Id = reader.GetInt32(0), Name = reader.GetString(1) });
                    }
                }
            }

            if (UserTypeOptions.Count == 0)
            {
                // Fallback built-in types (This might need a different approach now that we use IDs)
                UserTypeOptions.Add(new UserTypeOption { Id = 1, Name = "Admin" });
                UserTypeOptions.Add(new UserTypeOption { Id = 2, Name = "User" });
            }
        }

        // ──── View model for table rows ────
        public class UserRow
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string UserType { get; set; } = string.Empty;
            public int UserTypeId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
        }

        // ──── Form input model ────
        public class UserFormInput
        {
            public int Id { get; set; }
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            
            [Required(ErrorMessage = "User Type is required")]
            public int UserTypeId { get; set; }
            public string Email { get; set; } = string.Empty;
            public string Status { get; set; } = "active";
        }
    }
}
