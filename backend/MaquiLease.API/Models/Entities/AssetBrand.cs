using System.ComponentModel.DataAnnotations;

namespace MaquiLease.API.Models.Entities
{
    public class AssetBrand
    {
        [Key]
        public int AssetBrandId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string Label { get; set; } = string.Empty;
    }
}
