using System;

namespace MapsetVerifier.Framework.Objects.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class CheckAttribute : Attribute
    {
        // Used to identify which classes to add to checks in plugins.
    }
}