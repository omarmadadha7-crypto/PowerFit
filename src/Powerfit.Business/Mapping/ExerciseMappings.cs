using DataAccess.Entities;
using Powerfit.Business.DTOs.Exercises;
using Powerfit.Business.Validators;

namespace Powerfit.Business.Mapping;

public static class ExerciseMappings
{
    public static ExerciseDto ToDto(this Exercise e) =>
        new(e.ExerciseId, e.Name, e.MuscleGroup, e.ExerciseType, e.Equipment,
            e.DifficultyLevel, e.ContraindicatedAreas, e.IsActive);

    public static Exercise ToEntity(this CreateExerciseRequest r) => new()
    {
        Name = r.Name.Trim(),
        MuscleGroup = r.MuscleGroup.Trim(),
        ExerciseType = MappingHelpers.Canonical(r.ExerciseType, AllowedValues.ExerciseTypeValues),
        Equipment = r.Equipment.Trim(),
        DifficultyLevel = MappingHelpers.Canonical(r.DifficultyLevel, AllowedValues.LevelValues),
        ContraindicatedAreas = MappingHelpers.Clean(r.ContraindicatedAreas),
        IsActive = r.IsActive
    };

    /// <summary>Copies the request onto a tracked entity.</summary>
    public static void Apply(this Exercise e, UpdateExerciseRequest r)
    {
        e.Name = r.Name.Trim();
        e.MuscleGroup = r.MuscleGroup.Trim();
        e.ExerciseType = MappingHelpers.Canonical(r.ExerciseType, AllowedValues.ExerciseTypeValues);
        e.Equipment = r.Equipment.Trim();
        e.DifficultyLevel = MappingHelpers.Canonical(r.DifficultyLevel, AllowedValues.LevelValues);
        e.ContraindicatedAreas = MappingHelpers.Clean(r.ContraindicatedAreas);
        e.IsActive = r.IsActive;
    }
}
