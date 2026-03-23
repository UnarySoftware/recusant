using System.Text;

namespace Unary.Core
{
    public class ModVersionSelector : ModVersion
    {
        public enum SelectionType
        {
            Exact,
            Lower,
            LowerEqual,
            Higher,
            HigherEqual,
            Range
        }

        public SelectionType Type = SelectionType.Exact;

        public int MaxMajor = 0;
        public int MaxMinor = 0;
        public int MaxPatch = 0;

        public ModVersionSelector()
        {

        }

        public override string ToString()
        {
            StringBuilder result = new();

            switch (Type)
            {
                case SelectionType.Exact:
                    result.Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
                    break;
                case SelectionType.Lower:
                    result.Append("< ").Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
                    break;
                case SelectionType.LowerEqual:
                    result.Append("<= ").Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
                    break;
                case SelectionType.Higher:
                    result.Append("> ").Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
                    break;
                case SelectionType.HigherEqual:
                    result.Append(">= ").Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
                    break;
                case SelectionType.Range:
                    result.Append('[').Append(Major).Append('.').Append(Minor).Append('.').Append(Patch)
                        .Append(" - ").Append(MaxMajor).Append('.').Append(MaxMinor).Append('.').Append(MaxPatch).Append(']');
                    break;
            }

            return result.ToString();
        }

        public new bool TryParse(string input)
        {
            input = input.Replace(" ", "");

            if (input.StartsWith("<="))
            {
                Type = SelectionType.LowerEqual;
                input = input[2..];
            }
            else if (input.StartsWith(">="))
            {
                Type = SelectionType.HigherEqual;
                input = input[2..];
            }
            else if (input.StartsWith('<'))
            {
                Type = SelectionType.Lower;
                input = input[1..];
            }
            else if (input.StartsWith('>'))
            {
                Type = SelectionType.Higher;
                input = input[1..];
            }

            if (input.Contains('-'))
            {
                string[] versions = input.Split('-');

                if (versions.Length != 2)
                {
                    return false;
                }

                Type = SelectionType.Range;

                if (!ParseVersion(versions[0], out Major, out Minor, out Patch))
                {
                    return false;
                }

                if (!ParseVersion(versions[1], out MaxMajor, out MaxMinor, out MaxPatch))
                {
                    return false;
                }
            }
            else
            {
                if (!ParseVersion(input, out Major, out Minor, out Patch))
                {
                    return false;
                }
            }

            return true;
        }

        private static int Compare(ModVersion a, int major, int minor, int patch)
        {
            if (a.Major != major) return a.Major.CompareTo(major);
            if (a.Minor != minor) return a.Minor.CompareTo(minor);
            return a.Patch.CompareTo(patch);
        }

        public bool InRange(ModVersion compared)
        {
            return Type switch
            {
                SelectionType.Exact => Compare(compared, Major, Minor, Patch) == 0,
                SelectionType.Lower => Compare(compared, Major, Minor, Patch) < 0,
                SelectionType.LowerEqual => Compare(compared, Major, Minor, Patch) <= 0,
                SelectionType.Higher => Compare(compared, Major, Minor, Patch) > 0,
                SelectionType.HigherEqual => Compare(compared, Major, Minor, Patch) >= 0,
                SelectionType.Range => Compare(compared, Major, Minor, Patch) >= 0
                                        && Compare(compared, MaxMajor, MaxMinor, MaxPatch) <= 0,
                _ => false,
            };
        }
    }
}
