namespace WorkoutApp.Models;
public class Workout
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string MuscleGroup { get; set; } = "";
    public int Sets { get; set; }
    public int Reps { get; set; }
    public string Difficulty { get; set; } = "";
}