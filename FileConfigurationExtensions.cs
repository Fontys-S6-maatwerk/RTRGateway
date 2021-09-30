using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Ocelot.Configuration.File;

namespace RTRGateway
{
    public static class FileConfigurationExtensions
    {
        public static IServiceCollection ConfigureDownstreamHostAndPortVariables(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.PostConfigure<FileConfiguration>(fileConfiguration => {

                Dictionary<string, Uri> hosts = configuration.GetSection("Hosts").GetChildren()
                    .ToDictionary(x => x.Key, x => new Uri(x.Value));

                fileConfiguration.Routes.ForEach(route => ConfigureRoute(route, hosts));
            });
            return services;
        }

        private static void ConfigureRoute(FileRoute route, Dictionary<string, Uri> hosts)
        {
            route.DownstreamHostAndPorts.ForEach(hostAndPort =>
            {
                var fileHost = hostAndPort.Host;
                if (IsHostVariable(fileHost))
                {
                    string dynamicService = fileHost.TrimStart('{').TrimEnd('}');

                    KeyValuePair<string, Uri> host = hosts.Where(x => x.Key.Equals(dynamicService)).First();

                    route.DownstreamScheme = host.Value.Scheme;
                    hostAndPort.Host = host.Value.Host;
                    hostAndPort.Port = host.Value.Port;
                }
            });
        }

        private static bool IsHostVariable(string host) => host.StartsWith("{") && host.EndsWith("}");
    }
}
