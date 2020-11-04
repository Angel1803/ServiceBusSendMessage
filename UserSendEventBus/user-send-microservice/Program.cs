using Autofac;
using Microsoft.Azure.ServiceBus;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBus.Abstractions;
using Microsoft.eShopOnContainers.BuildingBlocks.EventBusServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendMessageServiceBus.IntegrationEvent;
using SendMessageServiceBus.ServiceBus;
using System;
using System.Collections.Generic;
using System.IO;

namespace SendMessageServiceBus
{
    class Program
    {
        private static IEventBus eventBus;
        private static ServiceCollection services;
        public ILifetimeScope AutofacContainer { get; private set; }

        static void Main(string[] args)
        {
            //Asignamos services al ServiceCollection a
            services = new ServiceCollection();
            //Llamamos al método para configurar la conexión
            ConfigurationServiceBusConnection(services);

            //Configuracion del eventBus
            var serviceProvider = services.BuildServiceProvider();
            eventBus = serviceProvider.GetRequiredService<IEventBus>();

            SendUserMessage();
        }

        //Configuracion del ServiceBus
        public static void ConfigurationServiceBusConnection(IServiceCollection services)
        {
            //Configuracion para leer el appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

            //Se obtiene la configuracion de appsettings.json para la conexion
            var serviceBusConnectionString = builder.GetSection("EventBusConnection").Value;

            //Comenzamos la configuración del servicio Service Bus

            services.AddLogging().AddSingleton<IServiceBusPersisterConnection>(sp =>
            {
                //Obtiene el GetRequiredService inyectando una interfaz que tiene una clase genérica.
                var logger = sp.GetRequiredService<ILogger<DefaultServiceBusPersisterConnection>>();
                //Se inicia la conexion implementando la configuracion obtenida a la clase ServiceBusConectionStringBuilder
                var serviceBusConnection = new ServiceBusConnectionStringBuilder(serviceBusConnectionString);
                //Va a instanciar el DefaultServiceBusPersisterConnection con la configuracion
                return new DefaultServiceBusPersisterConnection(serviceBusConnection, logger);
            });

            RegisterEventBus(services);
        }

        //Registro del EventBus
        private static void RegisterEventBus(IServiceCollection services)
        {
            //Configuracion para leer el appsettings.json
            var builder = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).Build();

            //Obtengo el nombre del cliente suscriptor desde el appsettings.json
            var subscriptionClientName = builder.GetSection("TopicName").Value;

            services.AddSingleton<IEventBus, EventBusServiceBus>(sp =>
            {
                var serviceBusPersisterConnection = sp.GetRequiredService<IServiceBusPersisterConnection>();
                var iLifetimeScope = sp.GetService<ILifetimeScope>();
                var logger = sp.GetRequiredService<ILogger<EventBusServiceBus>>();
                var eventBusSubcriptionsManager = sp.GetRequiredService<IEventBusSubscriptionsManager>();

                return new EventBusServiceBus(serviceBusPersisterConnection, logger, eventBusSubcriptionsManager, subscriptionClientName, iLifetimeScope);
            });

            services.AddSingleton<IEventBusSubscriptionsManager, InMemoryEventBusSubscriptionsManager>();
        }

        public static void SendUserMessage()
        {
            //Lista de datos dummy.
            List<ClienteIntegrationEvent> users = GetData();
            //Serializa el objeto especificado en una cadena JSON.
            var serializeUser = JsonConvert.SerializeObject(users);
            //tipo de mensaje
            string messageType = "UserData";
            //Representa un identificador único global (GUID).
            string messageId = Guid.NewGuid().ToString();

            //Se asignan los valores a las propiedades de ServiceBusMessage
            var message = new ServiceBusMessage
            {
                Id = messageId,
                Type = messageType,
                Content = serializeUser
            };
            eventBus.Publish(message);
            Console.WriteLine("Mensaje enviado: " + message.Content.ToString());
        }

        private static List<ClienteIntegrationEvent> GetData()
        {
            ClienteIntegrationEvent user = new ClienteIntegrationEvent();
            List<ClienteIntegrationEvent> lstUsers = new List<ClienteIntegrationEvent>();
            
            user = new ClienteIntegrationEvent();
            user.Id = 4;
            user.Name = "Carlos Flores";
            user.Edad = 23;
            user.Profesion = "ISC";

            lstUsers.Add(user);

            return lstUsers;
        }
    }
}
