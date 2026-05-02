using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace API.Models;

public class Message
{
    [Key]
    public int Id { get; set; }
    public string? Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsRead { get; set; }
    [ForeignKey("SenderId")]
    public AppUser? Sender { get; set; }
    public string? SenderId { get; set; }
    [ForeignKey("ReceiverId")]
    public AppUser? Receiver { get; set; }
    public string? ReceiverId { get; set; }
}
