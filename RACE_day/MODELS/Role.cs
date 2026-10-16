namespace RaceDay.API.Models;

public class Role
{
    public int RoleID { get; set; }
    public string RoleName { get; set; } = string.Empty;

    // Navigation property
    public ICollection<User> Users { get; set; } = new List<User>();
}