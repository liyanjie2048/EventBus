global using System;
global using System.Text.Json;
global using System.Threading;
global using System.Threading.Tasks;

global using Confluent.Kafka;

global using Liyanjie.EventBus;

global using Microsoft.Extensions.DependencyInjection;
global using Microsoft.Extensions.Logging;
global using Microsoft.Extensions.Options;

global using Polly;
global using Polly.Retry;
