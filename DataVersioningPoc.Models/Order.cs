namespace DataVersioningPoc.Models;

[DataStructureVersion(1)]
public class Order
{
    public int Id { get; set; }

    public DateTime CreatedAt { get; set; }

    public decimal TotalAmount { get; set; }
}