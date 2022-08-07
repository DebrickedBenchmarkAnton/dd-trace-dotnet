// <copyright file="LineProbeResolver.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Datadog.Trace.Debugger.Configurations;
using Datadog.Trace.Debugger.Configurations.Models;
using Datadog.Trace.Debugger.Models;
using Datadog.Trace.ExtensionMethods;
using Datadog.Trace.Logging;
using Datadog.Trace.Pdb;
using Datadog.Trace.Vendors.dnlib.DotNet.Pdb.Symbols;

namespace Datadog.Trace.Debugger
{
    internal class LineProbeResolver : ILineProbeResolver
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor<LineProbeResolver>();

        private readonly object _locker;
        private readonly Dictionary<Assembly, FilePathLookup> _loadedAssemblies;

        private LineProbeResolver()
        {
            _locker = new object();
            _loadedAssemblies = new Dictionary<Assembly, FilePathLookup>();
        }

        public static LineProbeResolver Create()
        {
            return new LineProbeResolver();
        }

        private static IList<SymbolDocument> GetDocumentsFromPDB(Assembly loadedAssembly)
        {
            try
            {
                if (loadedAssembly.IsDynamic ||
                    loadedAssembly.ManifestModule.IsResource() ||
                    string.IsNullOrWhiteSpace(loadedAssembly.Location) ||
                    IsThirdPartyCode(loadedAssembly))
                {
                    return null;
                }

                using var reader = DatadogPdbReader.CreatePdbReader(loadedAssembly);
                if (reader != null)
                {
                    return reader.GetDocuments();
                }
            }
            catch (Exception e)
            {
                Log.Warning(e, "Failed to retrieve documents from PDB for {AssemblyLocation}", loadedAssembly.Location);
            }

            return null;
        }

        private static bool IsThirdPartyCode(Assembly loadedAssembly)
        {
            // This implementation is just a stub - we will need to replace it
            // with a proper implementation in the future.
            string[] thirdPartyStartsWith = { "Microsoft", "System" };

            var assemblyName = loadedAssembly.GetName().Name;
            return thirdPartyStartsWith.Any(t => assemblyName.StartsWith(t));
        }

        private FilePathLookup GetSourceFilePathForAssembly(Assembly loadedAssembly)
        {
            if (_loadedAssemblies.TryGetValue(loadedAssembly, out var trie))
            {
                return trie;
            }

            return _loadedAssemblies[loadedAssembly] = CreateLookupForSourceFilePaths(loadedAssembly);
        }

        private FilePathLookup CreateLookupForSourceFilePaths(Assembly loadedAssembly)
        {
            var documents = GetDocumentsFromPDB(loadedAssembly);
            if (documents == null)
            {
                return null; // No PDB available or unsupported assembly
            }

            var lookup = new FilePathLookup();
            foreach (var symbolDocument in documents)
            {
                lookup.InsertPath(symbolDocument.URL);
            }

            return lookup;
        }

        private bool TryFindAssemblyContainingFile(string probeFilePath, out string sourceFileFullPathFromPdb, out Assembly assembly)
        {
            lock (_locker)
            {
                foreach (var candidateAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    var lookup = GetSourceFilePathForAssembly(candidateAssembly);
                    if (lookup == null)
                    {
                        continue;
                    }

                    var path = lookup.FindPathThatEndsWith(probeFilePath);
                    if (path != null)
                    {
                        sourceFileFullPathFromPdb = path;
                        assembly = candidateAssembly;
                        return true;
                    }
                }
            }

            sourceFileFullPathFromPdb = null;
            assembly = null;
            return false;
        }

        public LineProbeResolveResult TryResolveLineProbe(ProbeDefinition probe, out BoundLineProbeLocation location)
        {
            location = null;
            if (!TryFindAssemblyContainingFile(probe.Where.SourceFile, out var filePathFromPdb, out var assembly))
            {
                return new LineProbeResolveResult(LiveProbeResolveStatus.Unbound, "Source file location for probe was not found, possibly because the relevant assembly was not yet loaded.");
            }

            using var pdbReader = DatadogPdbReader.CreatePdbReader(assembly);
            if (pdbReader == null)
            {
                return new LineProbeResolveResult(LiveProbeResolveStatus.Error, $"Failed to read from PDB");
            }

            if (probe.Where.Lines?.Length != 1 || !int.TryParse(probe.Where.Lines[0], out var lineNum))
            {
                return new LineProbeResolveResult(LiveProbeResolveStatus.Error, $"Failed to parse line number.");
            }

            var method = pdbReader.GetContainingMethodAndOffset(filePathFromPdb, lineNum, column: null, out var bytecodeOffset);
            if (bytecodeOffset.HasValue == false)
            {
                return new LineProbeResolveResult(LiveProbeResolveStatus.Error, "Probe location did not map out to a valid bytecode offset");
            }

            location = new BoundLineProbeLocation(probe, assembly.ManifestModule.ModuleVersionId, method.Token, bytecodeOffset.Value, lineNum);
            return new LineProbeResolveResult(LiveProbeResolveStatus.Bound);
        }

        public void OnDomainUnloaded()
        {
            lock (_locker)
            {
                foreach (var unloadedAssembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    _loadedAssemblies.Remove(unloadedAssembly);
                }
            }
        }

        internal class FilePathLookup
        {
            private static readonly string[] DirectorySeparatorsCrossPlatform = { @"\", @"/" };
            private readonly Trie _trie = new();
            private string _directoryPathSeparator;

            public void InsertPath(string path)
            {
                // Note: We purposefully are not supporting the case where the inserted paths contains a mix of both forward and back slashes (e.g. "c:\test\some/path/file.cs")
                // This should be fine because the inserted paths are generated by the compiler, not by humans, so we can assume that they are all consistent.
                _directoryPathSeparator ??= DirectorySeparatorsCrossPlatform.FirstOrDefault(path.Contains);
                _trie.Insert(GetReversePath(path));
            }

            public string FindPathThatEndsWith(string path)
            {
                // The trie is built from the PDB, so it will contains absolute paths such as:
                //      "D:\build_agent\yada\yada\src\MyProject\MyFile.cs",
                // ...whereas probeFilePath will typically be a path within the Git repo, such as:
                //      "src/MyProject/MyFile.cs"
                // Because we're actually interested in matching the ending of the string rather than the beginning,
                // we reverse all strings before inserting or querying the trie, and then reverse again when we get the string out.
                var reversePath = GetReversePath(path);
                var match = _trie.GetStringStartingWith(reversePath);
                return match != null ? GetReversePath(match) : null;
            }

            private string GetReversePath(string documentFullPath)
            {
                var partsReverse = documentFullPath.Split(DirectorySeparatorsCrossPlatform, StringSplitOptions.None).Reverse();
                // Preserve the type of slash (back- or forward- slash) that was originally inserted.
                return string.Join(_directoryPathSeparator ?? Path.DirectorySeparatorChar.ToString(), partsReverse);
            }
        }
    }
}
