using System.Net;
using System.Net.Mail;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace MindtecFinance.Pages;

public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;
    private readonly IConfiguration _configuration;

    public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    [BindProperty]
    public string UserName { get; set; } = string.Empty;

    [BindProperty]
    public string MobileNumber { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string? Message { get; set; }

    public string? LblMessage { get; set; }

    public void OnGet()
    {
    }

    public IActionResult OnPost()
    {
        bool isAjax = Request.Headers["X-Requested-With"] == "XMLHttpRequest";
        try
        {
            string user = _configuration["Smtp:User"] ?? "info@lbcosmotech.com";
            // From must be an address the SMTP server accepts for this login (usually the same as User).
            string from = _configuration["Smtp:From"] ?? user;

            using var mail = new MailMessage();
            mail.From = new MailAddress(from, "Mindtec Website");

            // Recipients are configurable: "Smtp:To" and "Smtp:Cc" as semicolon-separated lists.
            string to = _configuration["Smtp:To"] ?? "Dheeraj@mindtec.ca";
            string cc = _configuration["Smtp:Cc"] ?? "ashokrana1977@gmail.com;Info@mindtec.ca";
            foreach (var a in to.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mail.To.Add(a);
            foreach (var a in cc.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                mail.CC.Add(a);
            if (!string.IsNullOrWhiteSpace(Email))
            {
                try { mail.ReplyToList.Add(new MailAddress(Email.Trim(), UserName)); } catch (FormatException) { }
            }

            mail.Subject = $"Consultation Request from {UserName}";
            mail.Body = $"Name: {UserName}\nMobile: {MobileNumber}\nEmail: {Email}\nMessage: {Message}";

            using var smtp = new SmtpClient(_configuration["Smtp:Host"] ?? "mail.lbcosmotech.com")
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(user, _configuration["Smtp:Password"]),
                Port = _configuration.GetValue("Smtp:Port", 25),
                EnableSsl = _configuration.GetValue("Smtp:EnableSsl", false)
            };
            smtp.Send(mail);

            LblMessage = "Mail Sent";
            if (isAjax)
            {
                return new JsonResult(new { success = true, message = LblMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending consultation email via SMTP");
            LblMessage = "Error sending mail: " + ex.Message;
            if (isAjax)
            {
                return new JsonResult(new { success = false, message = LblMessage });
            }
        }

        return Page();
    }
}
