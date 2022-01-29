using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SourcetrailCSharpIndexer.Analyzing
{
    class CSharpAnalyzer
    {
#if DEBUG
        // should only be set to true when doing actual debugging
        // (VS behaves somewhat quirky when debugging in multithreaded mode)
        private const bool singleThreaded = false;
#else
        private const bool singleThreaded = false;
#endif

        private CSharpCompilation compilation;

        public void Analyze()
        {
            var watch = Stopwatch.StartNew();

            foreach (var input in Globals.Settings.InputList)
            {
                if (File.Exists(input))
                    ProcessInputFile(input);
                else if (Directory.Exists(input))
                    ProcessInputDirectory(input);
            }
            foreach (var refAssembly in Globals.Settings.ReferenceList)
                ProcessReferenceAssembly(refAssembly);

            Globals.Compilation = compilation;

            Console.WriteLine("Walking the code");
            foreach (var step in new[] { WalkerPhase.CollectSymbols, WalkerPhase.CollectReferences })
            {
                Console.WriteLine(step);
                if (singleThreaded)
                {
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        Console.Write('.');     // show some kind of progress
                        if (tree.HasCompilationUnitRoot)
                        {
                            var root = tree.GetCompilationUnitRoot();
                            var walker = new CodeWalker { Phase = step };
                            walker.Visit(root);
                            //DumpSyntaxNode(root, "");
                        }
                    }
                }
                else
                {
                    Parallel.ForEach(compilation.SyntaxTrees, tree =>
                    {
                        Console.Write('.');     // show some kind of progress
                        if (tree.HasCompilationUnitRoot)
                        {
                            var root = tree.GetCompilationUnitRoot();
                            var walker = new CodeWalker { Phase = step };
                            walker.Visit(root);
                            //DumpSyntaxNode(root, "");
                        }
                    });
                }
                Console.WriteLine();
            }

            watch.Stop();
            Console.WriteLine();
            Console.WriteLine("Total time elapsed: {0}", watch.Elapsed);
            Console.WriteLine("Processed {0} file(s), collected {1} symbol(s) and {2} reference(s)",
                Globals.DataCollector.NumFiles,
                Globals.DataCollector.NumSymbols,
                Globals.DataCollector.NumReferences);
            if (Globals.DataCollector.NumErrors > 0)
                Console.WriteLine("{0} error{1} {2} collected", Globals.DataCollector.NumErrors,
                    Globals.DataCollector.NumErrors == 1 ? "" : "s",
                    Globals.DataCollector.NumErrors == 1 ? "was" : "were");
        }

        private void ProcessInputDirectory(string path)
        {
            foreach (var file in Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories))
                ProcessInputFile(file);
        }

        private void ProcessInputFile(string filePath)
        {
            Console.WriteLine("Processing: {0}", filePath);

            var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(filePath), path: filePath);
            if (compilation == null)
            {
                compilation = CSharpCompilation.Create(Path.GetFileNameWithoutExtension(filePath));
                // reference all framework-assemblies (helps resolving method-overloads to system-types, e.g. string.Join(...))
                // adding just the single system-assembly does not seem to be enough...
                // was a bit hestitant at first because of the possible memory footprint, but it turned out be not that much actually
                if (string.IsNullOrWhiteSpace(Globals.Settings.FramworkPath))
                {
                    Console.WriteLine("REF: .NET framework path: " + Globals.Settings.FramworkPath);
                    var systemAssembly = typeof(string).Assembly;
                    var systemDir = Path.GetDirectoryName(systemAssembly.Location);
                    ProcessReferenceAssembly(systemDir);
                }
                else
                {
                    if (Directory.Exists(Globals.Settings.FramworkPath))
                        ProcessReferenceAssembly(Globals.Settings.FramworkPath);
                    else throw new DirectoryNotFoundException(
                        $"The specified framework directory '{Globals.Settings.FramworkPath}' does not exist.");
                }
            }
            // TODO: is it more efficient to collect the trees first and add them all in one step ?
            compilation = compilation.AddSyntaxTrees(tree);
        }

        private void ProcessReferenceAssembly(string path)
        {
            if (File.Exists(path))
            {
                compilation = compilation.AddReferences(MetadataReference.CreateFromFile(path));
                Console.WriteLine("REF: Referencing assembly " + path);
            }
            else if (Directory.Exists(path))
            {
                var metaReferences = Directory.GetFiles(path, "*.dll", SearchOption.TopDirectoryOnly)
                    .Select(file => 
                    {
                        Console.WriteLine("REF: Referencing assembly " + file);
                        return MetadataReference.CreateFromFile(file);
                    })
                    .ToArray();
                compilation = compilation.AddReferences(metaReferences);
            }
        }

        // used for debugging, dumps a syntax-node and all its children recursively
        private void DumpSyntaxNode(SyntaxNode root, string indent)
        {
            var kind = root.Kind();
            var location = root.GetLocation();
            var lineSpan = location.GetLineSpan();
            var text = root.ToString();
            if (text.Length > 60)
                text = text.Substring(0, 60);
            Console.WriteLine("{0} from [{1}, {2}] to [{3}, {4}] ({5}) '{6}'", indent + kind,
                lineSpan.StartLinePosition.Line, lineSpan.StartLinePosition.Character,
                lineSpan.EndLinePosition.Line, lineSpan.EndLinePosition.Character, 
                root.GetType().Name, text);

            foreach (var child in root.ChildNodes())
                DumpSyntaxNode(child, indent + "  ");
        }
    }
}
