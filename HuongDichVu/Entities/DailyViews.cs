using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace HuongDichVu.Entities
{
    public class DailyViews
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [ForeignKey("Book")]
        public int BookId { get; set; }

        [Required]
        public DateTime ViewDate { get; set; }

        [Required]
        public int ViewCountDay { get; set; }

        public virtual Book Book { get; set; }
    }
}
