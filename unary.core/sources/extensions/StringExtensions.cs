using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Godot;

namespace Unary.Core
{
    public static class StringExtensions
    {
        static int GetDeterministicHashCode(this string str)
        {
            unchecked
            {
                int hash1 = (5381 << 16) + 5381;
                int hash2 = hash1;

                for (int i = 0; i < str.Length; i += 2)
                {
                    hash1 = ((hash1 << 5) + hash1) ^ str[i];
                    if (i == str.Length - 1)
                    {
                        break;
                    }
                    hash2 = ((hash2 << 5) + hash2) ^ str[i + 1];
                }

                return hash1 + (hash2 * 1566083941);
            }
        }

        public static string ToHumanReadable(this string source)
        {
            return source.Trim('_').ToSnakeCase().Capitalize();
        }

        public static Guid ToGuid(this string source)
        {
            if (Guid.TryParse(source, out Guid result))
            {
                return result;
            }
            return new();
        }

        private static readonly Dictionary<char, char> _sceneTreeRemap = new()
        {
            { '.', '\u2024' },
            { '"', '\u2033' },
            { '%', '\uff05' },
            { ':', '\uff1a' },
        };

        private static readonly StringBuilder _stringBuilder = new();

        public static string FilterTreeName(this string source)
        {
            _stringBuilder.Clear();

            foreach (var targetChar in source)
            {
                if (_sceneTreeRemap.TryGetValue(targetChar, out var result))
                {
                    _stringBuilder.Append(result);
                }
                else
                {
                    _stringBuilder.Append(targetChar);
                }
            }

            return _stringBuilder.ToString();
        }

        public static string GetScriptType(this string resourcePath)
        {
            resourcePath = resourcePath.Replace("res://", "");

            if (!File.Exists(resourcePath))
            {
                return string.Empty;
            }

            string header;
            string key = "script_class=\"";

            using (StreamReader reader = new(resourcePath))
            {
                header = reader.ReadLine();
            }

            int keyStart = header.IndexOf(key);

            if (keyStart == -1)
            {
                return "Resource";
            }

            int valueStart = keyStart + key.Length;
            int valueEnd = header.IndexOf('\"', valueStart);

            if (valueEnd == -1)
            {
                return "Resource";
            }

            return header.Substring(valueStart, valueEnd - valueStart);
        }

        private static readonly string[] _audibleHashes =
        [
            "dog","cat","bird","fish","cow","pig","horse","sheep","goat","frog","bear","lion","tiger",
            "fox","wolf","deer","mouse","rat","ape","monkey","camel","goat","seal","whale","shark","crab",
            "snail","beetle","tree","plant","flower","grass","rock","soil","bread","butter","cheese","milk",
            "egg","meat","fish","rice","corn","potato","apple","banana","orange","grape","peach","berry","tomato",
            "onion","carrot","pepper","soup","cake","salt","sugar","tea","coffee","juice","water","breadstick","yogurt",
            "house","home","room","door","window","chair","table","bed","lamp","rug","shelf","stove","sink","towel",
            "mirror","clock","book","lamp","computer","phone","bag","chair","bench","cup","plate","spoon","fork","knife",
            "fridge","closet","garden","broom","soap","soap","towel","television","lamp","sofa","man","woman","child","friend",
            "teacher","student","parent","sister","brother","boy","girl","doctor","nurse","chef","lawyer","driver","painter",
            "singer","actor","farmer","farmer","pilot","editor","driver","waiter","manager","nurse","engineer","scientist",
            "clerk","police","guard","shirt","pants","coat","hat","shoe","sock","dress","coat","jacket","belt","scarf","glove",
            "ring","bag","cap","sandal","car","bus","train","bike","plane","boat","ship","driver","camera","phone","computer",
            "engine","wheel","road","map","light","rocket","cable","book","lesson","test","note","idea","fact","theory","art",
            "music","math","science","history","language","word","sentence","paragraph","quiz","homework","library","pencil",
            "eraser","desk","graph","table","love","fear","joy","pride","hope","envy","calm","thrill","pain","relief","anger",
            "smile","laugh","dream","sleep","breath","hype","day","night","morning","evening","year","week","moment","second",
            "hour","sun","rain","snow","wind","cloud","storm","light","dark","space","place","scene","level","town","city",
            "country","world","road","street","park","mall","water","fire","stone","metal","glass","wood","paper","plastic",
            "pill","hair","skin","blood","muscle","bone","breath","seed","leaf","root","sky","moon","sun","star","rain","tide"
        ];

        public static string GetAudibleHash(this string input)
        {
            byte[] code = BitConverter.GetBytes(input.GetDeterministicHashCode());
            return _audibleHashes[code[3]];
        }
    }
}
