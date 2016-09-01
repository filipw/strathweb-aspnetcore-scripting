using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Scripting
{
    public class ScriptingHost
    {
        internal Func<IServiceCollection, IMvcBuilder> ConfigureServices;
        internal Action<IApplicationBuilder, IHostingEnvironment, ILoggerFactory> ConfigureApp;

        public void Services(Func<IServiceCollection, IMvcBuilder> configureServices)
        {
            ConfigureServices = configureServices;
        }

        public void Configure(Action<IApplicationBuilder, IHostingEnvironment, ILoggerFactory> configureApp)
        {
            ConfigureApp = configureApp;
        }
    }
}