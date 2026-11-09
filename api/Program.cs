using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.EntityFrameworkCore;
using WorkoutApp.Data;
using WorkoutApp.Models;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis");
    options.InstanceName = "WorkoutApp:";
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    if (!db.Workouts.Any())
    {
        db.Workouts.AddRange(
            new Workout { Name = "Грудь и трицепс", Description = "Жим лежа", MuscleGroup = "Грудь", DurationMinutes = 60, Difficulty = "Средняя" },
            new Workout { Name = "Спина и бицепс", Description = "Подтягивания", MuscleGroup = "Спина", DurationMinutes = 65, Difficulty = "Средняя" }
        );
        db.SaveChanges();
    }
}

app.MapGet("/workouts", async (AppDbContext db, IDistributedCache cache) =>
{
    var key = "all_workouts";
    var cached = await cache.GetStringAsync(key);
    if (cached != null) return JsonSerializer.Deserialize<List<Workout>>(cached);
    var list = await db.Workouts.ToListAsync();
    await cache.SetStringAsync(key, JsonSerializer.Serialize(list),
        new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5) });
    return list;
});

app.MapGet("/workouts/{id}", async (int id, AppDbContext db) =>
    await db.Workouts.FindAsync(id) is Workout w ? Results.Ok(w) : Results.NotFound());

app.MapPost("/workouts", async (Workout w, AppDbContext db, IDistributedCache cache) =>
{
    db.Workouts.Add(w);
    await db.SaveChangesAsync();
    await cache.RemoveAsync("all_workouts");
    return Results.Created($"/workouts/{w.Id}", w);
});

app.MapPut("/workouts/{id}", async (int id, Workout input, AppDbContext db, IDistributedCache cache) =>
{
    var w = await db.Workouts.FindAsync(id);
    if (w is null) return Results.NotFound();
    w.Name = input.Name; w.Description = input.Description; w.MuscleGroup = input.MuscleGroup;
    w.DurationMinutes = input.DurationMinutes; w.Difficulty = input.Difficulty;
    await db.SaveChangesAsync();
    await cache.RemoveAsync("all_workouts");
    return Results.Ok(w);
});

app.MapDelete("/workouts/{id}", async (int id, AppDbContext db, IDistributedCache cache) =>
{
    var w = await db.Workouts.FindAsync(id);
    if (w is null) return Results.NotFound();
    db.Workouts.Remove(w);
    await db.SaveChangesAsync();
    await cache.RemoveAsync("all_workouts");
    return Results.NoContent();
});

app.Run();