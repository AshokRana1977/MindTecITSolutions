using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using System.Collections.Generic;
using System.Data;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MindtecFinance.Pages
{
    public class LoginModel : PageModel
    {
        private readonly IConfiguration _configuration;

        public LoginModel(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            string? userName = null;
            string? email = null;
            string? userType = null;
            string? status = null;

            // Validate user credentials directly from the "users" table using ADO.NET
            string connectionString = _configuration.GetConnectionString("DefaultConnection")!;

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                await connection.OpenAsync();

                string query = @"SELECT AU.UserName, AU.Email, UT.TypeName, AU.Status 
                                FROM AppUsers AU 
                                JOIN UserTypes UT ON AU.UserTypeId = UT.Id 
                                WHERE AU.UserName = @UserName AND AU.PasswordHash = @PasswordHash";

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    byte[] hash;
                    using (var sha = SHA256.Create())
                    {
                        hash = sha.ComputeHash(Encoding.UTF8.GetBytes(Input.Password));
                    }

                    command.Parameters.AddWithValue("@UserName", Input.UserName);
                    command.Parameters.Add("@PasswordHash", SqlDbType.VarBinary, 32).Value = hash;
                    
                    //command.Parameters.AddWithValue("@Password", Input.Password);
                    //command.Parameters.Add("@Password", SqlDbType.NVarChar, 256).Value = Input.Password;

                    using (SqlDataReader reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            userName = reader["UserName"].ToString();
                            email = reader["Email"].ToString();
                            userType = reader["TypeName"].ToString();
                            status = reader["Status"].ToString();
                        }
                    }
                }
            }

            if (userName != null && !string.Equals(status, "active", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(string.Empty, "In-Active User");
                return Page();
            }

            if (userName != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userName),
                    new Claim(ClaimTypes.Email, email ?? string.Empty),
                    new Claim(ClaimTypes.Role, userType ?? "user")
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                return RedirectToPage("/Dashboard/Index");
            }

            ModelState.AddModelError(string.Empty, "Invalid login attempt.");
            return Page();
        }

        public class LoginInput
        {
            public string UserName { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
        }
    }
}
