using System.Reflection;
using MapsetVerifier.Framework.Objects.Attributes;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

/// <summary>
/// Guarantees every [Check] class has a unique short name across the assembly.
/// Duplicate names (e.g. CheckSkinning in two mode folders) break metadata export
/// (which uses Type.Name) and make checks harder to reference unambiguously.
/// </summary>
public class CheckClassNameUniquenessTests
{
    [Fact]
    public void AllCheckClasses_HaveUniqueShortNames()
    {
        var checkTypes = typeof(Common)
            .Assembly.GetExportedTypes()
            .Where(type => type.GetCustomAttribute<CheckAttribute>() != null)
            .ToList();

        Assert.NotEmpty(checkTypes);

        var duplicates = checkTypes
            .GroupBy(type => type.Name)
            .Where(group => group.Count() > 1)
            .Select(group =>
                $"{group.Key}: {string.Join(", ", group.Select(type => type.FullName))}"
            )
            .OrderBy(message => message)
            .ToList();

        Assert.True(
            duplicates.Count == 0,
            "Check classes must have unique short names. Rename duplicates so the mode "
                + "appears in the class name (e.g. CheckSkinningStandard). Conflicts:\n"
                + string.Join("\n", duplicates)
        );
    }
}
