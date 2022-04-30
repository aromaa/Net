using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Net.Communication.SourceGenerator
{
	[Generator]
    public partial class ByRefTypeHandlerGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not SyntaxReceiver receiver)
            {
                return;
            }

            Compilation compilation = context.Compilation;

            INamedTypeSymbol? packetByRefTypeAttributeSymbol = compilation.GetTypeByMetadataName("Net.Communication.Attributes.PacketByRefTypeAttribute");
            if (packetByRefTypeAttributeSymbol is null)
            {
                return;
            }

            foreach (ClassDeclarationSyntax @class in receiver.ClassesWithAttributes)
            {
                SemanticModel model = compilation.GetSemanticModel(@class.SyntaxTree);

                if (model.GetDeclaredSymbol(@class) is not ITypeSymbol symbol)
                {
                    continue;
                }

                ITypeSymbol? packetType = null;
                int type = 0;

                foreach (AttributeData attributeData in symbol.GetAttributes())
                {
                    if (SymbolEqualityComparer.Default.Equals(attributeData.AttributeClass, packetByRefTypeAttributeSymbol))
                    {
                        packetType = attributeData.ConstructorArguments[0].Value as ITypeSymbol;

                        foreach (KeyValuePair<string, TypedConstant> kvp in attributeData.NamedArguments)
                        {
                            string key = kvp.Key;
                            TypedConstant value = kvp.Value;

                            switch (key)
                            {
                                case "Type":
                                {
                                    if (value.Value is int result)
                                    {
                                        type = result;
                                    }

                                    break;
                                }
                                case "Parser":
                                {
                                    if (value.Value is bool result && result)
                                    {
                                        type |= 1;
                                    }

                                    break;
                                }
                                case "Handler":
                                {
                                    if (value.Value is bool result && result)
                                    {
                                        type |= 2;
                                    }

                                    break;
                                }
                            }
                        }
                    }
                }

                if (packetType is null)
                {
                    continue;
                }

                if (type != 0)
                {
                    bool parser = (type & 1) != 0;
                    bool handler = (type & 2) != 0;
                    bool both = parser & handler;

                    StringBuilder stringBuilder = new($@"
using Net.Buffers;
using Net.Communication.Incoming.Consumer;
using Net.Sockets.Pipeline.Handler;
namespace {symbol.ContainingNamespace}
{{
    partial class {symbol.Name}{(both ? " : IIncomingPacketConsumer" : string.Empty)}
    {{
        {(both ? $@"public void Read(IPipelineHandlerContext context, ref PacketReader reader)
        {{
            {packetType} value = this.Parse(ref reader);

            this.Handle(context, value);
        }}" : string.Empty)}

        {(parser ? $"public partial {packetType} Parse(ref PacketReader reader);" : string.Empty)}
        {(handler ? $"public partial void Handle(IPipelineHandlerContext context, in {packetType} packet);" : string.Empty)}
    }}
}}");

                    context.AddSource($"{symbol.ContainingNamespace}_{symbol.Name}_IIncomingPacketConsumer.cs", SourceText.From(stringBuilder.ToString(), Encoding.UTF8));
                }
            }
        }

        private sealed class SyntaxReceiver : ISyntaxReceiver
        {
            internal List<ClassDeclarationSyntax> ClassesWithAttributes { get; } = new List<ClassDeclarationSyntax>();

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                if (syntaxNode is ClassDeclarationSyntax classDeclarationSyntax && classDeclarationSyntax.AttributeLists.Count > 0)
                {
                    this.ClassesWithAttributes.Add(classDeclarationSyntax);
                }
            }
        }
    }
}
