using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Kodify.AutoDoc.Models;
using Kodify.AutoDoc;

namespace Kodify.Extensions.Diagrams.Services
{
    public class PUMLDiagramGenerator
    {
        private readonly ProjectAnalyzer _projectAnalyzer;
        private AutoDoc.Models.ProjectInfo _projectInfo;

        public PUMLDiagramGenerator()
        {
            _projectAnalyzer = new ProjectAnalyzer();
            _projectInfo = _projectAnalyzer.Analyze();
        }
        
        public void GeneratePUML()
        {
            string outputPath = Path.Combine(DetectProjectRoot(), "diagrams");
            GeneratePUML(outputPath);
        }

        public void GeneratePUML(string outputPath)
        {
            var sourcePath = DetectProjectRoot();
            GeneratePUMLDiagrams(sourcePath, outputPath);
        }

        private void GeneratePUMLDiagrams(string sourcePath, string outputPath)
        {
            // Get all C# files recursively.
            var csFiles = Directory.GetFiles(sourcePath, "*.cs", SearchOption.AllDirectories);
            
            // Group classes by namespace.
            // We'll store for each namespace a list of tuples containing the class declaration and its file path.
            var namespaceClasses = new Dictionary<string, List<(ClassDeclarationSyntax ClassDecl, string FilePath)>>();
            
            foreach (var file in csFiles)
            {
                var fileContent = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(fileContent, path: file);
                var root = tree.GetRoot();
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                
                foreach (var classDecl in classDeclarations)
                {
                    string ns = GetNamespace(classDecl) ?? "Global";
                    
                    if (!namespaceClasses.ContainsKey(ns))
                    {
                        namespaceClasses[ns] = new List<(ClassDeclarationSyntax, string)>();
                    }
                    namespaceClasses[ns].Add((classDecl, file));
                }
            }
            
            // Build the PUML using a StringBuilder.
            var sb = new StringBuilder();
            sb.AppendLine("@startuml");
            sb.AppendLine("' Interactive diagram with clickable classes");
            sb.AppendLine("skinparam class {");
            sb.AppendLine("  BackgroundColor<<Clickable>> LightBlue");
            sb.AppendLine("  BorderColor<<Clickable>> Black");
            sb.AppendLine("}");
            
            // For each namespace (or package), list the classes.
            foreach (var ns in namespaceClasses.Keys.OrderBy(n => n))
            {
                sb.AppendLine($"package {ns} {{");
                
                foreach (var (classDecl, filePath) in namespaceClasses[ns])
                {
                    string className = classDecl.Identifier.Text;
                    
                    // Create a clickable hyperlink using a file:// URL.
                    // Convert the file path to an absolute URI.
                    string fullFilePath = Path.GetFullPath(filePath);
                    string fileUri = new Uri(fullFilePath).AbsoluteUri;
                    
                    // Format: class ClassName <<Clickable>> [[fileUri]]
                    sb.AppendLine($"class {className} <<Clickable>> [[{fileUri}]] {{");
                    
                    // Add method and property members.
                    foreach (var member in classDecl.Members)
                    {
                        if (member is MethodDeclarationSyntax method)
                        {
                            // Use + for public, - for non‑public.
                            string visibility = method.Modifiers.Any(SyntaxKind.PublicKeyword) ? "+" : "-";
                            sb.AppendLine($"    {visibility} {method.Identifier.Text}()");
                        }
                        else if (member is PropertyDeclarationSyntax property)
                        {
                            string visibility = property.Modifiers.Any(SyntaxKind.PublicKeyword) ? "+" : "-";
                            string typeName = property.Type.ToString();
                            sb.AppendLine($"    {visibility} {property.Identifier.Text} : {typeName}");
                        }
                    }
                    
                    sb.AppendLine("}");
                    sb.AppendLine();
                }
                
                sb.AppendLine("}");
            }
            
            sb.AppendLine("@enduml");
            
            // Write the assembled PUML content to the output file.
            Directory.CreateDirectory(outputPath);
            string outputFile = Path.Combine(outputPath, "ClassDiagrams.puml");
            File.WriteAllText(outputFile, sb.ToString());
        }

        private string GetNamespace(ClassDeclarationSyntax classDecl)
        {
            SyntaxNode parent = classDecl.Parent;
            while (parent != null)
            {
                if (parent is NamespaceDeclarationSyntax namespaceDecl)
                {
                    return namespaceDecl.Name.ToString();
                }
                parent = parent.Parent;
            }
            return null;
        }

        private string DetectProjectRoot()
        {
            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            DirectoryInfo trueRoot = null;

            // First check for Git repository root
            try
            {
                var repoPath = LibGit2Sharp.Repository.Discover(directory.FullName);
                if (!string.IsNullOrEmpty(repoPath))
                {
                    using var repo = new LibGit2Sharp.Repository(repoPath);
                    trueRoot = new DirectoryInfo(repo.Info.WorkingDirectory);
                }
            }
            catch { }

            // If Git root not found, look for solution/project files
            if (trueRoot == null)
            {
                var currentDir = directory;
                while (currentDir != null)
                {
                    if (currentDir.GetFiles().Any(f => f.Extension == ".sln" || f.Extension == ".csproj"))
                    {
                        trueRoot = currentDir;
                        break;
                    }
                    currentDir = currentDir.Parent;
                }
            }

            return trueRoot?.FullName ?? 
                throw new DirectoryNotFoundException("Project root not found");
        }
    }
}