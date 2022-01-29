using System.Collections.Generic;

namespace SourcetrailCSharpIndexer
{
    class IndexerSettings
    {
        /// <summary>
        /// List of file and/or folders to index
        /// </summary>
        public IList<string> InputList { get; } = new List<string>();

        /// <summary>
        /// List of assembly-paths or folders where referenced assemblies are stored 
        /// </summary>
        public IList<string> ReferenceList { get; } = new List<string>();

        /// <summary>
        /// Path of the Sourcetrail-database that we generate
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Whether types from foreign assemblies should be included in the database
        /// </summary>
        public bool OmitExternals { get; set; }

        /// <summary>
        /// The full path to the .NET Framework the currently processed files are based on.<br/>
        /// Used for loading reference-assemblies
        /// </summary>
        public string FramworkPath { get; set; }
    }
}
