using MapsetVerifier.Framework;
using MapsetVerifier.Framework.Objects;
using MapsetVerifier.Framework.Objects.Metadata;
using MapsetVerifier.Parser.Objects;
using MapsetVerifier.Server.Model;

namespace MapsetVerifier.Server.Service;

public static class DocumentationService
{
    public static IEnumerable<ApiDocumentationCheck> GetGeneralDocumentation()
    {
        var checks = CheckerRegistry.GetChecksWithId()
            .Where(check => check.Value is GeneralCheck);
        
        var result = checks.Select(check => {
            var metadata = check.Value.GetMetadata();
            return new ApiDocumentationCheck(
                id: check.Key,
                description: metadata.Message,
                category: metadata.Category,
                subCategory: metadata.GetMode(),
                author: metadata.Author,
                outcomes: check.Value.GetTemplates()
                    .Select(template => template.Value.Level)
            );
        });

        return result.OrderBy(check => check.Category)
            .ThenBy(check => check.Description);
    }
    
    public static IEnumerable<ApiDocumentationCheck> GetBeatmapDocumentation(Beatmap.Mode mode)
    {
        var gamemodeChecks = CheckerRegistry.GetChecksWithId()
            .Where(check =>
                check.Value is BeatmapCheck &&
                check.Value.GetMetadata() is BeatmapCheckMetadata metadata &&
                metadata.Modes.Contains(mode));

        var result = gamemodeChecks.Select(check => {
            var metadata = check.Value.GetMetadata();
            return new ApiDocumentationCheck(
                id: check.Key,
                description: metadata.Message,
                category: metadata.Category,
                subCategory: metadata.GetMode(),
                author: metadata.Author,
                outcomes: check.Value.GetTemplates()
                    .Select(template => template.Value.Level)
            );
        });

        return result.OrderBy(check => check.Category)
            .ThenBy(check => check.Description);
    }

    public static ApiDocumentationCheckDetails? GetDocumentationDetails(int checkId)
    {
        var check = CheckerRegistry.GetChecksWithId()
            .FirstOrDefault(check => check.Key == checkId)
            .Value;

        if (check?.GetMetadata() == null)
            return null;

        var templates = check.GetTemplates();
        var outcomes = templates.Select(template =>
        {
            var templateValue = template.Value;
            var templateMessage = templateValue.Format(
                templateValue.GetDefaultArguments()
                    .Select(arg => "`" + arg + "`")
                    .ToArray<object>()
            );
            
            return new ApiDocumentationCheckDetailsOutcome(
                level: templateValue.Level,
                description: templateMessage,
                cause: templateValue.Cause
            );
        }).ToList();
        
        var descriptions = check.GetMetadata().Documentation.Select(section =>
        {
            var value = section.Value;
            // Make the key a markdown h1 + some white space after that for the full description
            var formattedDescription = "# " + section.Key + "\n\n" + value;
            return formattedDescription;
        });
        var fullDescription = string.Join("\n\n", descriptions);
        // Remove all leading tabs and spaces from each line
        fullDescription = string.Join("\n", fullDescription
            .Split('\n')
            .Select(line => line.TrimStart()));

        return new ApiDocumentationCheckDetails(
            description: fullDescription,
            outcomes: outcomes
        );
    }
}