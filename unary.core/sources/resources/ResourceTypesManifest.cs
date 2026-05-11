namespace Unary.Core
{
    public class ResourceTypesManifest
    {
        public const string Extension = ".types";
        public string ModId = string.Empty;
        public string[] Paths { get; set; }
        public string[] Types { get; set; }
    }
}
