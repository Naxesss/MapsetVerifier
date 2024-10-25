using System.Collections.Generic;
using MapsetVerifier.Framework.Objects.Metadata;

namespace MapsetVerifier.Framework.Objects
{
    public abstract class Check
    {
        public IssueTemplate GetTemplate(string template) => GetTemplates()[template];

        public abstract Dictionary<string, IssueTemplate> GetTemplates();
        public abstract CheckMetadata GetMetadata();

        public override string ToString() => GetMetadata().Message;
    }
}