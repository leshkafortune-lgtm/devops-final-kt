namespace WorkoutApp.Models;
public class Workout
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string MuscleGroup { get; set; } = "";
    public int Sets { get; set; }
    public int[] Reps { get; set; } = Array.Empty<int>();
    public double? Weight { get; set; }
    public string WeightUnit { get; set; } = "kg";
    public string Difficulty { get; set; } = "";
}