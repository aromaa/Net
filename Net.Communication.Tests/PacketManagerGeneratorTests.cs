using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using Net.Buffers;
using Net.Communication.Attributes;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Manager;
using Net.Communication.Outgoing;
using Net.Sockets.Pipeline.Handler;
using Xunit;

namespace Net.Communication.Tests;

public partial class PacketManagerGeneratorTests
{
	[Fact]
	public void HasCorrectPacketData()
	{
		PacketManagerData<uint> data = PacketManagerGeneratorTests.GetTestPacketManagerData();

		Assert.Equal([
			new PacketManagerData<uint>.ParserData(typeof(TestParserNonGeneric), 1u),
			new PacketManagerData<uint>.ParserData(typeof(TestParserGeneric), 2u, typeof(string)),
			new PacketManagerData<uint>.ParserData(typeof(TestParserGenericConstraint<>), 3u, typeof(IDisposable))
		], data.Parsers.AsEnumerable());

		Assert.Equal([
			new PacketManagerData.HandlerData(typeof(TestHandlerNonGeneric)),
			new PacketManagerData.HandlerData(typeof(TestHandlerGeneric), typeof(string)),
			new PacketManagerData.HandlerData(typeof(TestHandlerGenericConstraint<>), typeof(IDisposable))
		], data.Handlers.AsEnumerable());
		Assert.Equal([
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerNonGeneric), 11u),
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerGeneric), 12u, typeof(string)),
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerGenericConstraint<>), 13u, typeof(IDisposable)),
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerGenericHandler<string, StringComposerDataHandler>), 14u, typeof(StrongBox<string>))
		], data.Composers.AsEnumerable());

		Assert.Equal([
			new PacketManagerData.ComposerHandlerCandidateData(typeof(IComposerDataHandler<>), typeof(StringComposerDataHandler), typeof(IComposerDataHandler<string>))
		], data.ComposerHandlerCandidates.AsEnumerable());
	}

	[Fact]
	public void HasCorrectPacketHandlers()
	{
		PacketManagerData data = PacketManagerGeneratorTests.GetTestPacketManagerHandlerData();

		Assert.Equal([
			new PacketManagerData.HandlerData(typeof(TestHandlerNonGeneric)),
			new PacketManagerData.HandlerData(typeof(TestHandlerGeneric), typeof(string)),
			new PacketManagerData.HandlerData(typeof(TestHandlerGenericConstraint<>), typeof(IDisposable))
		], data.Handlers.AsEnumerable());
	}

	[Fact]
	public void HasCorrectPacketData2()
	{
		PacketManagerData<string> data = PacketManagerGeneratorTests.GetTestStringPacketManagerData();

		Assert.Equal([
			new PacketManagerData<string>.ParserData(typeof(TestStringParser), "TEST"),
		], data.Parsers.AsEnumerable());
	}

	[Fact]
	public void MissingComposerHandlerData()
	{
		PacketManagerData<uint> data = PacketManagerGeneratorTests.GetTestGenericComposerHandlerMissingData();

		Assert.Equal([
			new PacketManagerData<uint>.ComposerData(typeof(TestGenericComposerHandlerMissing.TestComposer<,>), 8, typeof(StrongBox<>))
		], data.Composers.AsEnumerable());
	}

	[Fact]
	public void ComposerDataHandlerOnlyData()
	{
		PacketManagerData data = PacketManagerGeneratorTests.GetIntComposerDataHandlerPacketManagerData();

		Assert.Equal([
			new PacketManagerData.ComposerHandlerCandidateData(typeof(IComposerDataHandler<>), typeof(IntComposerDataHandler), typeof(IComposerDataHandler<int>))
		], data.ComposerHandlerCandidates.AsEnumerable());
	}

	[PacketManagerGenerator(typeof(TestPacketManager))]
	private static partial PacketManagerData<uint> GetTestPacketManagerData();

	[PacketManagerGenerator(typeof(TestPacketManager))]
	private static partial PacketManagerData GetTestPacketManagerHandlerData();

	private sealed class TestPacketManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider);

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketParserId(1u)]
	private sealed class TestParserNonGeneric : IIncomingPacketParser
	{
		[return: NotNull]
		public T Parse<T>(ref PacketReader reader) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketParserId(2u)]
	private sealed class TestParserGeneric : IIncomingPacketParser<string>
	{
		public string Parse(ref PacketReader reader) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketParserId(3u)]
	private sealed class TestParserGenericConstraint<T> : IIncomingPacketParser<T>
		where T : IDisposable
	{
		public T Parse(ref PacketReader reader) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	private sealed class TestHandlerNonGeneric : IIncomingPacketHandler
	{
		public void Handle<T>(IPipelineHandlerContext context, in T packet) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	private sealed class TestHandlerGeneric : IIncomingPacketHandler<string>
	{
		public void Handle(IPipelineHandlerContext context, in string packet) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	private sealed class TestHandlerGenericConstraint<T> : IIncomingPacketHandler<T>
		where T : IDisposable
	{
		public void Handle(IPipelineHandlerContext context, in T packet) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketComposerId(11u)]
	private sealed class TestComposerNonGeneric : IOutgoingPacketComposer
	{
		public void Compose<T>(ref PacketWriter writer, in T packet) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketComposerId(12u)]
	private sealed class TestComposerGeneric : IOutgoingPacketComposer<string>
	{
		public void Compose(ref PacketWriter writer, in string packet) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketComposerId(13u)]
	private sealed class TestComposerGenericConstraint<T> : IOutgoingPacketComposer<T>
		where T : IDisposable
	{
		public void Compose(ref PacketWriter writer, in T packet) => throw new NotImplementedException();
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	[PacketComposerId(14u)]
	private sealed class TestComposerGenericHandler<TData, TDataHandler> : IOutgoingPacketComposer<StrongBox<TData>>
		where TDataHandler : IComposerDataHandler<TData>
	{
		public void Compose(ref PacketWriter writer, in StrongBox<TData> packet)
		{
			writer.WriteBytes(TDataHandler.Serialize(packet.Value!));
		}
	}

	private sealed class TestStringPacketManager(IServiceProvider serviceProvider) : PacketManager<string>(serviceProvider);

	[PacketManagerGenerator(typeof(TestStringPacketManager))]
	private static partial PacketManagerData<string> GetTestStringPacketManagerData();

	[PacketManagerRegister(typeof(TestStringPacketManager))]
	[PacketParserId("TEST")]
	private sealed class TestStringParser : IIncomingPacketParser
	{
		[return: NotNull]
		public T Parse<T>(ref PacketReader reader) => throw new NotImplementedException();
	}

	private interface IComposerDataHandler<in T>
	{
		public static abstract byte[] Serialize(T value);
	}

	[PacketManagerRegister(typeof(TestPacketManager))]
	internal sealed class StringComposerDataHandler : IComposerDataHandler<string>
	{
		public static byte[] Serialize(string value) => Encoding.UTF8.GetBytes(value);
	}

	private class TestGenericComposerHandlerMissing(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestGenericComposerHandlerMissing))]
		[PacketComposerId(8u)]
		internal sealed class TestComposer<TData, TDataHandler> : IOutgoingPacketComposer<StrongBox<TData>>
			where TDataHandler : IComposerDataHandler<TData>
		{
			public void Compose(ref PacketWriter writer, in StrongBox<TData> packet)
			{
				writer.WriteBytes(TDataHandler.Serialize(packet.Value!));
			}
		}
	}

	[PacketManagerGenerator(typeof(TestGenericComposerHandlerMissing))]
	private static partial PacketManagerData<uint> GetTestGenericComposerHandlerMissingData();

	private sealed class IntComposerDataHandlerPacketManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider);

	[PacketManagerGenerator(typeof(IntComposerDataHandlerPacketManager))]
	private static partial PacketManagerData<uint> GetIntComposerDataHandlerPacketManagerData();

	[PacketManagerRegister(typeof(IntComposerDataHandlerPacketManager))]
	internal sealed class IntComposerDataHandler : IComposerDataHandler<int>
	{
		public static byte[] Serialize(int value) => BitConverter.GetBytes(value);
	}
}
