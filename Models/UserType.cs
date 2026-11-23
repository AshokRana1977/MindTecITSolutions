using System.ComponentModel.DataAnnotations;

namespace MindtecFinance.Models
{
    public class UserType
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string TypeName { get; set; } = string.Empty;

        [StringLength(255)]
        public string? Description { get; set; }
    }
}
