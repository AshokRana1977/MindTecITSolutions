using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MindtecFinance.Models
{
    public class Company
    {
        [Key]
        public int CompanyId { get; set; }

        [Required]
        [StringLength(200)]
        public string CompanyName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? CompanyPhone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? CompanyEmail { get; set; }

        public string? CompanyAddress { get; set; }

        [StringLength(100)]
        public string? CompanyWebsite { get; set; }

        public string? PrimaryContactName { get; set; }

        public List<Contact> Contacts { get; set; } = new();
    }

    public class Contact
    {
        [Key]
        public int ContactID { get; set; }

        public int CompanyId { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Title { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        public bool PrimaryContact { get; set; }
    }
}
