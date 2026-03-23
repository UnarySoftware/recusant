using System.Text;

namespace Unary.Core
{
    public class ModVersion
    {
        public int Major = 0;
        public int Minor = 0;
        public int Patch = 0;

        public ModVersion()
        {

        }

        protected static bool ParseVersion(string version, out int major, out int minor, out int patch)
        {
            string[] parts = version.Split('.');

            if (parts.Length == 3 &&
            int.TryParse(parts[0], out major) &&
            int.TryParse(parts[1], out minor) &&
            int.TryParse(parts[2], out patch))
            {
                return true;
            }

            major = 0;
            minor = 0;
            patch = 0;

            return false;
        }

        public override string ToString()
        {
            StringBuilder result = new();
            result.Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
            return result.ToString();
        }

        public bool TryParse(string input)
        {
            return ParseVersion(input, out Major, out Minor, out Patch);
        }
    }
}
