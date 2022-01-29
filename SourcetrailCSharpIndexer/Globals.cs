using Microsoft.CodeAnalysis.CSharp;
using SourcetrailCSharpIndexer.Analyzing;
using System;

namespace SourcetrailCSharpIndexer
{
    static class Globals
    {
        /// <summary>
        /// The current settings for the indexer as specified on the command-line
        /// </summary>
        public static IndexerSettings Settings { get; set; }

        /// <summary>
        /// The language to use when recording a file in the sourcetrail-db
        /// </summary>
        public const string FileLanguage = "cpp";

        /// <summary>
        /// Gets or sets the <see cref="DataCollector"/> that writes to the sourcetrail database
        /// </summary>
        public static DataCollector DataCollector { get; set; }

        /// <summary>
        /// Gets or sets the current <see cref="Compilation"/> for the included files
        /// </summary>
        public static CSharpCompilation Compilation { get; set; }

        /// <summary>
        /// Gets or sets the symbol-id of the class that is currently processed by the <see cref="CodeWalker"/>
        /// </summary>
        [ThreadStatic]
        public static int CurrentClassId;

        /// <summary>
        /// Gets or sets the symbol-id of the method or property that is currently processed by the <see cref="CodeWalker"/>
        /// </summary>
        [ThreadStatic]
        public static int CurrentMethodId;
    }
}
