using System;
using System.ComponentModel.DataAnnotations;

namespace MindtecFinance.Models
{
    public class CoConfiguration
    {
        [Key]
        public int ConfigId { get; set; }

        [Required]
        public int CompanyId { get; set; }

        public int? ConfigTypeId { get; set; }
        public string? ConfigTypeName { get; set; }

        [Required]
        [StringLength(200)]
        public string ConfName { get; set; } = string.Empty;

        public string? Description { get; set; }

        [StringLength(200)]
        public string? Server { get; set; }

        [StringLength(100)]
        public string? UserName { get; set; }

        [StringLength(200)]
        public string? Password { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? CreatedBy { get; set; }

        public DateTime UpdateAt { get; set; } = DateTime.Now;

        public string? UpdatedBy { get; set; }
    }
}
