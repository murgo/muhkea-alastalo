using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Alastalo
{
    /// <summary>
    /// Calculates the amount of unique words in all word pairs in text file.
    /// 
    /// Algorithm:
    /// 1) Read file, get distinct words, only allowed characters
    /// 2) "Hash" each word ('a' -> 0001, 'b' -> 0010, 'ab' --> 0011 etc.)
    /// 3) Create list of only best words/hashes (0011 over 0010 or 0001)
    /// 4) Compare all pairs of best hashes (muhkeus score is "hash1 | hash2")
    /// 5) ???
    /// 6) Profit (or drone)
    /// </summary>
    class MuhkeusCalculator
    {
        private static readonly char[] AllowedLetters = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', 'å', 'ä', 'ö' };

        /// <summary>
        /// Calculates the best word pairs in the file with the given path.
        /// </summary>
        public void Solve(string path)
        {
            Console.WriteLine("Reading file {0}...", path);
            var words = ReadFile(path);

            Console.WriteLine("Creating hashes...");
            var allHashes = BuildHashes(words);

            Console.WriteLine("Picking best candidates...");
            var bestHashes = GetBestHashes(allHashes.Keys.ToList());

            Console.WriteLine("Checking pairs...");
            var results = CheckPairs(bestHashes);

            PrintResults(results, allHashes);
        }

        /// <summary>
        /// Reads the file, and removes duplicate words.
        /// </summary>
        private List<string> ReadFile(string filename)
        {
            var book = File.ReadAllText(filename);
            var words = book.Split(new[] { ' ', '\t', '\r', '\n', '.', ',', ':', ';' }, StringSplitOptions.RemoveEmptyEntries);
            Console.WriteLine("Word count: {0}", words.Length);

            var unique = words.Select(s => s.ToLowerInvariant()).Distinct().ToList();
            Console.WriteLine("Unique word count: {0}", unique.Count);

            return unique;
        }

        /// <summary>
        /// Builds the list of hashes from the list of words.
        /// </summary>
        private Dictionary<uint, List<string>> BuildHashes(IEnumerable<string> words)
        {
            var allHashes = new Dictionary<uint, List<string>>();
            foreach (var word in words)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var c in word)
                {
                    if (AllowedLetters.Contains(c))
                    {
                        sb.Append(c);
                    }
                }

                var strippedWord = sb.ToString();
                var hash = CalculateHash(strippedWord);

                if (!allHashes.ContainsKey(hash)) allHashes.Add(hash, new List<string>());
                allHashes[hash].Add(strippedWord);
            }

            Console.WriteLine("Unique hash count (amount of unique simplified words, \"lola\" -> \"alo\"): {0}", allHashes.Count);
            return allHashes;
        }

        /// <summary>
        /// Gets the hashes of best candidates.
        /// All the bad hashes are discarded based on the size of the hash. (0011 is worse than 0111).
        /// </summary>
        private List<uint> GetBestHashes(List<uint> hashList)
        {
            // create list consisting of best unique hashes, "hei" is better than "ei" (0111 is better than 0011)
            var bestHashes = new List<uint>();
            foreach (var hash in hashList)
            {
                var newIsBetter = true;
                for (int i = 0; i < bestHashes.Count; i++)
                {
                    var hashToCompare = bestHashes[i];
                    var tmp = hashToCompare & hash;
                    if (tmp != hashToCompare && tmp != hash) continue;

                    // Clashing hash already exists in list (e.g. 0111, 0110)
                    if (hash > hashToCompare)
                    {
                        // New word is better than the old one, discard old
                        bestHashes.RemoveAt(i);
                        i--;
                    }
                    else
                    {
                        // Old is better, don't add new
                        newIsBetter = false;
                        break;
                    }
                }

                if (newIsBetter)
                    bestHashes.Add(hash);
            }

            Console.WriteLine("Best candidate count (\"hei\" is better than \"ei\"): {0}", bestHashes.Count);
            return bestHashes;
        }

        /// <summary>
        /// Combines the hashes and finds best scoring pairs.
        /// </summary>
        /// <returns>Dictionary with hash as the key, and list of hashes of words that make up the key.</returns>
        private Dictionary<uint, List<Tuple<uint, uint>>> CheckPairs(IReadOnlyList<uint> uniqueHashes)
        {
            var results = new Dictionary<uint, List<Tuple<uint, uint>>>();
            for (int i = 0; i < uniqueHashes.Count - 1; i++)
            {
                for (int j = i + 1; j < uniqueHashes.Count; j++)
                {
                    var hash1 = uniqueHashes[i];
                    var hash2 = uniqueHashes[j];
                    var combined = hash1 | hash2;
                    if (!results.ContainsKey(combined))
                    {
                        results.Add(combined, new List<Tuple<uint, uint>>());
                    }
                    results[combined].Add(Tuple.Create(hash1, hash2));
                }
            }
            return results;
        }

        private void PrintResults(Dictionary<uint, List<Tuple<uint, uint>>> results, Dictionary<uint, List<string>> allHashes)
        {
            var ordered = results.Select(kvp => new { Score = CountBits(kvp.Key), WordHashes = kvp.Value }).OrderByDescending(k => k.Score).GroupBy(k => k.Score).ToList();
            var allBest = ordered.First();

            Console.WriteLine();
            Console.WriteLine("Best results: ");
            var allWords = new List<string>();
            foreach (var tuple in allBest.SelectMany(result => result.WordHashes))
            {
                allWords.AddRange(allHashes[tuple.Item1].SelectMany(w1 => allHashes[tuple.Item2], (w1, w2) => string.Format("{0} {1}", w1, w2)));
            }
            Console.WriteLine("Score: {0}, Words: {1}", allBest.Key, string.Join(", ", allWords));
            Console.WriteLine();
        }

        /// <summary>
        /// Calculates the hash.
        /// Hashing logic: 
        /// if the word contains letter 'a', hash will be 00000001, 
        /// if it contains 'b' -> 00000010
        /// 'dab' -> 00001011 etc.
        /// </summary>
        private uint CalculateHash(string word)
        {
            uint hash = 0;
            for (int index = 0; index < AllowedLetters.Length; index++)
            {
                var c = AllowedLetters[index];
                if (word.Contains(c))
                {
                    hash |= (uint)1 << index;
                }
            }
            return hash;
        }

        /// <summary>
        /// Counts the number of ones in the binary representation of the word.
        /// </summary>
        private int CountBits(uint u)
        {
            int count = 0;
            while (u > 0)
            {
                count++;
                u &= u - 1;
            }
            return count;
        }
    }

    /// <summary>
    /// Entry point class.
    /// </summary>
    public static class Program
    {
        /// <summary>
        /// Entry point.
        /// </summary>
        public static void Main(string[] args)
        {
            var startTime = DateTime.Now;
            var path = args.Length > 0 ? args[0] : "alastalon_salissa.txt";
            if (!File.Exists(path))
            {
                Console.WriteLine("USAGE: Alastalo.exe path_to_txt");
                return;
            }

            var solver = new MuhkeusCalculator();
            solver.Solve(path);

            Console.WriteLine("Elapsed time: {0}", (DateTime.Now - startTime));
            Console.WriteLine("Press enter to continue...");
            Console.ReadLine();
        }
    }
}
