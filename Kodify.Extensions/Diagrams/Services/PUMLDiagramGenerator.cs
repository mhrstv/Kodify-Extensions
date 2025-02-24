using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Kodify.Repository.Services;

namespace Kodify.Extensions.Diagrams
{
    public class PUMLDiagramGenerator
    {
        private readonly ProjectAnalyzer _projectAnalyzer;
        private Kodify.Repository.Models.ProjectInfo _projectInfo;

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
            var namespaceInterfaces = new Dictionary<string, List<(InterfaceDeclarationSyntax InterfaceDecl, string FilePath)>>();
            var relationships = new List<string>();
            
            foreach (var file in csFiles)
            {
                var fileContent = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(fileContent, path: file);
                var root = tree.GetRoot();
                var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>().ToList();
                var interfaceDeclarations = root.DescendantNodes().OfType<InterfaceDeclarationSyntax>().ToList();
                
                foreach (var classDecl in classDeclarations)
                {
                    string ns = GetNamespace(classDecl) ?? "Global";
                    
                    if (!namespaceClasses.ContainsKey(ns))
                    {
                        namespaceClasses[ns] = new List<(ClassDeclarationSyntax, string)>();
                    }
                    namespaceClasses[ns].Add((classDecl, file));

                    // Handle inheritance
                    if (classDecl.BaseList != null)
                    {
                        foreach (var baseType in classDecl.BaseList.Types)
                        {
                            relationships.Add($"{classDecl.Identifier.Text} --|> {baseType.Type}");
                        }
                    }
                }

                foreach (var interfaceDecl in interfaceDeclarations)
                {
                    string ns = GetNamespaceForInterface(interfaceDecl) ?? "Global";
                    
                    if (!namespaceInterfaces.ContainsKey(ns))
                    {
                        namespaceInterfaces[ns] = new List<(InterfaceDeclarationSyntax, string)>();
                    }
                    namespaceInterfaces[ns].Add((interfaceDecl, file));
                }
            }
            
            // Build the PUML using a StringBuilder.
            var sb = new StringBuilder();
            sb.AppendLine("@startuml");
            
            // Global styling
            sb.AppendLine("' Configuration");
            sb.AppendLine("hide empty members");
            sb.AppendLine("skinparam shadowing false");
            sb.AppendLine("skinparam handwritten false");
            sb.AppendLine("skinparam monochrome false");
            sb.AppendLine("skinparam linetype ortho");
            
            // Class styling
            sb.AppendLine("skinparam class {");
            sb.AppendLine("    BackgroundColor<<Clickable>> #E3F2FD");
            sb.AppendLine("    BorderColor<<Clickable>> #1976D2");
            sb.AppendLine("    HeaderBackgroundColor<<Clickable>> #BBDEFB");
            sb.AppendLine("    FontSize 12");
            sb.AppendLine("    AttributeFontSize 11");
            sb.AppendLine("    AttributeFontColor #333333");
            sb.AppendLine("    BorderThickness 1");
            sb.AppendLine("}");
            
            // Interface styling
            sb.AppendLine("skinparam interface {");
            sb.AppendLine("    BackgroundColor<<Clickable>> #F1F8E9");
            sb.AppendLine("    BorderColor<<Clickable>> #689F38");
            sb.AppendLine("    HeaderBackgroundColor<<Clickable>> #DCEDC8");
            sb.AppendLine("    FontSize 12");
            sb.AppendLine("    AttributeFontSize 11");
            sb.AppendLine("    AttributeFontColor #333333");
            sb.AppendLine("    BorderThickness 1");
            sb.AppendLine("}");
            
            // Arrow styling
            sb.AppendLine("skinparam arrow {");
            sb.AppendLine("    Color #666666");
            sb.AppendLine("    FontSize 11");
            sb.AppendLine("    Thickness 1");
            sb.AppendLine("}");
            
            // For each namespace (or package), list the interfaces and classes
            foreach (var ns in namespaceClasses.Keys.Union(namespaceInterfaces.Keys).OrderBy(n => n))
            {
                sb.AppendLine($"package {ns} {{");
                
                // Add interfaces first
                if (namespaceInterfaces.ContainsKey(ns))
                {
                    foreach (var (interfaceDecl, filePath) in namespaceInterfaces[ns])
                    {
                        string interfaceName = interfaceDecl.Identifier.Text;
                        string fullFilePath = Path.GetFullPath(filePath);
                        string fileUri = new Uri(fullFilePath).AbsoluteUri;
                        
                        sb.AppendLine($"interface {interfaceName} <<Clickable>> [[{fileUri}]] {{");
                        
                        foreach (var member in interfaceDecl.Members)
                        {
                            if (member is MethodDeclarationSyntax method)
                            {
                                sb.AppendLine($"    + {method.Identifier.Text}()");
                            }
                            else if (member is PropertyDeclarationSyntax property)
                            {
                                string typeName = property.Type.ToString();
                                sb.AppendLine($"    + {property.Identifier.Text} : {typeName}");
                            }
                        }
                        
                        sb.AppendLine("}");
                        sb.AppendLine();
                    }
                }

                // Add classes
                if (namespaceClasses.ContainsKey(ns))
                {
                    foreach (var (classDecl, filePath) in namespaceClasses[ns])
                    {
                        string className = classDecl.Identifier.Text;
                        string fullFilePath = Path.GetFullPath(filePath);
                        string fileUri = new Uri(fullFilePath).AbsoluteUri;
                        
                        sb.AppendLine($"class {className} <<Clickable>> [[{fileUri}]] {{");
                        
                        // Add fields
                        foreach (var member in classDecl.Members)
                        {
                            if (member is FieldDeclarationSyntax field)
                            {
                                string visibility = field.Modifiers.Any(SyntaxKind.PublicKeyword) ? "+" : "-";
                                string typeName = field.Declaration.Type.ToString();
                                foreach (var variable in field.Declaration.Variables)
                                {
                                    sb.AppendLine($"    {visibility} {variable.Identifier.Text} : {typeName}");
                                }
                            }
                            else if (member is MethodDeclarationSyntax method)
                            {
                                string visibility = method.Modifiers.Any(SyntaxKind.PublicKeyword) ? "+" : "-";
                                string returnType = method.ReturnType.ToString();
                                sb.AppendLine($"    {visibility} {method.Identifier.Text}() : {returnType}");
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

                        // Add relationships based on field types and method parameters
                        foreach (var member in classDecl.Members)
                        {
                            if (member is FieldDeclarationSyntax field)
                            {
                                string typeName = field.Declaration.Type.ToString();
                                if (!IsBuiltInType(typeName))
                                {
                                    relationships.Add($"{className} --> {typeName} : has");
                                }
                            }
                            else if (member is PropertyDeclarationSyntax property)
                            {
                                string typeName = property.Type.ToString();
                                if (!IsBuiltInType(typeName))
                                {
                                    relationships.Add($"{className} --> {typeName} : has");
                                }
                            }
                        }
                    }
                }
                
                sb.AppendLine("}");
            }

            // Add all relationships at the end
            sb.AppendLine();
            sb.AppendLine("' Relationships");
            foreach (var relationship in relationships.Distinct())
            {
                sb.AppendLine(relationship);
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

        private string GetNamespaceForInterface(InterfaceDeclarationSyntax interfaceDecl)
        {
            SyntaxNode parent = interfaceDecl.Parent;
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

        private bool IsBuiltInType(string typeName)
        {
            var builtInTypes = new[] { "string", "int", "bool", "double", "float", "decimal", "char", "byte", "object", "String", "Int32", "Boolean", "Double", "Single", "Decimal", "Char", "Byte", "Object" };
            return builtInTypes.Contains(typeName) || typeName.StartsWith("List<") || typeName.StartsWith("IEnumerable<") || typeName.StartsWith("Dictionary<");
        }
    }
}