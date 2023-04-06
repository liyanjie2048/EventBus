global using System;
global using System.IO;
global using System.Net.Sockets;
global using System.Text;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

global using Liyanjie.EventBus;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using Polly;

global using RabbitMQ.Client;
global using RabbitMQ.Client.Events;
global using RabbitMQ.Client.Exceptions;
