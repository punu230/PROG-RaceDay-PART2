using System.ComponentModel.DataAnnotations;

namespace RaceDay.API.DTOs.Categories;

public class CreateCategoryDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [Range(0, 100)]
    public int? AgeGroupMin { get; set; }

    [Range(0, 100)]
    public int? AgeGroupMax { get; set; }

    [RegularExpression("[MF]")]
    public char? Gender { get; set; }

    [Required]
    [Range(0, 10000)]
    public decimal EntryFee { get; set; }
}