using Microsoft.EntityFrameworkCore;
using WorkoutApp.Models;

namespace WorkoutApp.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    
    public DbSet<Workout> Workouts => Set<Workout>();
}