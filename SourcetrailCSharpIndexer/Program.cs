using SourcetrailCSharpIndexer.Analyzing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;

namespace SourcetrailCSharpIndexer
{
    class Program
    {        
        static void Main(string[] args)
        {
            Globals.Settings = new IndexerSettings();

            if (!ProcessCommandLine(args))
            {
                Usage();
                Environment.ExitCode = 1;
                return;
            }
            Globals.DataCollector = new DataCollector(Globals.Settings.OutputPath, true);
            try
            {
                var analyzer = new CSharpAnalyzer();
                analyzer.Analyze();
            }
            finally
            {
                Globals.DataCollector?.Dispose();
            }
        }

        private static void Usage()
        {
            var versionInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            Console.WriteLine();
            Console.WriteLine("SourcetrailCSharpIndexer v{0}.{1}", versionInfo.FileMajorPart, versionInfo.FileMinorPart);
            Console.WriteLine("Arguments:");
            Console.WriteLine(" -i  Input-File/Path");
            Console.WriteLine("     Specifies the full path of a file or a folder to index");
            Console.WriteLine("     If the argument refers to a folder, all *.cs-files in this folder and all sub-folders will be included");
            Console.WriteLine("     May be specified multiple times");
            Console.WriteLine(" -o  OutputFilename");
            Console.WriteLine("     Full path and filename of the generated database");
            Console.WriteLine(" -r  AssemblyPathOrFileName");
            Console.WriteLine("     Specifies the path to a reference-assembly (used for type-resolution)");
            Console.WriteLine("     If the argument refers to a folder, all assemblies in this folder will be referenced");
            Console.WriteLine("     May be specified multiple times");
            Console.WriteLine(" -fp Framework-Path");
            Console.WriteLine("     Specifies the path to the .net-framework, your application is based on");
            Console.WriteLine("     By default, the indexer attempts to use the .net5 assemblies");
            Console.WriteLine(" -ox (OmitExternals)");
            Console.WriteLine("     Specifies, that types from referenced assemblies should be omitted from the generated database");
        }

        private static bool ProcessCommandLine(string[] args)
        {
            var i = 0;
            while (i < args.Length)
            {
                var arg = args[i];
                if (arg.StartsWith("/") || arg.StartsWith("-"))
                    arg = arg.Substring(1);
                switch (arg.ToLowerInvariant())
                {
                    case "i":   // input file/path
                        i++;
                        if (i < args.Length)
                            Globals.Settings.InputList.Add(args[i]);
                        else
                            return false;
                        break;
                    case "r":   // reference assembly/assembly path
                        i++;
                        if (i < args.Length)
                            Globals.Settings.ReferenceList.Add(args[i]);
                        else
                            return false;
                        break;
                    case "fp":   // framework path
                        i++;
                        if (i < args.Length)
                            Globals.Settings.FramworkPath = args[i];
                        else
                            return false;
                        break;
                    case "ox":  // omit externals
                        Globals.Settings.OmitExternals = true;
                        break;
                    case "o":   // output path
                        i++;
                        if (i < args.Length)
                            Globals.Settings.OutputPath = args[i];
                        else
                            return false;
                        break;
                }
                i++;
            }
            return Globals.Settings.InputList.Count > 0
                && !string.IsNullOrWhiteSpace(Globals.Settings.OutputPath);
        }
    }
}
