using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Communication.Incoming.Consumer;
using Net.Sockets.Pipeline.Handler;

namespace Net.Communication.Manager
{
    public abstract partial class PacketManager<T>
    {
        private IIncomingPacketConsumer BuildByRefConsumer(Type byRefType, object parser, object handler)
        {
            MethodInfo parseMethod = this.GetParserByRefParseMethod(parser.GetType());
            MethodInfo handleMethod = this.GetHandlerByRefHandleMethod(handler.GetType());

            TypeBuilder typeBuilder = GetTypeBuilder(byRefType);

            FieldBuilder parserField = GetFieldBuilder(typeBuilder, "Parser", parser.GetType());
            FieldBuilder handlerField = GetFieldBuilder(typeBuilder, "Handler", handler.GetType());

            ConstructorBuilder constructor = GetConstructorBuilder(typeBuilder, parserField, handlerField);

            GetReadMethodBuilder(byRefType, typeBuilder, parserField, handlerField, parseMethod, handleMethod);

            Type type = typeBuilder.CreateType()!;

            return (IIncomingPacketConsumer)Activator.CreateInstance(
                type: type, 
                bindingAttr: BindingFlags.Instance | BindingFlags.NonPublic, 
                binder: null, 
                args: new[]
                {
                    parser,
                    handler
                },
                culture: null
            )!;

            static TypeBuilder GetTypeBuilder(Type byRefType)
            {
                AssemblyName assemblyName = new AssemblyName("PacketManagerByRefAssembly");
                AssemblyBuilder assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
                ModuleBuilder moduleBuilder = assemblyBuilder.DefineDynamicModule("<Module>");

                TypeBuilder tb = moduleBuilder.DefineType(
                    name: byRefType + "ByRefConsumer",
                    attr: TypeAttributes.Class | TypeAttributes.NotPublic | TypeAttributes.AutoClass | TypeAttributes.AnsiClass | TypeAttributes.Sealed | TypeAttributes.BeforeFieldInit,
                    parent: null,
                    interfaces: new[]
                    {
                        typeof(IIncomingPacketConsumer)
                    }
                );

                return tb;
            }

            static FieldBuilder GetFieldBuilder(TypeBuilder typeBuilder, string fieldName, Type type)
            {
                FieldBuilder fieldBuilder = typeBuilder.DefineField(
                    fieldName: fieldName,
                    type: type,
                    attributes: FieldAttributes.Private | FieldAttributes.InitOnly
                );

                return fieldBuilder;
            }

            static ConstructorBuilder GetConstructorBuilder(TypeBuilder typeBuilder, FieldBuilder parserField, FieldBuilder handlerField)
            {
                ConstructorBuilder constructorBuilder = typeBuilder.DefineConstructor(
                    attributes: MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.SpecialName | MethodAttributes.RTSpecialName,
                    callingConvention: CallingConventions.HasThis,
                    parameterTypes: new[]
                    {
                        parserField.FieldType,
                        handlerField.FieldType
                    }
                );

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
                    parameterTypes: new[]
                    {
                        typeof(IPipelineHandlerContext),
                        typeof(PacketReader).MakeByRefType()
                    }
                );
                
                ILGenerator ilGenerator = methodBuilder.GetILGenerator();

                LocalBuilder byRefTypeLocal = ilGenerator.DeclareLocal(byRefType);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldfld, parserField);
                ilGenerator.Emit(OpCodes.Ldarg_2);
                ilGenerator.Emit(OpCodes.Call, parseMethod); //ITS FINE! Up to here
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
    }
}
