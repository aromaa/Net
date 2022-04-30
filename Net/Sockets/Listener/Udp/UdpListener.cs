using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Net.Buffers;
using Net.Buffers.Extensions;
using Net.Sockets.Async;
using Net.Sockets.Udp;
using Net.Utils;

namespace Net.Sockets.Listener.Udp;

internal sealed class UdpListener : AbstractPipelineSocket, IListener
{
	private readonly int SourceAddressLength;
	private readonly int SourceHeaderLength;

	internal UdpListener(IPEndPoint endPoint)
		: base(UdpListener.CreateSocket(endPoint))
	{
		this.SourceAddressLength = SocketUtils.GetAddressFamilyLength(this.Socket.AddressFamily);
		this.SourceHeaderLength = this.SourceAddressLength + sizeof(ushort) + sizeof(ushort); //Address + Port + Length
	}

	internal void StartListening()
	{
		this.Prepare();
	}

	private static Socket CreateSocket(IPEndPoint endPoint)
	{
		Socket socket = new(endPoint.Address.AddressFamily, SocketType.Dgram, ProtocolType.Udp);
		socket.Bind(endPoint);

		return socket;
	}

	protected override async Task HandleReceive(PipeWriter writer)
	{
		const int MtuDefault = 1500;

		int mtu = MtuDefault + this.SourceHeaderLength;

		using SocketReceiveAwaitableEventArgs eventArgs = new(AbstractPipelineSocket.PipeOptions.WriterScheduler)
		{
			RemoteEndPoint = IPEndPointUtils.GetNone(this.Socket.AddressFamily)
		};

		while (true)
		{
			Memory<byte> buffer = writer.GetMemory(sizeHint: mtu);

			eventArgs.SetBuffer(buffer.Slice(start: this.SourceHeaderLength));

			read:
			int receivedBytes = this.Socket.ReceiveFromAsync(eventArgs) ? await eventArgs : eventArgs.BytesTransferred;

			switch (eventArgs.SocketError)
			{
				case SocketError.Success:
					break;
				case SocketError.MessageSize:
					goto read; //Re-use the buffer!
				default:
					this.Disconnect($"Receive error: {eventArgs.SocketError}");
					return;
			}

			//Assume, assume
			WriteEndPoint(buffer.Span, (IPEndPoint)eventArgs.RemoteEndPoint!, receivedBytes);

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			void WriteEndPoint(Span<byte> endPointSpan, IPEndPoint ipEndPoint, int receivedBytes)
			{
				ipEndPoint.Address.TryWriteBytes(endPointSpan, out _);

				ref byte portNumberIndex = ref MemoryMarshal.GetReference(endPointSpan);

				//Depends on CPU endianness! Remember to read correctly!
				Unsafe.As<byte, int>(ref Unsafe.Add(ref portNumberIndex, this.SourceAddressLength)) = ipEndPoint.Port | (receivedBytes << 16);
			}

			writer.Advance(receivedBytes + this.SourceHeaderLength);

			FlushResult flushResult = await writer.FlushAsync().ConfigureAwait(false);
			if (flushResult.IsCompleted || flushResult.IsCanceled)
			{
				break;
			}
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	protected override void ProcessIncomingData(ref PacketReader reader)
	{
		ReadOnlySequence<byte> sourceAddress = reader.ReadBytes(this.SourceAddressLength);
            
		ReadU(ref reader.GetReaderRef(), out ushort port);
		ReadU(ref reader.GetReaderRef(), out ushort length);

		SocketAddress socketAddress = new(sourceAddress, port);
		DatagramPacket datagram = new(socketAddress, reader.ReadBytes(length));

		this.Pipeline.Read(ref datagram);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void ReadU(ref SequenceReader<byte> reader, out ushort value)
		{
			Unsafe.SkipInit(out value);

			Read(ref reader, out Unsafe.As<ushort, short>(ref value));

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			static void Read(ref SequenceReader<byte> reader, out short value)
			{
				if (BitConverter.IsLittleEndian)
				{
					reader.TryReadLittleEndian(out value);
				}
				else
				{
					reader.TryReadBigEndian(out value);
				}
			}
		}
	}
}