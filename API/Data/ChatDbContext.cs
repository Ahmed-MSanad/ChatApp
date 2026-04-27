using API.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace API.Data;

public class ChatDbContext : IdentityDbContext<AppUser>
{
    public ChatDbContext(DbContextOptions<ChatDbContext> options) : base(options)
    {
        
    }
}
