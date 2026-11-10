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
            new Workout { Name = "Грудь и трицепс", Description = "Жим лежа", MuscleGroup = "Грудь", Sets = 4, Reps = new[] { 10, 10, 10, 10 }, Difficulty = "Средняя" },
            new Workout { Name = "Спина и бицепс", Description = "Подтягивания", MuscleGroup = "Спина", Sets = 3, Reps = new[] { 12, 10, 8 }, Difficulty = "Средняя" }
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
    w.Sets = w.Reps?.Length ?? 0;
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
    w.Reps = input.Reps ?? Array.Empty<int>();
    w.Sets = w.Reps.Length;
    w.Difficulty = input.Difficulty;
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