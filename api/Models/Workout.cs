namespace WorkoutApp.Models;

public class Workout
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string MuscleGroup { get; set; } = string.Empty;
    public int DurationMinutes { get; set; }
    public string Difficulty { get; set; } = string.Empty;
}