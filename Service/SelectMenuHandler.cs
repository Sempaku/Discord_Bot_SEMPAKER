using Discord.WebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNet.Docker.Service
{
    public class SelectMenuHandler
    {
        private readonly Dictionary<string, Func<SocketMessageComponent, Task>> _handlers;

        public SelectMenuHandler()
        {
            _handlers = new Dictionary<string, Func<SocketMessageComponent, Task>>();
        }

        public void AddHandler(string customId, Func<SocketMessageComponent, Task> handler)
        {
            _handlers[customId] = handler;
        }

        public async Task HandleSelectMenu(SocketMessageComponent component)
        {
            string customId = component.Data.CustomId;
            if (_handlers.TryGetValue(customId, out var handler))
            {
                await handler(component);
            }
        }
    }
}