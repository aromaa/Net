using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Net.Buffers;
using Net.Communication.Incoming.Consumer;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Manager;

public abstract partial class PacketManager<T>
{
	private static readonly Func<ModuleBuilder, ConstructorInfo> addIgnoreAccessChecksToAttributeToModule = Type.GetType("System.Reflection.Emit.IgnoreAccessChecksToAttributeBuilder, System.Reflection.DispatchProxy")!.GetMethod("AddToModule",
	[
		typeof(ModuleBuilder)
	])!.CreateDelegate<Func<ModuleBuilder, ConstructorInfo>>();

	//Key: The assembly where the by ref type is implemented
	private static readonly ConditionalWeakTable<Assembly, GeneratedAssemblyData> generatedAssemblies = [];

	private IIncomingPacketConsumer BuildByRefConsumer(Type byRefType, object parser, object handler)
	{
		Type consumerType = GetConsumerType(byRefType, parser.GetType(), handler.GetType());

		return (IIncomingPacketConsumer)Activator.CreateInstance(
			type: consumerType,
			bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic,
			binder: null,
			args:
			[
				parser,
				handler
			],
			culture: null)!;

		Type GetConsumerType(Type type, Type parserType, Type handlerType)
		{
			GeneratedAssemblyData data = GetAssemblyData(type, parserType, handlerType);

			MethodInfo parseMethod = this.GetParserByRefParseMethod(parserType);
			MethodInfo handleMethod = this.GetHandlerByRefHandleMethod(handlerType);

			return data.DefineType(type, parserType, handlerType, parseMethod, handleMethod);
		}

		static GeneratedAssemblyData GetAssemblyData(Type byRefType, Type parserType, Type handlerType)
		{
			GeneratedAssemblyData data = PacketManager<T>.generatedAssemblies.GetValue(byRefType.Assembly, assembly =>
			{
				AssemblyName assemblyName = new($"{parserType.Assembly.GetName().Name}_PacketManager_ByRef");
				AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);

				ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("<Module>");

				ConstructorInfo attribute = PacketManager<T>.addIgnoreAccessChecksToAttributeToModule(moduleBuilder);

				return new GeneratedAssemblyData(assemblyBuilder, moduleBuilder, attribute);
			});

			data.AllowAccessTo(parserType.Assembly);
			data.AllowAccessTo(handlerType.Assembly);

			return data;
		}
	}

	private sealed class GeneratedAssemblyData
	{
		private readonly AssemblyBuilder assemblyBuilder;
		private readonly ModuleBuilder moduleBuilder;

		private readonly ConstructorInfo ignoresAccessChecksToAttribute;

		private readonly HashSet<string> assemblies;

		private readonly Dictionary<(Type, Type, Type), TypeBuilder> mappedTypes;

		internal GeneratedAssemblyData(AssemblyBuilder assemblyBuilder, ModuleBuilder moduleBuilder, ConstructorInfo ignoresAccessChecksToAttribute)
		{
			this.assemblyBuilder = assemblyBuilder;
			this.moduleBuilder = moduleBuilder;

			this.ignoresAccessChecksToAttribute = ignoresAccessChecksToAttribute;

			this.assemblies = [];

			this.mappedTypes = [];
		}

		internal void AllowAccessTo(Assembly assembly)
		{
			lock (this.assemblyBuilder)
			{
				string name = assembly.GetName().Name!;
				if (!this.assemblies.Add(name))
				{
					return;
				}

				this.assemblyBuilder.SetCustomAttribute(new CustomAttributeBuilder(this.ignoresAccessChecksToAttribute,
				[
					name
				]));
			}
		}

		internal Type DefineType(Type byRefType, Type parserType, Type handlerType, MethodInfo parseMethod, MethodInfo handleMethod)
		{
			lock (this.assemblyBuilder)
			{
				if (!this.mappedTypes.TryGetValue((byRefType, parserType, handlerType), out TypeBuilder? type))
				{
					type = this.mappedTypes[(byRefType, parserType, handlerType)] = this.moduleBuilder.DefineType(
						name: $"ByRefConsumer_{byRefType}_{parserType}_{handlerType}",
						attr: TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
						parent: null,
						interfaces:
						[
							typeof(IIncomingPacketConsumer)
						]);

					FieldBuilder parserField = GetFieldBuilder(type, "Parser", parserType);
					FieldBuilder handlerField = GetFieldBuilder(type, "Handler", handlerType);

					GetConstructorBuilder(type, parserField, handlerField);
					GetReadMethodBuilder(byRefType, type, parserField, handlerField, parseMethod, handleMethod);

					static FieldBuilder GetFieldBuilder(TypeBuilder typeBuilder, string fieldName, Type type)
					{
						FieldBuilder fieldBuilder = typeBuilder.DefineField(
							fieldName: fieldName,
							type: type,
							attributes: FieldAttributes.Private | FieldAttributes.InitOnly);

						return fieldBuilder;
					}

					static ConstructorBuilder GetConstructorBuilder(TypeBuilder typeBuilder, FieldBuilder parserField, FieldBuilder handlerField)
					{
						ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
							attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
							callingConvention: CallingConventions.HasThis,
							parameterTypes:
							[
								parserField.FieldType,
								handlerField.FieldType
							]);

						ILGenerator ilGenerator = constructorBuilder.GetILGenerator();
						ilGenerator.Emit(OpCodes.Ldarg_0);
						ilGenerator.Emit(OpCodes.Ldarg_1);
						ilGenerator.Emit(OpCodes.Stfld, parserField);

						ilGenerator.Emit(OpCodes.Ldarg_0);
						ilGenerator.Emit(OpCodes.Ldarg_2);
						ilGenerator.Emit(OpCodes.Stfld, handlerField);

						ilGenerator.Emit(OpCodes.Ret);

						return constructorBuilder;
					}

					static MethodBuilder GetReadMethodBuilder(Type byRefType, TypeBuilder typeBuilder, FieldBuilder parserField, FieldBuilder handlerField, MethodInfo parseMethod, MethodInfo handleMethod)
					{
						MethodBuilder methodBuilder = typeBuilder.DefineMethod(
							name: "Read",
							attributes: MethodAttributes.Public | MethodAttributes.Final | MethodAttributes.HideBySig | MethodAttributes.NewSlot | MethodAttributes.Virtual,
							callingConvention: CallingConventions.HasThis,
							returnType: null,
							parameterTypes:
							[
								typeof(IPipelineHandlerContext),
								typeof(PacketReader).MakeByRefType()
							]);

						ILGenerator ilGenerator = methodBuilder.GetILGenerator();

						LocalBuilder byRefTypeLocal = ilGenerator.DeclareLocal(byRefType);

						ilGenerator.Emit(OpCodes.Ldarg_0);
						ilGenerator.Emit(OpCodes.Ldfld, parserField);
						ilGenerator.Emit(OpCodes.Ldarg_2);
						ilGenerator.Emit(OpCodes.Call, parseMethod);
						ilGenerator.Emit(OpCodes.Stloc_0);

						ilGenerator.Emit(OpCodes.Ldarg_0);
						ilGenerator.Emit(OpCodes.Ldfld, handlerField);
						ilGenerator.Emit(OpCodes.Ldarg_1);
						ilGenerator.Emit(OpCodes.Ldloca_S, byRefTypeLocal);
						ilGenerator.Emit(OpCodes.Call, handleMethod);

						ilGenerator.Emit(OpCodes.Ret);

						return methodBuilder;
					}
				}

				return type.CreateType()!;
			}
		}
	}
}
