using System.Reflection;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Attributes;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.RankingCriteria;
using Xunit;

namespace MapsetVerifier.Checks.Tests;

/// <summary>
/// Guarantees that every <see cref="IssueTemplate.WithRule" /> link points at a ranking criteria statement which
/// exists and applies to the check's modes. Levels may differ from the statement's kind, e.g. a Problem
/// linking to a guideline.
/// </summary>
public class RankingCriteriaLinkTests
{
    private static IEnumerable<(Check Check, string Key, IssueTemplate Template)> Templates() =>
        typeof(Common)
            .Assembly.GetExportedTypes()
            .Where(type => type.GetCustomAttribute<CheckAttribute>() != null)
            .Select(type => (Check)Activator.CreateInstance(type)!)
            .SelectMany(check =>
                check.GetTemplates().Select(pair => (check, pair.Key, pair.Value))
            );

    private static string Name(Check check, string key) => $"{check.GetType().Name} \"{key}\"";

    [Fact]
    public void LinkedStatementsExist()
    {
        var problems = new List<string>();

        foreach (var (check, key, template) in Templates())
        foreach (var id in template.RuleIds)
        {
            if (!RcStore.Embedded.TryGetStatement(id, out _, out var statement))
                problems.Add($"{Name(check, key)} links to unknown statement {id}");
            else if (statement.IsRetired)
                problems.Add(
                    $"{Name(check, key)} links to {id}, which was removed from the ranking criteria"
                );
            else if (statement.Upstream.Intro)
                problems.Add(
                    $"{Name(check, key)} links to {id}, which only introduces its nested statements; link those instead"
                );
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }

    [Fact]
    public void LinkedStatementsApplyToTheCheckModes()
    {
        var problems = new List<string>();

        foreach (var (check, key, template) in Templates())
        {
            if (check.GetMetadata() is not BeatmapCheckMetadata metadata)
                continue;

            foreach (var id in template.RuleIds)
                if (
                    RcStore.Embedded.TryGetStatement(id, out var page, out _)
                    && !page.Modes.Intersect(metadata.Modes).Any()
                )
                    problems.Add(
                        $"{Name(check, key)} links to {id}, but the check does not run for {page.Title}"
                    );
        }

        Assert.True(problems.Count == 0, string.Join("\n", problems));
    }
}
