# Kodify.Extensions

A Kodify subpackage that provides automated documentation and visualization tools for .NET projects. This package automatically generates PlantUML class diagrams and documentation based on project analysis.

## Features

### PlantUML Class Diagram Generation

Automatically generates comprehensive class diagrams with a single line of code:
```csharp
var generator = new PUMLDiagramGenerator();
generator.GeneratePUML();
```

The generated diagrams include:
- Class and interface hierarchies
- Inheritance relationships
- Field and property associations
- Method signatures
- Access modifiers
- Namespace organization

Features:
- Interactive clickable diagrams
- Automatic project structure detection
- Modern styling with Material Design colors
- Complete type relationship mapping
- Namespace-based package organization

### Diagram Customization

The generated diagrams feature professional styling:

- Clean, modern appearance
- Distinct visual hierarchy
- Clear relationship indicators
- Optimized readability
- Interactive navigation

Visual elements include:
- Classes (Light blue theme)
- Interfaces (Light green theme)
- Relationships (Clean orthogonal lines)
- Custom fonts and sizing
- Proper spacing and organization

## Installation

```bash
dotnet add package Kodify.Extensions
```

## Quick Start

1. Generate class diagrams:
```csharp
using Kodify.Extensions.Diagrams.Services;

// Create a new diagram generator
var generator = new PUMLDiagramGenerator();

// Generate diagrams in the default location
generator.GeneratePUML();

// Or specify a custom output path
generator.GeneratePUML("path");
```

## Generated Files

### Class Diagrams
- `/diagrams/ClassDiagrams.puml` - Main class diagram file
- Interactive elements linking to source files
- Organized by namespaces
- Complete relationship mapping

### Output Format
The generated PUML files include:
- Professional styling configuration
- Material Design color scheme
- Clickable elements for navigation
- Comprehensive type relationships
- Clear visual hierarchy

## Dependencies

This package is part of the Kodify ecosystem and requires:
- Kodify (>= 0.1.8)
- .NET 8.0 or later
- Microsoft.CodeAnalysis.CSharp
- LibGit2Sharp (for repository detection)

## Best Practices

The generated diagrams follow UML best practices:
- Clear visual hierarchy
- Proper relationship notation
- Comprehensive member visibility
- Organized namespace structure
- Interactive source code navigation
- Professional styling and colors

## References

This is a subpackage of [Kodify](https://github.com/mhrstv/Kodify). Please see the main package for more information.

## License

MIT License - see the main Kodify repository for details. 