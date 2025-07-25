﻿using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Net.Buffers;
using Net.Communication.Attributes;
using Net.Communication.Incoming.Consumer;
using Net.Communication.Incoming.Handler;
using Net.Communication.Incoming.Parser;
using Net.Communication.Manager;
using Net.Communication.Outgoing;
using Net.Sockets.Pipeline.Handler;
using Xunit;

namespace Net.Communication.Tests;

public class PacketManagerTests
{
	private readonly IServiceProvider ServiceProvider = new ServiceCollection().BuildServiceProvider();

	[Fact]
	public void TestParserFound()
	{
		TestParsersManager manager = new(this.ServiceProvider);
		manager.TryGetParser(3, out IIncomingPacketParser? parser);
		manager.TryGetConsumer(3, out IIncomingPacketConsumer? consumer);

		Assert.IsType<TestParsersManager.TestParser>(parser);
		Assert.NotNull(consumer); //Parser without handler has default one to push it to the socket pipeline
	}

	[Fact]
	public void TestParserNonGenericFound()
	{
		TestParsersManager manager = new(this.ServiceProvider);
		manager.TryGetParser(5, out IIncomingPacketParser? parser);
		manager.TryGetConsumer(5, out IIncomingPacketConsumer? consumer);

		Assert.IsType<TestParsersManager.TestParserNonGeneric>(parser);
		Assert.Null(consumer); //Non generic parsers don't have default consumers, not possible to know what type is expected
	}

	[Fact]
	public void TestConsumerFound()
	{
		TestConsumersManager manager = new(this.ServiceProvider);
		manager.TryGetConsumer(6, out IIncomingPacketConsumer? consumer);

		Assert.IsType<TestConsumersManager.TestConsumer>(consumer);
	}

	[Fact]
	public void TestComposerFound()
	{
		TestComposersManager manager = new(this.ServiceProvider);
		manager.TryGetComposer<string>(out IOutgoingPacketComposer? composer, out uint id);

		Assert.IsType<TestComposersManager.TestComposer>(composer);
		Assert.Equal(2u, id);
	}

	[Fact]
	public void TestHandlerFound()
	{
		TestHandlersManager manager = new(this.ServiceProvider);
		manager.TryGetHandler<string>(out IIncomingPacketHandler? handler);

		Assert.IsType<TestHandlersManager.TestHandler>(handler);
	}

	[Fact]
	public void TestParserOnlyConsumerWorks()
	{
		IncomingObjectCatcher catcher = new();

		DummyIPipelineSocket socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(catcher));
		PacketReader reader = default;

		TestParsersManager manager = new(this.ServiceProvider);
		manager.TryConsumePacket(socket.Pipeline.Context, ref reader, 3);

		Assert.Equal("Parser", catcher.Pop());
	}

	[Fact]
	public void TestHandlerWorks()
	{
		IncomingObjectCatcher catcher = new();

		DummyIPipelineSocket socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(catcher));

		TestHandlersManager manager = new(this.ServiceProvider);
		manager.TryHandlePacket(socket.Pipeline.Context, "Handler");

		Assert.Equal("Handler", catcher.Pop());
	}

	[Fact]
	public void TestParserHandlerConsumerWorks()
	{
		IncomingObjectCatcher catcher = new();

		DummyIPipelineSocket socket = DummyIPipelineSocket.Create(socket => socket.Pipeline.AddHandlerFirst(catcher));
		PacketReader reader = default;

		TestParserHandlerManager manager = new(this.ServiceProvider);
		manager.TryConsumePacket(socket.Pipeline.Context, ref reader, 1);

		Assert.Equal("ParserHandler", catcher.Pop());
	}

	[Fact]
	public void TestConsumerWorks()
	{
		using MemoryStream stream = new();

		PacketWriter writer = new(PipeWriter.Create(stream));

		TestComposersManager manager = new(this.ServiceProvider);
		manager.TryComposePacket(ref writer, "Writer", out uint id);

		writer.Dispose();

		Assert.Equal(2u, id);
		Assert.Equal(Encoding.UTF8.GetBytes("Writer"), stream.ToArray());
	}

	[Fact]
	public void TestGenericConsumerWorks()
	{
		using MemoryStream stream = new();

		PacketWriter writer = new(PipeWriter.Create(stream));

		TestGenericComposersManager manager = new(this.ServiceProvider);
		manager.TryComposePacket(ref writer, new StrongBox<string>("Writer"), out uint id);

		writer.Dispose();

		Assert.Equal(3u, id);
		Assert.Equal(Encoding.UTF8.GetBytes("Writer"), stream.ToArray());
	}

	[Fact]
	public void TestGenericConsumerCombineWorks()
	{
		using MemoryStream stream = new();

		PacketWriter writer = new(PipeWriter.Create(stream));

		TestGenericComposerHandlerMissing manager = new(this.ServiceProvider);
		TestGenericComposersCombineManager combineManager = new(this.ServiceProvider, manager);
		combineManager.TryComposePacket(ref writer, new StrongBox<int>(5), out uint id);

		writer.Dispose();

		Assert.Equal(8u, id);
		Assert.Equal(BitConverter.GetBytes(5), stream.ToArray());
	}

	private sealed class TestParsersManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestParsersManager))]
		[PacketParserId(3u)]
		internal sealed class TestParser : IIncomingPacketParser<string>
		{
			public string Parse(ref PacketReader reader) => "Parser";
		}

		[PacketManagerRegister(typeof(TestParsersManager))]
		[PacketParserId(5u)]
		internal sealed class TestParserNonGeneric : IIncomingPacketParser
		{
			[return: NotNull]
			public T Parse<T>(ref PacketReader reader) => throw new NotImplementedException();
		}
	}

	private sealed class TestHandlersManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestHandlersManager))]
		internal sealed class TestHandler : IIncomingPacketHandler<string>
		{
			public void Handle(IPipelineHandlerContext context, in string packet)
			{
				string temp = packet;

				context.ProgressReadHandler(ref temp);
			}
		}
	}

	private sealed class TestParserHandlerManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestParserHandlerManager))]
		[PacketParserId(1u)]
		internal sealed class TestParserHandler : IIncomingPacketParser<string>, IIncomingPacketHandler<string>
		{
			public string Parse(ref PacketReader reader)
			{
				return "ParserHandler";
			}

			public void Handle(IPipelineHandlerContext context, in string packet)
			{
				string temp = packet;

				context.ProgressReadHandler(ref temp);
			}
		}
	}

	private sealed class TestConsumersManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestConsumersManager))]
		[PacketParserId(6u)]
		internal sealed class TestConsumer : IIncomingPacketConsumer
		{
			public void Read(IPipelineHandlerContext context, ref PacketReader reader)
			{
				string value = "Consumer";

				context.ProgressReadHandler(ref value);
			}
		}
	}

	private sealed class TestComposersManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestComposersManager))]
		[PacketComposerId(2u)]
		internal sealed class TestComposer : IOutgoingPacketComposer<string>
		{
			public void Compose(ref PacketWriter writer, in string packet)
			{
				writer.WriteBytes(Encoding.UTF8.GetBytes(packet));
			}
		}
	}

	private class TestGenericComposersManager(IServiceProvider serviceProvider) : PacketManager<uint>(serviceProvider)
	{
		[PacketManagerRegister(typeof(TestGenericComposersManager))]
		[PacketComposerId(3u)]
		internal sealed class TestComposer<TData, TDataHandler> : IOutgoingPacketComposer<StrongBox<TData>>
			where TDataHandler : IComposerDataHandler<TData>
		{
			public void Compose(ref PacketWriter writer, in StrongBox<TData> packet)
			{
				writer.WriteBytes(TDataHandler.Serialize(packet.Value!));
			}
		}

		internal interface IComposerDataHandler<in T>
		{
			public static abstract byte[] Serialize(T value);
		}

		[PacketManagerRegister(typeof(TestGenericComposersManager))]
		internal sealed class StringComposerDataHandler : IComposerDataHandler<string>
		{
			public static byte[] Serialize(string value) => Encoding.UTF8.GetBytes(value);
		}
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

		internal interface IComposerDataHandler<in T>
		{
			public static abstract byte[] Serialize(T value);
		}
	}

	private sealed class TestGenericComposersCombineManager : PacketManager<uint>
	{
		internal TestGenericComposersCombineManager(IServiceProvider serviceProvider, PacketManager<uint> other)
			: base(serviceProvider)
		{
			this.Combine(other);
		}

		[PacketManagerRegister(typeof(TestGenericComposersCombineManager))]
		internal sealed class IntComposerDataHandler : TestGenericComposerHandlerMissing.IComposerDataHandler<int>
		{
			public static byte[] Serialize(int value) => BitConverter.GetBytes(value);
		}
	}
}
