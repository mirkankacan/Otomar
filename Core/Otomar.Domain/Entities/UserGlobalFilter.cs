using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Otomar.Domain.Entities
{
    public class UserGlobalFilter
    {
        public UserGlobalFilter()
        {
            CreatedDate = DateTime.Now;
        }

        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(450)]
        public string UserId { get; set; }

        [Required]
        [MaxLength(50)]
        public string FilterType { get; set; } // "Brand", "Category", "Supplier", etc.

        [Required]
        [MaxLength(200)]
        public string FilterValue { get; set; }

        public DateTime CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        [ForeignKey("UserId")]
        public virtual ApplicationUser User { get; set; }
    }
}