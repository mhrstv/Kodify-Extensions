using System;
using System.IO;
using System.Reflection;
using FluentAssertions;
using Xunit;

namespace Kodify.Extensions.Tests.Structure
{
    public class ProjectStructureTests
    {
        private readonly string? _projectRoot;
        
        public ProjectStructureTests()
        {
            try
            {
                // Find the project root by going up from the test assembly location
                var assemblyLocation = Assembly.GetExecutingAssembly().Location;
                var directoryPath = Path.GetDirectoryName(assemblyLocation);
                
                if (directoryPath != null)
                {
                    var directory = new DirectoryInfo(directoryPath);
                    
                    // Go up until we find the solution directory
                    while (directory != null && !File.Exists(Path.Combine(directory.FullName, "Kodify.Extensions.sln")))
                    {
                        directory = directory.Parent;
                    }
                    
                    _projectRoot = directory?.FullName;
                }
            }
            catch (Exception)
            {
                // If anything goes wrong, leave the project root as null
                _projectRoot = null;
            }
        }
        
        [Fact]
        public void ProjectStructure_ShouldHaveCorrectFolders()
        {
            // Skip if we couldn't find the project root
            if (string.IsNullOrEmpty(_projectRoot))
            {
                return;
            }
            
            // Check for required project folders
            var mainProjectPath = Path.Combine(_projectRoot, "Kodify.Extensions");
            var diagramsFolder = Path.Combine(mainProjectPath, "Diagrams");
            
            Directory.Exists(mainProjectPath).Should().BeTrue("because the main project folder should exist");
            Directory.Exists(diagramsFolder).Should().BeTrue("because the Diagrams folder should exist");
        }
        
        [Fact]
        public void ProjectStructure_ShouldHaveRequiredFiles()
        {
            // Skip if we couldn't find the project root
            if (string.IsNullOrEmpty(_projectRoot))
            {
                return;
            }
            
            // Check for required files
            var mainProjectPath = Path.Combine(_projectRoot, "Kodify.Extensions");
            var csprojFile = Path.Combine(mainProjectPath, "Kodify.Extensions.csproj");
            var readmeFile = Path.Combine(mainProjectPath, "README.md");
            var pumlGeneratorFile = Path.Combine(mainProjectPath, "Diagrams", "Services", "PUMLDiagramGenerator.cs");
            
            File.Exists(csprojFile).Should().BeTrue("because the project file should exist");
            File.Exists(readmeFile).Should().BeTrue("because the README file should exist");
            File.Exists(pumlGeneratorFile).Should().BeTrue("because the PUML generator file should exist");
        }
    }
} 