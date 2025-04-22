using System;
using System.IO;
using System.Text.RegularExpressions;
using FluentAssertions;
using Kodify.Extensions.Diagrams;
using Xunit;

namespace Kodify.Extensions.Tests.Diagrams
{
    public class PUMLFormatTests : IDisposable
    {
        private readonly string _testPath;
        private readonly string _outputPath;
        
        public PUMLFormatTests()
        {
            _testPath = Path.Combine(Path.GetTempPath(), "KodifyPUMLTests", Guid.NewGuid().ToString());
            _outputPath = Path.Combine(_testPath, "output");
            
            Directory.CreateDirectory(_testPath);
            Directory.CreateDirectory(_outputPath);
        }
        
        [Fact]
        public void PUMLOutput_ShouldHaveCorrectStructure()
        {
            // Arrange
            var simpleClass = @"
namespace TestNamespace
{
    public class SimpleClass
    {
        public string Test { get; set; }
    }
}";
            var filePath = Path.Combine(_testPath, "SimpleClass.cs");
            File.WriteAllText(filePath, simpleClass);
            
            var generator = new PUMLDiagramGenerator();
            
            // Act
            generator.GeneratePUML(_testPath, _outputPath);
            
            // Assert
            var outputFile = Path.Combine(_outputPath, "ClassDiagrams.puml");
            var content = File.ReadAllText(outputFile);
            
            // Verify basic structure
            content.Should().StartWith("@startuml");
            content.Should().EndWith("@enduml" + Environment.NewLine);
            
            // Verify styling sections
            content.Should().Contain("' Configuration");
            content.Should().Contain("skinparam class {");
            content.Should().Contain("skinparam interface {");
            content.Should().Contain("skinparam arrow {");
            
            // Verify package and class declaration
            content.Should().Contain("package TestNamespace {");
            content.Should().Contain("class SimpleClass <<Clickable>>");
            
            // Verify property format
            content.Should().Contain("+ Test : string");
            
            // Verify links are in correct format
            var linkPattern = new Regex(@"\[\[file:///[^]]+\]\]");
            linkPattern.IsMatch(content).Should().BeTrue("because the diagram should contain clickable file links");
        }
        
        [Fact]
        public void PUMLOutput_ShouldUseCorrectStyling()
        {
            // Arrange
            var simpleClass = @"
namespace TestNamespace
{
    public class SimpleClass { }
}";
            var filePath = Path.Combine(_testPath, "SimpleClass.cs");
            File.WriteAllText(filePath, simpleClass);
            
            var generator = new PUMLDiagramGenerator();
            
            // Act
            generator.GeneratePUML(_testPath, _outputPath);
            
            // Assert
            var outputFile = Path.Combine(_outputPath, "ClassDiagrams.puml");
            var content = File.ReadAllText(outputFile);
            
            // Check for styling elements
            content.Should().Contain("BackgroundColor<<Clickable>>");
            content.Should().Contain("BorderColor<<Clickable>>");
            content.Should().Contain("FontSize");
            
            // Check for specific styling improvements we made
            content.Should().Contain("hide empty members");
            content.Should().Contain("skinparam shadowing false");
            content.Should().Contain("skinparam monochrome false");
            
            // Color values should use hex format
            content.Should().Match(m => m.Contains("#") && 
                                      (m.Contains("E3F2FD") || 
                                       m.Contains("F1F8E9") || 
                                       m.Contains("333333")));
        }
        
        public void Dispose()
        {
            if (Directory.Exists(_testPath))
            {
                try 
                {
                    Directory.Delete(_testPath, true);
                }
                catch (IOException)
                {
                    // Ignore cleanup errors
                }
            }
        }
    }
} 