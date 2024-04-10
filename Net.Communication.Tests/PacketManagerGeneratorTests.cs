using System.Diagnostics.CodeAnalysis;
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
			new PacketManagerData<uint>.HandlerData(typeof(TestHandlerNonGeneric)),
			new PacketManagerData<uint>.HandlerData(typeof(TestHandlerGeneric), typeof(string)),
			new PacketManagerData<uint>.HandlerData(typeof(TestHandlerGenericConstraint<>), typeof(IDisposable))
		], data.Handlers.AsEnumerable());
		Assert.Equal([
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerNonGeneric), 11u),
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerGeneric), 12u, typeof(string)),
			new PacketManagerData<uint>.ComposerData(typeof(TestComposerGenericConstraint<>), 13u, typeof(IDisposable))
		], data.Composers.AsEnumerable());
	}

	[PacketManagerGenerator(typeof(TestPacketManager))]
	private static partial PacketManagerData<uint> GetTestPacketManagerData();

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
}
