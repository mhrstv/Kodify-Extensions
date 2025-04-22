using System;
using System.IO;
using System.Text;
using FluentAssertions;
using Kodify.Extensions.Diagrams;
using Moq;
using Xunit;

namespace Kodify.Extensions.Tests.Diagrams.Services
{
    public class PUMLDiagramGeneratorTests : IDisposable
    {
        private readonly string _testProjectPath;
        private readonly string _testOutputPath;

        public PUMLDiagramGeneratorTests()
        {
            // Setup test paths
            _testProjectPath = Path.Combine(Path.GetTempPath(), "KodifyExtensionsTests", Guid.NewGuid().ToString());
            _testOutputPath = Path.Combine(_testProjectPath, "diagrams");
            
            // Ensure test directories exist
            Directory.CreateDirectory(_testProjectPath);
            Directory.CreateDirectory(_testOutputPath);
        }

        [Fact]
        public void GeneratePUML_WithValidCSharpFiles_GeneratesCorrectDiagram()
        {
            // Arrange
            var testClass = @"
namespace TestNamespace
{
    public class TestClass
    {
        private string _privateField;
        public string PublicProperty { get; set; }
        
        public void TestMethod()
        {
        }
    }
}";
            var testInterface = @"
namespace TestNamespace
{
    public interface ITestInterface
    {
        string TestProperty { get; set; }
        void TestMethod();
    }
}";

            var classPath = Path.Combine(_testProjectPath, "TestClass.cs");
            var interfacePath = Path.Combine(_testProjectPath, "TestInterface.cs");
            
            File.WriteAllText(classPath, testClass);
            File.WriteAllText(interfacePath, testInterface);

            var generator = new PUMLDiagramGenerator();

            // Act
            generator.GeneratePUML(_testProjectPath, _testOutputPath);

            // Assert
            var outputFile = Path.Combine(_testOutputPath, "ClassDiagrams.puml");
            File.Exists(outputFile).Should().BeTrue();
            
            var content = File.ReadAllText(outputFile);
            content.Should().Contain("@startuml");
            content.Should().Contain("@enduml");
            content.Should().Contain("class TestClass");
            content.Should().Contain("interface ITestInterface");
            content.Should().Contain("package TestNamespace");
            content.Should().Contain("+ PublicProperty : string");
            content.Should().Contain("- _privateField : string");
        }

        [Fact]
        public void GeneratePUML_WithInheritance_GeneratesCorrectRelationships()
        {
            // Arrange
            var baseClass = @"
namespace TestNamespace
{
    public class BaseClass
    {
        public virtual void BaseMethod() { }
    }
}";
            var derivedClass = @"
namespace TestNamespace
{
    public class DerivedClass : BaseClass, ITestInterface
    {
        public override void BaseMethod() { }
    }
}";
            var testInterface = @"
namespace TestNamespace
{
    public interface ITestInterface
    {
        void TestMethod();
    }
}";

            var basePath = Path.Combine(_testProjectPath, "BaseClass.cs");
            var derivedPath = Path.Combine(_testProjectPath, "DerivedClass.cs");
            var interfacePath = Path.Combine(_testProjectPath, "TestInterface.cs");
            
            File.WriteAllText(basePath, baseClass);
            File.WriteAllText(derivedPath, derivedClass);
            File.WriteAllText(interfacePath, testInterface);

            var generator = new PUMLDiagramGenerator();

            // Act
            generator.GeneratePUML(_testProjectPath, _testOutputPath);

            // Assert
            var outputFile = Path.Combine(_testOutputPath, "ClassDiagrams.puml");
            var content = File.ReadAllText(outputFile);
            
            content.Should().Contain("DerivedClass --|> BaseClass");
            content.Should().Contain("DerivedClass --|> ITestInterface");
        }

        [Fact]
        public void GeneratePUML_WithAssociations_GeneratesCorrectRelationships()
        {
            // Arrange
            var classWithAssociations = @"
namespace TestNamespace
{
    public class ClassA
    {
        private ClassB _classB;
        public ClassC PropertyC { get; set; }
    }

    public class ClassB { }
    public class ClassC { }
}";

            var filePath = Path.Combine(_testProjectPath, "ClassWithAssociations.cs");
            File.WriteAllText(filePath, classWithAssociations);

            var generator = new PUMLDiagramGenerator();

            // Act
            generator.GeneratePUML(_testProjectPath, _testOutputPath);

            // Assert
            var outputFile = Path.Combine(_testOutputPath, "ClassDiagrams.puml");
            var content = File.ReadAllText(outputFile);
            
            content.Should().Contain("ClassA --> ClassB : has");
            content.Should().Contain("ClassA --> ClassC : has");
        }

        public void Dispose()
        {
            // Cleanup test directories
            if (Directory.Exists(_testProjectPath))
            {
                try
                {
                    Directory.Delete(_testProjectPath, true);
                }
                catch (IOException)
                {
                    // Ignore errors on cleanup
                }
            }
        }
    }
} 