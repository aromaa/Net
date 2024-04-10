﻿using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Net.Communication.SourceGenerator;

[Generator(LanguageNames.CSharp)]
public sealed class PacketManagerGenerator : IIncrementalGenerator
{
	public void Initialize(IncrementalGeneratorInitializationContext context)
	{
		IncrementalValuesProvider<(INamedTypeSymbol Type, ImmutableArray<INamedTypeSymbol> Managers)> registrations = context.SyntaxProvider.ForAttributeWithMetadataName("Net.Communication.Attributes.PacketManagerRegisterAttribute",
			static (node, _) => node is ClassDeclarationSyntax,
			static (context, _) =>
			{
				ImmutableArray<INamedTypeSymbol>.Builder builder = ImmutableArray.CreateBuilder<INamedTypeSymbol>(context.Attributes.Length);
				foreach (AttributeData attributeData in context.Attributes)
				{
					if (attributeData.ConstructorArguments.IsDefaultOrEmpty || attributeData.ConstructorArguments[0].Value is not INamedTypeSymbol namedType)
					{
						continue;
					}

					builder.Add(namedType);
				}

				return ((INamedTypeSymbol)context.TargetSymbol, builder.ToImmutable());
			});

		IncrementalValuesProvider<(SemanticModel SemanticModel, MethodDeclarationSyntax GeneratorMethod, INamedTypeSymbol? Manager)> generators = context.SyntaxProvider.ForAttributeWithMetadataName("Net.Communication.Attributes.PacketManagerGeneratorAttribute",
			static (node, _) => node is MethodDeclarationSyntax,
			static (context, _) => (context.SemanticModel, (MethodDeclarationSyntax)context.TargetNode, context.Attributes[0].ConstructorArguments[0].Value as INamedTypeSymbol));

		registrations.Combine(generators.Collect());

		context.RegisterImplementationSourceOutput(generators.Combine(registrations.Collect()), static (sourceProductionContext, value) =>
		{
			(SemanticModel semanticModel, MethodDeclarationSyntax generatorMethod, INamedTypeSymbol? manager) = value.Left;
			if (manager is null)
			{
				return;
			}

			INamedTypeSymbol? parserType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Incoming.Parser.IIncomingPacketParser");
			INamedTypeSymbol? parserGenericType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Incoming.Parser.IIncomingPacketParser`1");
			INamedTypeSymbol? handlerType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Incoming.Handler.IIncomingPacketHandler");
			INamedTypeSymbol? handlerGenericType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Incoming.Handler.IIncomingPacketHandler`1");
			INamedTypeSymbol? composerType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Outgoing.IOutgoingPacketComposer");
			INamedTypeSymbol? composerGenericType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Outgoing.IOutgoingPacketComposer`1");

			INamedTypeSymbol? parserIdType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Attributes.PacketParserIdAttribute");
			INamedTypeSymbol? composerIdType = semanticModel.Compilation.GetTypeByMetadataName("Net.Communication.Attributes.PacketComposerIdAttribute");

			StringBuilder hintNameBuilder = new();

			using StringWriter stream = new();
			using (IndentedTextWriter writer = new(stream, "\t"))
			{
				writer.WriteLine("// <auto-generated/>");
				writer.WriteLine("#pragma warning disable");
				writer.WriteLine();

				List<ClassDeclarationSyntax> hierarchy = [];
				BaseNamespaceDeclarationSyntax? namespaceDeclaration = null;
				for (SyntaxNode? node = generatorMethod; node is not null; node = node.Parent)
				{
					if (node is ClassDeclarationSyntax classDeclaration)
					{
						hierarchy.Add(classDeclaration);
					}
					else if (node is BaseNamespaceDeclarationSyntax syntax)
					{
						namespaceDeclaration = syntax;

						break;
					}
					else if (node is CompilationUnitSyntax)
					{
						break;
					}
				}

				if (namespaceDeclaration is not null)
				{
					hintNameBuilder.Append(namespaceDeclaration.Name.ToString().Replace('.', '_'));
					hintNameBuilder.Append('_');

					writer.WriteLine($"namespace {namespaceDeclaration.Name}");
					writer.WriteLine("{");
					writer.Indent++;
				}

				for (int j = hierarchy.Count - 1; j >= 0; j--)
				{
					ClassDeclarationSyntax classDeclaration = hierarchy[j];

					hintNameBuilder.Append(classDeclaration.Identifier);
					hintNameBuilder.Append('_');

					if (classDeclaration.Arity > 0)
					{
						hintNameBuilder.Append(classDeclaration.Arity);
						hintNameBuilder.Append('_');
					}

					writer.WriteLine($"partial class {classDeclaration.Identifier.ToString() + classDeclaration.TypeParameterList}");
					writer.WriteLine("{");
					writer.Indent++;
				}

				string returnType = $"global::Net.Communication.Manager.{generatorMethod.ReturnType}";

				writer.WriteLine($"{generatorMethod.Modifiers} {returnType} {generatorMethod.Identifier}({generatorMethod.ParameterList.Parameters})");
				writer.WriteLine("{");
				writer.Indent++;
				writer.WriteLine($"global::System.Collections.Immutable.ImmutableArray<{returnType}.ParserData>.Builder parsers = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<{returnType}.ParserData>();");
				writer.WriteLine($"global::System.Collections.Immutable.ImmutableArray<{returnType}.HandlerData>.Builder handlers = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<{returnType}.HandlerData>();");
				writer.WriteLine($"global::System.Collections.Immutable.ImmutableArray<{returnType}.ComposerData>.Builder composers = global::System.Collections.Immutable.ImmutableArray.CreateBuilder<{returnType}.ComposerData>();");

				foreach ((INamedTypeSymbol type, ImmutableArray<INamedTypeSymbol> managers) in value.Right)
				{
					if (!managers.Contains(manager))
					{
						continue;
					}

					bool handler = false;
					object? parserId = null;
					object? composerId = null;
					ITypeSymbol? parserHandlesType = null;
					ITypeSymbol? handlerHandlesType = null;
					ITypeSymbol? composerHandlesType = null;
					foreach (INamedTypeSymbol implementedInterface in type.AllInterfaces)
					{
						if (implementedInterface.IsGenericType)
						{
							if (SymbolEqualityComparer.Default.Equals(implementedInterface.ConstructedFrom, parserGenericType))
							{
								parserHandlesType = GetHandledType(implementedInterface.TypeArguments[0]);
							}
							else if (SymbolEqualityComparer.Default.Equals(implementedInterface.ConstructedFrom, handlerGenericType))
							{
								handler = true;
								handlerHandlesType = GetHandledType(implementedInterface.TypeArguments[0]);
							}
							else if (SymbolEqualityComparer.Default.Equals(implementedInterface.ConstructedFrom, composerGenericType))
							{
								composerHandlesType = GetHandledType(implementedInterface.TypeArguments[0]);
							}

							static ITypeSymbol? GetHandledType(ITypeSymbol symbol)
							{
								if (symbol is ITypeParameterSymbol typeParameter)
								{
									if (typeParameter.ConstraintTypes.Length == 1)
									{
										return typeParameter.ConstraintTypes[0];
									}

									return null;
								}

								return symbol;
							}

							continue;
						}

						if (SymbolEqualityComparer.Default.Equals(implementedInterface, parserType))
						{
							foreach (AttributeData attributeData in type.GetAttributes())
							{
								if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, parserIdType))
								{
									parserId = attributeData.ConstructorArguments[0].Value;

									break;
								}
							}
						}
						else if (SymbolEqualityComparer.Default.Equals(implementedInterface, handlerType))
						{
							handler = true;
						}
						else if (SymbolEqualityComparer.Default.Equals(implementedInterface, composerType))
						{
							foreach (AttributeData attributeData in type.GetAttributes())
							{
								if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, composerIdType))
								{
									composerId = attributeData.ConstructorArguments[0].Value;

									break;
								}
							}
						}
					}

					if (parserId is not null)
					{
						writer.WriteLine($"parsers.Add(new {returnType}.ParserData(typeof({(type.IsGenericType ? type.ConstructUnboundGenericType() : type)}), {parserId}, {(parserHandlesType is not null ? $"typeof({parserHandlesType})" : "null")}));");
					}

					if (handler)
					{
						writer.WriteLine($"handlers.Add(new {returnType}.HandlerData(typeof({(type.IsGenericType ? type.ConstructUnboundGenericType() : type)}), {(handlerHandlesType is not null ? $"typeof({handlerHandlesType})" : "null")}));");
					}

					if (composerId is not null)
					{
						writer.WriteLine($"composers.Add(new {returnType}.ComposerData(typeof({(type.IsGenericType ? type.ConstructUnboundGenericType() : type)}), {composerId}, {(composerHandlesType is not null ? $"typeof({composerHandlesType})" : "null")}));");
					}
				}

				writer.WriteLine($"return new {returnType}(parsers.ToImmutable(), handlers.ToImmutable(), composers.ToImmutable());");

				writer.Indent--;
				writer.WriteLine("}");

				for (int j = namespaceDeclaration is not null ? -1 : 0; j < hierarchy.Count; j++)
				{
					writer.Indent--;
					writer.WriteLine("}");
				}
			}

			hintNameBuilder.Append(generatorMethod.Identifier);
			hintNameBuilder.Append(".g.cs");

			string hintName = hintNameBuilder.ToString();
			string source = stream.ToString();

			sourceProductionContext.AddSource(hintName, source);
		});
	}
}
