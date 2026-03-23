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
                default:
                case SelectionType.Lower:
                    {
                        result.Append("< ");
                        break;
                    }
                case SelectionType.LowerEqual:
                    {
                        result.Append("<= ");
                        break;
                    }
                case SelectionType.Higher:
                    {
                        result.Append("> ");
                        break;
                    }
                case SelectionType.HigherEqual:
                    {
                        result.Append(">= ");
                        break;
                    }
            }

            if (Type == SelectionType.Exact)
            {
                result.Append(Major).Append('.').Append(Minor).Append('.').Append(Patch);
            }
            else // Range
            {
                result.Append('[').Append(Major).Append('.').Append(Minor).Append('.').Append(Patch).Append(" - ")
                .Append(MaxMajor).Append('.').Append(MaxMinor).Append('.').Append(MaxPatch).Append(']');
            }

            return result.ToString();
        }

        public new bool TryParse(string input)
        {
            input = input.Replace(" ", "");

            if (input.Contains('-'))
            {
                string[] versions = input.Split('-');

                if (versions.Length != 2)
                {
                    return false;
                }

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

        public bool InRange(ModVersion compared)
        {
            switch (Type)
            {
                case SelectionType.Exact:
                    {
                        return compared.Major == Major && compared.Minor == Minor && compared.Patch == Patch;
                    }
                case SelectionType.Lower:
                    {
                        if (compared.Major < Major)
                        {
                            return true;
                        }

                        if (compared.Minor < Minor)
                        {
                            return true;
                        }

                        if (compared.Patch < Patch)
                        {
                            return true;
                        }

                        return false;
                    }
                case SelectionType.LowerEqual:
                    {
                        if (compared.Major == Major && compared.Minor == Minor && compared.Patch == Patch)
                        {
                            return true;
                        }

                        if (compared.Major < Major)
                        {
                            return true;
                        }

                        if (compared.Minor < Minor)
                        {
                            return true;
                        }

                        if (compared.Patch < Patch)
                        {
                            return true;
                        }

                        return false;
                    }
                case SelectionType.Higher:
                    {
                        if (compared.Major > Major)
                        {
                            return true;
                        }

                        if (compared.Minor > Minor)
                        {
                            return true;
                        }

                        if (compared.Patch > Patch)
                        {
                            return true;
                        }

                        return false;
                    }
                case SelectionType.HigherEqual:
                    {
                        if (compared.Major == Major && compared.Minor == Minor && compared.Patch == Patch)
                        {
                            return true;
                        }

                        if (compared.Major > Major)
                        {
                            return true;
                        }

                        if (compared.Minor > Minor)
                        {
                            return true;
                        }

                        if (compared.Patch > Patch)
                        {
                            return true;
                        }

                        return false;
                    }
                case SelectionType.Range:
                    {
                        if (compared.Major < Major || compared.Major > MaxMajor)
                        {
                            return false;
                        }

                        if (compared.Minor < Minor || compared.Minor > MaxMinor)
                        {
                            return false;
                        }

                        if (compared.Patch < Patch || compared.Patch > MaxPatch)
                        {
                            return false;
                        }

                        return true;
                    }
                default:
                    return false;
            }
        }
    }
}
