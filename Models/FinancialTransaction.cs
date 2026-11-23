using System;
using System.ComponentModel.DataAnnotations;

namespace MindtecFinance.Models
{
    public class FinancialTransaction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public DateTime Date { get; set; } = DateTime.Now;

        [Required]
        public string Type { get; set; } = "Expense"; // Income or Expense

        [Required]
        public string Category { get; set; } = "General";

        [Required]
        public decimal Amount { get; set; }

        public string Description { get; set; } = string.Empty;

        public bool Shared { get; set; }

        public int? OwnerId { get; set; }
        public int? PaidById { get; set; }
        public bool SharedContributionDone { get; set; }

        public string OwnerName { get; set; } = string.Empty;
        public string PaidByName { get; set; } = string.Empty;
    }
}
