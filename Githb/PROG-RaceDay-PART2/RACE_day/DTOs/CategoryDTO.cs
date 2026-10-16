namespace RaceDay.API.DTOs.Categories;

public class CategoryDto
{
    public int CategoryID { get; set; }
    public int EventID { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? AgeGroupMin { get; set; }
    public int? AgeGroupMax { get; set; }
    public char? Gender { get; set; }
    public decimal EntryFee { get; set; }
}