using MapsetVerifier.Parser.Statics;

namespace MapsetVerifier.Parser.Objects.Events
{
    public class Video
    {
        // Video,-320,"aragoto.avi"
        // Video, offset, filename

        public readonly int offset;
        public readonly string path;

        /// <summary> The path in lowercase without extension or quotationmarks. </summary>
        public readonly string strippedPath;

        public Video(string[] args)
        {
            offset = GetOffset(args);
            path = GetPath(args);

            strippedPath = PathStatic.ParsePath(path, true);
        }

        /// <summary> Returns the temporal offset of the video (i.e. when it should start playing). </summary>
        private int GetOffset(string[] args) => int.Parse(args[1]);

        /// <summary> Returns the file path which this video uses. Retains case and extension. </summary>
        private string GetPath(string[] args) => PathStatic.ParsePath(args[2], retainCase: true);
    }
}