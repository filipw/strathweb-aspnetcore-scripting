using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.DotNet.InternalAbstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.DependencyModel;
using Microsoft.Extensions.Logging;

namespace AspNetCore.Scripting
{
    public class Startup
    {
        private IEnumerable<Assembly> _assemblies = new[]
        {
            typeof(object).GetTypeInfo().Assembly,
            typeof(Enumerable).GetTypeInfo().Assembly
        };

        private IEnumerable<string> _namespaces = new[]
        {
            "System",
            "System.IO",
            "System.Linq",
            "System.Collections.Generic",
            "Microsoft.Extensions.DependencyInjection",
            "Microsoft.AspNetCore.Builder"
        };

        public string ScriptFullPath => Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ScriptName);

        public string ScriptName => "app.csx";

        private ScriptState<object> _scriptResult;
        private readonly ScriptingHost _scriptHost = new ScriptingHost();
        private Script<object> _script;

        public Startup(IHostingEnvironment env)
        {
            var builder = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();
            Configuration = builder.Build();

            var code = File.ReadAllText(ScriptFullPath);

            var opts = ScriptOptions.Default.
                AddImports(_namespaces).
                AddReferences(_assemblies).
                AddReferences(typeof(ScriptingHost).GetTypeInfo().Assembly);

            var runtimeId = RuntimeEnvironment.GetRuntimeIdentifier();
            var assemblyNames = DependencyContext.Default.GetRuntimeAssemblyNames(runtimeId);

            foreach (var assemblyName in assemblyNames)
            {
                var assembly = Assembly.Load(assemblyName);
                opts = opts.AddReferences(assembly);
            }

            _script = CSharpScript.Create(code, opts, typeof(ScriptingHost));
            var c = _script.GetCompilation();
            _scriptResult = _script.RunAsync(_scriptHost).Result;
        }

        public IConfigurationRoot Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            var executionStateProperty = _scriptResult.GetType().GetProperty("ExecutionState", BindingFlags.NonPublic | BindingFlags.Instance);
            var executionState = executionStateProperty.GetValue(_scriptResult);

            var submissionStatesField = executionState.GetType().GetField("_submissionStates", BindingFlags.NonPublic | BindingFlags.Instance);
            var submissions = submissionStatesField.GetValue(executionState) as object[];

            var scriptAssembly = submissions[1].GetType().GetTypeInfo().Assembly;

            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(scriptAssembly));
            manager.FeatureProviders.Add(new ScriptControllerFeatureProvider());
            services.AddSingleton(manager);

            var mvcBuilder = _scriptHost.ConfigureServices?.Invoke(services)?.AddControllersAsServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            _scriptHost.ConfigureApp?.Invoke(app, env, loggerFactory);
        }
    }
}
