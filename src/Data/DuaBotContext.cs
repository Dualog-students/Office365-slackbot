using Microsoft.EntityFrameworkCore;

namespace DuaBot.Data
{
    public class DuaBotContext : DbContext
    {
        public DbSet<UserTokenMap> UserTokens { get; set; }
        public DbSet<SlackUpdateTask> SlackUpdateTasks { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder.UseSqlite("Data Source=db.sqlite3");
    }
}
