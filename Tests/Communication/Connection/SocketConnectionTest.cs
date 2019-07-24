using Net.Connections;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Tests.Communication.Connection
{
    public class SocketConnectionTest
    {
        [Fact]
        public void TestDisconnectEventReRegister()
        {
            SocketConnection connection = new SocketConnection(null);

            Assert.True(connection.TryRegisterDisconnectEvent(Connection_DisconnectEvent));

            connection.DisconnectEvent -= Connection_DisconnectEvent;

            Assert.True(connection.TryRegisterDisconnectEvent(Connection_DisconnectEvent));

            void Connection_DisconnectEvent(SocketConnection connection) => throw new NotImplementedException();
        }
    }
}
