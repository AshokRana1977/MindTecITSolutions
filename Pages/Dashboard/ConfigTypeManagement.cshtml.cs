using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace MindtecFinance.Pages.Dashboard
{
    [Authorize(Roles = "SA, Admin")]
    public class ConfigTypeManagementModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public ConfigTypeManagementModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public List<UserTypeRow> UserTypes { get; set; } = new();

        [BindProperty]
        public UserTypeFormInput NewUserType { get; set; } = new();

        [BindProperty]
        public UserTypeFormInput EditUserType { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await LoadUserTypesAsync();
        }

        public async Task<IActionResult> OnPostCreateAsync()
        {
            if (string.IsNullOrWhiteSpace(NewUserType.TypeName))
            {
                ErrorMessage = "Type Name is required.";
                await LoadUserTypesAsync();
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                // Check if type name already exists
                string checkQuery = "SELECT COUNT(*) FROM ConfigTypes WHERE TypeName = @TypeName";
                using (var checkCmd = new SqlCommand(checkQuery, conn))
                {
                    checkCmd.Parameters.AddWithValue("@TypeName", NewUserType.TypeName);
                    int count = (int)await checkCmd.ExecuteScalarAsync();
                    if (count > 0)
                    {
                        ErrorMessage = $"Configuration Type '{NewUserType.TypeName}' already exists.";
                        await LoadUserTypesAsync();
                        return Page();
                    }
                }

                string insertQuery = "INSERT INTO ConfigTypes (TypeName, Description) VALUES (@TypeName, @Description)";
                using (var cmd = new SqlCommand(insertQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@TypeName", NewUserType.TypeName);
                    cmd.Parameters.AddWithValue("@Description", NewUserType.Description ?? (object)DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            SuccessMessage = $"Configuration Type '{NewUserType.TypeName}' created successfully.";
            ModelState.Clear();
            NewUserType = new UserTypeFormInput();
            await LoadUserTypesAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostEditAsync()
        {
            if (EditUserType.Id == 0 || string.IsNullOrWhiteSpace(EditUserType.TypeName))
            {
                ErrorMessage = "Invalid data provided.";
                await LoadUserTypesAsync();
                return Page();
            }

            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();

                string updateQuery = "UPDATE ConfigTypes SET TypeName = @TypeName, Description = @Description WHERE Id = @Id";
                using (var cmd = new SqlCommand(updateQuery, conn))
                {
                    cmd.Parameters.AddWithValue("@Id", EditUserType.Id);
                    cmd.Parameters.AddWithValue("@TypeName", EditUserType.TypeName);
                    cmd.Parameters.AddWithValue("@Description", EditUserType.Description ?? (object)DBNull.Value);
                    await cmd.ExecuteNonQueryAsync();
                }
            }

            SuccessMessage = $"Configuration Type '{EditUserType.TypeName}' updated successfully.";
            ModelState.Clear();
            EditUserType = new UserTypeFormInput();
            await LoadUserTypesAsync();
            return Page();
        }

        private async Task LoadUserTypesAsync()
        {
            UserTypes.Clear();
            string connStr = _configuration.GetConnectionString("DefaultConnection")!;

            using (var conn = new SqlConnection(connStr))
            {
                await conn.OpenAsync();
                string query = "SELECT Id, TypeName, Description FROM ConfigTypes ORDER BY Id";

                using (var cmd = new SqlCommand(query, conn))
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        UserTypes.Add(new UserTypeRow
                        {
                            Id = reader.GetInt32(0),
                            TypeName = reader.GetString(1),
                            Description = reader.IsDBNull(2) ? null : reader.GetString(2)
                        });
                    }
                }
            }
        }

        public class UserTypeRow
        {
            public int Id { get; set; }
            public string TypeName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        public class UserTypeFormInput
        {
            public int Id { get; set; }
            public string TypeName { get; set; } = string.Empty;
            public string? Description { get; set; }
        }
    }
}
