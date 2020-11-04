using System;
using System.Collections.Generic;
using System.Text;

namespace SendMessageServiceBus.ServiceBus
{
    class ServiceBusMessage : Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Events.IntegrationEvent
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public string Content { get; set; }
    }
}
