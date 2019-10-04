﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using static BenMcCallum.DotNet.FixReferences.Common;

namespace BenMcCallum.DotNet.FixReferences
{
    public static class FixLocationsOfProjectsProcessor
    {
        public static void Process(string slnFilePath, string currentWorkingDirectory, bool removeExtras)
        {
            var csProjFilePaths = Directory.GetFiles(currentWorkingDirectory, "*.csproj", SearchOption.AllDirectories);

            var csProjFilesProcessed = new HashSet<string>();

            var slnFileDirectoryPath = Path.GetDirectoryName(slnFilePath);
            var slnFileContents = File.ReadAllText(slnFilePath);
            var matches = SlnFileCsProjRegex.Matches(slnFileContents);
            foreach (Match match in matches)
            {
                ProcessCsProjFileMatch(match, slnFileDirectoryPath, csProjFilesProcessed, csProjFilePaths);
            }

            if (removeExtras)
            {
                var toDelete = csProjFilePaths.Where(fp => !csProjFilesProcessed.Contains(Path.GetFileName(fp))).ToList();
                toDelete.ForEach(fp => File.Delete(fp));
            }
        }

        private static void ProcessCsProjFileMatch(Match match, string rootPath, HashSet<string> csProjFilesProcessed, string[] csProjFilePaths)
        {
            var csProjFileName = ExtractCsProjName(match.Value);
            if (csProjFilesProcessed.Contains(csProjFileName))
            {
                return;
            }

            var csProjReferenceRelativePath = ExtractCsProjReferenceRelativePath(match.Value);

            // Find where it currently is
            var csProjFilePath = FindCsProjFilePath(csProjFilePaths, csProjFileName);

            // Determine where it should be moved to
            var newCsProjFilePath = Path.Combine(rootPath, csProjReferenceRelativePath);

            // Move it there instead, creating dirs as necessary
            Directory.CreateDirectory(newCsProjFilePath.Replace(csProjFileName, "").TrimEnd('/'));
            File.Move(csProjFilePath, newCsProjFilePath);

            // Mark that we've moved this one
            csProjFilesProcessed.Add(csProjFileName);

            // Process the contents of this file for any of its references
            ProcessCsProjFileContents(csProjFilesProcessed, csProjFilePaths, newCsProjFilePath);
        }

        private static void ProcessCsProjFileContents(HashSet<string> csProjFilesProcessed, string[] csProjFilePaths, string csProjFilePath)
        {
            var csProjFileDirectoryPath = Path.GetDirectoryName(csProjFilePath);
            var csProjFileContents = File.ReadAllText(csProjFilePath);
            var matches = CsProjRegex.Matches(csProjFileContents);
            foreach (Match match in matches)
            {
                ProcessCsProjFileMatch(match, csProjFileDirectoryPath, csProjFilesProcessed, csProjFilePaths);
            }
        }

        private static string ExtractCsProjReferenceRelativePath(string input)
        {
            var firstQuoteIndex = input.IndexOf('"');
            if (firstQuoteIndex > 0)
            {
                input = input.Substring(firstQuoteIndex + 1);
            }
            return input.TrimEnd('\"');
        }
    }
}