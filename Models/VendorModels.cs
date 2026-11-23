using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MindtecFinance.Models
{
    public class Vendor
    {
        [Key]
        public int VendorId { get; set; }

        [Required]
        [StringLength(200)]
        public string VendorName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Phone { get; set; }

        [EmailAddress]
        [StringLength(100)]
        public string? Email { get; set; }

        public string? Address { get; set; }

        public string? PrimaryContactName { get; set; }

        public List<VendorContact> Contacts { get; set; } = new();
        public List<VendorWebsite> Websites { get; set; } = new();
    }

    public class VendorContact
    {
        [Key]
        public int ContactId { get; set; }

        public int VendorId { get; set; }

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

    public class VendorWebsite
    {
        [Key]
        public int WebsiteId { get; set; }

        public int VendorId { get; set; }

        [Required]
        [StringLength(200)]
        public string WebsiteUrl { get; set; } = string.Empty;

        [StringLength(100)]
        public string? UserName { get; set; }

        [StringLength(200)]
        public string? Password { get; set; }
    }
}
