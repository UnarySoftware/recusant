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

        private static readonly string[] _hashNouns =
        [
            "dog","cat","bird","fish","cow","pig","horse","sheep","goat","frog","bear","lion","tiger",
            "fox","wolf","deer","mouse","rat","ape","monkey","camel","elk","seal","whale","shark","crab",
            "snail","beetle","tree","plant","flower","grass","rock","soil","bread","butter","cheese","milk",
            "egg","meat","tuna","rice","corn","potato","apple","banana","orange","grape","peach","berry","tomato",
            "onion","carrot","pepper","soup","cake","salt","sugar","tea","coffee","juice","water","breadstick","yogurt",
            "house","home","room","door","window","chair","table","bed","lamp","rug","shelf","stove","sink","towel",
            "mirror","clock","book","vase","computer","phone","bag","stool","bench","cup","plate","spoon","fork","knife",
            "fridge","closet","garden","broom","soap","brush","sponge","mat","sofa","man","woman","child","friend",
            "teacher","student","parent","sister","brother","boy","girl","doctor","nurse","chef","lawyer","driver","painter",
            "singer","actor","farmer","baker","pilot","editor","miner","waiter","manager","medic","engineer","scientist",
            "clerk","police","guard","shirt","pants","coat","hat","shoe","sock","vest","jacket","belt","scarf","glove",
            "ring","pouch","cap","sandal","car","bus","train","bike","plane","boat","ship","mechanic","camera","radio","tablet",
            "engine","wheel","road","map","light","rocket","cable","scroll","lesson","test","note","idea","fact","theory","art",
            "music","math","science","history","word","quiz","library","pencil","eraser","desk","graph","chart","love","fear",
            "joy","pride","hope","envy","calm","thrill","pain","relief","anger","smile","laugh","dream","sleep","breath","hype",
            "day","night","morning","evening","year","week","moment","second","hour","sun","rain","snow","wind","cloud","storm",
            "glow","dark","space","place","scene","level","town","city","country","world","path","street","park","mall","river",
            "fire","stone","metal","glass","wood","paper","plastic","pill","hair","skin","blood","muscle","bone","pulse","seed",
            "leaf","root","sky","moon","comet","star","frost","tide"
        ];

        private static readonly string[] _hashAdjectives =
        [
            "bright","calm","happy","quick","silent","warm","friendly","gentle","brave","kind","smooth","loud","fancy","fresh",
            "golden","sweet","jolly","sharp","large","mild","nice","old","pink","quiet","rare","shiny","tall","urban","vast","cool",
            "young","zany","awesome","brisk","clear","deep","eager","swift","graceful","huge","icy","mellow","keen","light","merry",
            "neat","open","proud","lush","rich","silly","tasty","unique","vivid","soft","xenial","yellow","pure","active","pale",
            "clean","daring","elegant","slim","rosy","cheery","inventive","tame","fine","lively","flat","free","orange","peaceful",
            "wise","roaring","wild","trusty","stern","vibrant","trim","youthful","zealous","adorable","breezy","cheerful","dazzling",
            "sleek","famous","glossy","rugged","perky","snappy","plucky","luminous","mighty","nifty","spry","sturdy","savvy","radiant","simple",
            "tender","serene","scenic","witty","plush","wiry","silky","artful","brilliant","crisp","stark","stout","frosty","lucid",
            "handsome","ideal","jovial","kindly","lithe","supple","noble","ordinary","polished","agile","robust","zippy","nimble","lanky",
            "husky","earthy","rustic","minty","zesty","spicy","bold","smoky","delightful","cozy","snug","glad","mossy","sandy",
            "leafy","rocky","piney","fizzy","fluffy","fuzzy","gaudy","gutsy","handy","hardy","hasty","hefty","humid","inky","jumpy","juicy",
            "lofty","amazing","lowly","lucky","lumpy","macho","meek","messy","misty","muddy","musty","nippy","nutty","pasty","patchy",
            "punchy","quirky","regal","rowdy","ruddy","rusty","salty","sassy","scruffy","shaggy","slinky","slick","snazzy","soggy",
            "solid","somber","sooty","spunky","stale","steady","sticky","stony","stormy","subtle","svelte","brawny","burly","catchy",
            "chilly","chunky","classy","clever","cloudy","crafty","creamy","crunchy","curly","curvy","dainty","dusty","feisty","fiery",
            "flaky","flinty","foamy","folksy","frisky","frothy","funky","gloomy","grainy","grassy","gritty","groovy","gruff","grumpy","hazy",
            "herby","homey","jazzy","jaunty","lean","limber","loopy","lusty","murky","natty","nervy","oaken","peppy","pithy","plain",
            "plump","prime","reedy","seedy","shady","soapy","spiky","tangy","tawny"
        ];

        public static (string adjective, string noun) GetAudibleHash(this string input)
        {
            byte[] code = BitConverter.GetBytes(input.GetDeterministicHashCode());
            return (_hashAdjectives[code[3]], _hashNouns[code[2]]);
        }
    }
}
