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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyModel;
using Microsoft.DotNet.PlatformAbstractions;

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

        public Startup()
        {
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

        public void ConfigureServices(IServiceCollection services)
        {
            var scriptAssembly = _scriptResult.GetScriptAssembly();
            var manager = new ApplicationPartManager();
            manager.ApplicationParts.Add(new AssemblyPart(scriptAssembly));
            manager.FeatureProviders.Add(new ScriptControllerFeatureProvider());
            services.AddSingleton(manager);

            var mvcBuilder = _scriptHost.ConfigureServices?.Invoke(services)?.AddControllersAsServices();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            _scriptHost.ConfigureApp?.Invoke(app, env);
        }
    }
}
