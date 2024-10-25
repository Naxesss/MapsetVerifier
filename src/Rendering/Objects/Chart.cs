using System.Collections.Generic;

namespace MapsetVerifier.Rendering.Objects
{
    public class Chart<T>
    {
        public Chart(string title, List<T> data = null)
        {
            Title = title;
            Data = data ?? new List<T>();
        }

        public string Title { get; }
        public List<T> Data { get; private set; }
    }
}