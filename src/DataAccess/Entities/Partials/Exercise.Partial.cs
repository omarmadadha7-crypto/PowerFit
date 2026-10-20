using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace DataAccess.Entities;

public partial class Exercise
{
    public const string NoEquipment = "None";

    public Exercise()
    {
        Equipment = NoEquipment;
        IsActive = true;
    }

    [NotMapped]
    public bool RequiresEquipment =>
        !string.Equals(Equipment, NoEquipment, StringComparison.OrdinalIgnoreCase);

    [NotMapped] public IReadOnlyList<string> ContraindicatedAreaList => TextList.Split(ContraindicatedAreas);
    [NotMapped] public int DifficultyRank => DifficultyLevels.Rank(DifficultyLevel);

    /// <summary>
    /// False when any injured area overlaps with a contraindicated area. Areas are compared by meaning first
    /// (see <see cref="BodyAreas"/>: "الركبة" == "knee"), then as plain text for terms the dictionary does not know.
    /// </summary>
    public bool IsSafeFor(IEnumerable<string>? injuredAreas)
    {
        if (injuredAreas is null) return true;

        var contra = ContraindicatedAreaList;
        if (contra.Count == 0) return true;

        var contraAreas = BodyAreas.ResolveAll(contra);

        foreach (var injury in injuredAreas)
        {
            if (string.IsNullOrWhiteSpace(injury)) continue;

            var inj = injury.Trim();

            if (contraAreas.Count > 0 && contraAreas.Overlaps(BodyAreas.Resolve(inj)))
                return false;

            if (contra.Any(c =>
                    c.Contains(inj, StringComparison.OrdinalIgnoreCase) ||
                    inj.Contains(c, StringComparison.OrdinalIgnoreCase)))
                return false;
        }

        return true;
    }
}
