using System.ComponentModel.DataAnnotations;

namespace MindtecFinance.Models
{
    public class AppUser
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = string.Empty;

        [Required]
        [StringLength(25, MinimumLength = 5, ErrorMessage = "Password must be at least 5 characters long")]
        public string Password { get; set; } = string.Empty; // In a real app we'd hash this

        [Required]
        public int UserTypeId { get; set; } // Foreign Key to UserTypes table

        [Required]
        [StringLength(255)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        [StringLength(25)]
        public string Status { get; set; } = "active"; // e.g., active, inactive
    }
}
