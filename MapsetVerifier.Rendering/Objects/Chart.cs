using System.Collections.Generic;

namespace MapsetVerifier.Rendering.Objects
{
    public class Chart<T>(string title, List<T> data)
    {
        public string Title { get; } = title;
        public List<T> Data { get; private set; } = data;
    }
}