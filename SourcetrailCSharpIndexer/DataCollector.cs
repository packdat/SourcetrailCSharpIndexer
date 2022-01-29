using CoatiSoftware.SourcetrailDB;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Threading;
using SymbolKind = CoatiSoftware.SourcetrailDB.SymbolKind;

namespace SourcetrailCSharpIndexer
{
    /// <summary>
    /// Responsible for storing data in the sourcetrail-db
    /// </summary>
    internal class DataCollector : IDisposable
    {
        // names of symbols (types, methods, etc.) with their symbolId
        private readonly Dictionary<string, int> collectedSymbols = new();

        private readonly Dictionary<string, int> collectedFiles = new();

        private readonly object dbSync = new();     // sourcetraildb does not seeem to be thread-safe

        // statistics
        internal long NumFiles;
        internal long NumSymbols;
        internal long NumReferences;
        internal long NumErrors;

        public DataCollector(bool clearDatabase = false)
        {
            var outputFileName = Globals.Settings.OutputPath;
            if (string.IsNullOrWhiteSpace(outputFileName))
                throw new ArgumentException("A valid filename is required for the sourcetrail database",
                                            nameof(outputFileName));

            Locked(() =>
            {
                sourcetraildb.open(outputFileName);
                if (clearDatabase)
                    sourcetraildb.clear();
                return sourcetraildb.beginTransaction();
            });
        }

        public void Dispose()
        {
            Locked(() =>
            {
                sourcetraildb.commitTransaction();
                //sourcetraildb.optimizeDatabaseMemory();
                return sourcetraildb.close();
            });
        }

        public int CollectSymbol(string fullName, SymbolKind kind, string prefix = "", string postfix = "")
        {
            if (string.IsNullOrWhiteSpace(fullName))
                throw new ArgumentNullException(nameof(fullName),
                    "Symbol name may not be null or empty or consist only of whitespace characters");

            // caching the collected symbols drastically reduces execution time
            var identifier = prefix + fullName + postfix;
            if (collectedSymbols.TryGetValue(identifier, out int symbolId))
                return symbolId;

            var serializedName = NameHelper.SerializeName(fullName, prefix, postfix);
            symbolId = Locked(() => sourcetraildb.recordSymbol(serializedName));
            collectedSymbols[identifier] = symbolId;
            if (symbolId <= 0)
            {
                var err = Locked(() => sourcetraildb.getLastError());
                throw new InvalidOperationException("Sourcetrail DB error: " + err);
            }
            Locked(() => sourcetraildb.recordSymbolDefinitionKind(symbolId, DefinitionKind.DEFINITION_EXPLICIT));
            Locked(() => sourcetraildb.recordSymbolKind(symbolId, kind));
            Interlocked.Increment(ref NumSymbols);
            return symbolId;
        }

        public void CollectSymbolLocation(int symbolId, Location location)
        {
            var fileId = CollectFile(location.SourceTree.FilePath, Globals.FileLanguage);
            var span = location.GetLineSpan();
            Locked(() => sourcetraildb.recordSymbolLocation(symbolId, fileId,
                span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1, span.EndLinePosition.Character));
        }

        public void CollectSymbolSignatureLocation(int symbolId, Location location)
        {
            var fileId = CollectFile(location.SourceTree.FilePath, Globals.FileLanguage);
            var span = location.GetLineSpan();
            Locked(() => sourcetraildb.recordSymbolSignatureLocation(symbolId, fileId, 
                span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1, 
                span.EndLinePosition.Line + 1, span.EndLinePosition.Character));
        }

        public void CollectError(string message, bool fatal, string fileName, Location location)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("File name cannot be null, empty or whitespace", nameof(fileName));
            var fileId = CollectFile(fileName, Globals.FileLanguage);
            var span = location.GetLineSpan();
            Locked(() => sourcetraildb.recordError(message, fatal, fileId,
                span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1, span.EndLinePosition.Character));
            Interlocked.Increment(ref NumErrors);
        }

        public int CollectReference(int sourceSymbolId, int referenceSymbolId, ReferenceKind referenceKind)
        {
            if (sourceSymbolId <= 0 || referenceSymbolId <= 0)
                throw new ArgumentException("A symbol-id must be greater than zero");

            Interlocked.Increment(ref NumReferences);
            return Locked(() => sourcetraildb.recordReference(sourceSymbolId, referenceSymbolId, referenceKind));
        }

        public void MarkReferenceAsAmbiguous(int referenceId)
        {
            Locked(() => sourcetraildb.recordReferenceIsAmbiguous(referenceId));
        }

        public int CollectFile(string filename, string language)
        {
            if (string.IsNullOrWhiteSpace(filename))
                throw new ArgumentNullException(nameof(filename));

            if (collectedFiles.TryGetValue(filename, out int fileId))
                return fileId;
            fileId = Locked(() => sourcetraildb.recordFile(filename));
            Locked(() => sourcetraildb.recordFileLanguage(fileId, language));
            collectedFiles[filename] = fileId;
            Interlocked.Increment(ref NumFiles);
            return fileId;
        }

        public void CollectReferenceLocation(int referenceId, Location location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));
            var fileId = CollectFile(location.SourceTree.FilePath, Globals.FileLanguage);
            var span = location.GetLineSpan();
            CollectReferenceLocation(referenceId, fileId, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1, span.EndLinePosition.Character);
        }

        public void CollectReferenceLocation(int referenceId, int fileId, int startLine, int startColumn, int endLine, int endColumn)
        {
            if (referenceId <= 0)
                throw new ArgumentException("Reference id must be greater than zero", nameof(referenceId));
            if (fileId <= 0)
                throw new ArgumentException("File id must be greater than zero", nameof(fileId));

            Locked(() => sourcetraildb.recordReferenceLocation(referenceId, fileId, startLine, startColumn, endLine, endColumn));
        }

        public void CollectQualifierLocation(int referencedSymbolId, Location location)
        {
            if (location == null)
                throw new ArgumentNullException(nameof(location));
            var fileId = CollectFile(location.SourceTree.FilePath, Globals.FileLanguage);
            var span = location.GetLineSpan();
            CollectQualifierLocation(referencedSymbolId, fileId, span.StartLinePosition.Line + 1, span.StartLinePosition.Character + 1,
                span.EndLinePosition.Line + 1, span.EndLinePosition.Character);
        }

        public void CollectQualifierLocation(int referencedSymbolId, int fileId, int startLine, int startColumn, int endLine, int endColumn)
        {
            if (referencedSymbolId <= 0)
                throw new ArgumentException("Symbol id must be greater than zero", nameof(referencedSymbolId));
            if (fileId <= 0)
                throw new ArgumentException("File id must be greater than zero", nameof(fileId));

            Locked(() => sourcetraildb.recordQualifierLocation(referencedSymbolId, fileId, startLine, startColumn, endLine, endColumn));
        }

        private T Locked<T>(Func<T> action)
        {
            lock (dbSync)
            {
                return action();
            }
        }
    }
}
