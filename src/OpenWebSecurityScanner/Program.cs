using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace OpenWebSecurityScanner
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var path = "../../testProjects/WebAPI1/WebAPI1.csproj";

            var instainces = MSBuildLocator.QueryVisualStudioInstances();
            foreach (var instance in instainces)
            {
                Console.WriteLine($"Found `{instance.Name}` at `{instance.MSBuildPath}`.");
            }

            MSBuildLocator.RegisterDefaults();

            using (var workspace = MSBuildWorkspace.Create())
            {
                var project = await workspace.OpenProjectAsync(path);
                var compilation = await project.GetCompilationAsync();

                PrintDiagnostics(workspace, project);

                foreach (var doc in project.Documents)
                {
                    if (!doc.SupportsSemanticModel)
                    {
                        continue;
                    }

                    var semanticModel = await doc.GetSemanticModelAsync();
                    var syntaxRoot = await doc.GetSyntaxRootAsync();

                    var classDeclarations = syntaxRoot.DescendantNodes()
                                                      .OfType<ClassDeclarationSyntax>()
                                                      .ToList();

                    foreach (var classDeclaration in classDeclarations)
                    {
                        var symbol = semanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                        var symbolBaseType = symbol.BaseType;
                        while (symbolBaseType != null)
                        {
                            if (symbolBaseType.ToString() == "Microsoft.AspNetCore.Mvc.ControllerBase")
                            {
                                Console.WriteLine(symbol.ToString());
                            }

                            symbolBaseType = symbolBaseType.BaseType;
                        }
                    }
                }
            }
        }

        static void PrintDiagnostics(MSBuildWorkspace workspace, Project project)
        {
            var diagnostics = workspace.Diagnostics;
            if (diagnostics.Any())
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                foreach (var diagnostic in diagnostics)
                {
                    Console.WriteLine(diagnostic.Message);
                }
                Console.ResetColor();
                return;
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"Successfully loaded project `{project.Name}` located at {project.FilePath}.");
            Console.ResetColor();
        }
    }
}
