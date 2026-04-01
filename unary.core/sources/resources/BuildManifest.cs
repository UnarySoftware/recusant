namespace Unary.Core
{
    public class BuildManifest
    {
        public const string Extension = ".build";
        public ulong BuildNumber { get; set; } = 0;
        public string BuildData { get; set; } = "Unknown";
    }
}
