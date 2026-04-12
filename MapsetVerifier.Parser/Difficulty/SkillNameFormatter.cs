using System.Reflection;
using System.Text;
using MapsetVerifier.Parser.Objects;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Osu.Difficulty.Skills;

namespace MapsetVerifier.Parser.Difficulty;

public static class SkillNameFormatter
{
    public static string GetSkillName(Skill skill, Beatmap beatmap)
    {
        if (beatmap.GeneralSettings.mode == Beatmap.Mode.Standard && skill is Aim aimSkill)
            return aimSkill.IncludeSliders ? "Aim (with sliders)" : "Aim (no sliders)";

        var skillType = skill.GetType();
        var baseName = GetBaseSkillName(skillType);
        var qualifiers = GetQualifiers(skill, skillType);

        return qualifiers.Count == 0 ? baseName : $"{baseName} ({string.Join(", ", qualifiers)})";
    }

    private static string GetBaseSkillName(Type skillType)
    {
        var namespaceParts = skillType.Namespace?.Split('.') ?? [];
        var skillsIndex = Array.LastIndexOf(namespaceParts, "Skills");
        var relevantNamespaceParts = skillsIndex >= 0 && skillsIndex < namespaceParts.Length - 1
            ? namespaceParts[(skillsIndex + 1)..]
            : [];

        if (relevantNamespaceParts.Length > 0 && relevantNamespaceParts[^1] == skillType.Name)
            relevantNamespaceParts = relevantNamespaceParts[..^1];

        var nameParts = relevantNamespaceParts
            .Append(skillType.Name)
            .Select(HumanizeIdentifier)
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .ToList();

        return nameParts.Count == 0 ? skillType.Name : string.Join(" ", nameParts);
    }

    private static List<string> GetQualifiers(Skill skill, Type skillType)
    {
        var qualifiers = new List<string>();

        foreach (var field in skillType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            AppendQualifier(qualifiers, field.Name, field.FieldType, field.GetValue(skill));

        foreach (var property in skillType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                     .Where(property => property.CanRead && property.GetIndexParameters().Length == 0))
            AppendQualifier(qualifiers, property.Name, property.PropertyType, property.GetValue(skill));

        return qualifiers;
    }

    private static void AppendQualifier(List<string> qualifiers, string memberName, Type memberType, object? value)
    {
        if (value == null)
            return;

        if (memberType == typeof(bool))
        {
            if ((bool)value)
                qualifiers.Add(HumanizeIdentifier(memberName));

            return;
        }

        if (memberType.IsEnum)
            qualifiers.Add($"{HumanizeIdentifier(memberName)}: {HumanizeIdentifier(value.ToString() ?? string.Empty)}");
    }

    private static string HumanizeIdentifier(string identifier)
    {
        if (string.IsNullOrWhiteSpace(identifier))
            return string.Empty;

        var normalized = identifier.EndsWith("Skill", StringComparison.Ordinal) && identifier.Length > "Skill".Length
            ? identifier[..^"Skill".Length]
            : identifier;

        var builder = new StringBuilder(normalized.Length * 2);

        for (var index = 0; index < normalized.Length; index++)
        {
            var current = normalized[index];

            if (current == '_')
            {
                builder.Append(' ');
                continue;
            }

            if (index > 0 && char.IsUpper(current) &&
                (char.IsLower(normalized[index - 1]) || (index + 1 < normalized.Length && char.IsLower(normalized[index + 1]))))
            {
                builder.Append(' ');
            }

            builder.Append(current);
        }

        return builder.ToString().Trim();
    }
}