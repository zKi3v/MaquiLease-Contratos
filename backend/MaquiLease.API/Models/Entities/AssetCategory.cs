using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.Entities
{
    public class AssetCategory
    {
        [Key]
        public int AssetCategoryId { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty;
    }
}
