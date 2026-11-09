namespace RaceDay.MVC.Models
{
    public class EventDetailsViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public string Location { get; set; } = string.Empty;
        public decimal Distance { get; set; }
        public string Organiser { get; set; } = string.Empty;

        public List<CategoryViewModel> Categories { get; set; } = new();
        public string? ImageUrl { get; set; }
    }

    public class CategoryViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int MinAge { get; set; }
        public int MaxAge { get; set; }
        public string? Gender { get; set; }
        public decimal Fee { get; set; }
    }
}
