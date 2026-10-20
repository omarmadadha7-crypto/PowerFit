namespace Powerfit.Business.Validators;

public static class ExerciseValidator
{
    /// <summary>Shared by Create and Update (same fields, same rules as the SQL CHECKs).</summary>
    public static void Validate(
        string? name,
        string? muscleGroup,
        string? exerciseType,
        string? equipment,
        string? difficultyLevel,
        string? contraindicatedAreas)
    {
        var v = new ValidationBuilder();
        v.Required("name", name, 100)
         .Required("muscleGroup", muscleGroup, 50)
         .OneOf("exerciseType", exerciseType, AllowedValues.ExerciseTypeValues, required: true)
         .Required("equipment", equipment, 30)
         .OneOf("difficultyLevel", difficultyLevel, AllowedValues.LevelValues, required: true)
         .Optional("contraindicatedAreas", contraindicatedAreas, 200);
        v.ThrowIfInvalid();
    }
}
