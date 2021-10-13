// <copyright file="Program.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using CommandLine;
using CommandLine.Text;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Datadog.Trace.Tools.Runner
{
    internal class Program
    {
        private static CancellationTokenSource _tokenSource = new CancellationTokenSource();

        private static string RunnerFolder { get; set; }

        private static Platform Platform { get; set; }

        private static int Main(string[] args)
        {
            AnsiConsole.Write(
                new FigletText("Datadog")
                    .LeftAligned()
                    .Color(Color.Purple));

            var app = new CommandApp();
            app.SetDefaultCommand<OptionCommand>();
            app.Configure(cfg => cfg.SetApplicationName("dd-trace"));
            return app.Run(args);

            /*
            // Initializing
            RunnerFolder = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(RunnerFolder))
            {
                RunnerFolder = Path.GetDirectoryName(Environment.GetCommandLineArgs().FirstOrDefault());
            }

            if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                Platform = Platform.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                Platform = Platform.Linux;
            }
            else if (RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                Platform = Platform.MacOS;
            }
            else
            {
                Console.Error.WriteLine("The current platform is not supported. Supported platforms are: Windows, Linux and MacOS.");
                Environment.Exit(-1);
                return;
            }

            // ***

            Console.CancelKeyPress += Console_CancelKeyPress;
            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
            AppDomain.CurrentDomain.DomainUnload += CurrentDomain_ProcessExit;

            Parser parser = new Parser(settings =>
            {
                settings.AutoHelp = true;
                settings.AutoVersion = true;
                settings.EnableDashDash = true;
                settings.HelpWriter = null;
            });

            ParserResult<Options> result = parser.ParseArguments<Options>(args);
            Environment.ExitCode = result.MapResult(ParsedOptions, errors => ParsedErrors(result, errors));
            */
        }

        private static int ParsedOptions(Options options)
        {
            string[] args = options.Value.ToArray();

            // Start logic

            Dictionary<string, string> profilerEnvironmentVariables = Utils.GetProfilerEnvironmentVariables(RunnerFolder, Platform, options);
            if (profilerEnvironmentVariables is null)
            {
                return 1;
            }

            // We try to autodetect the CI Visibility Mode
            if (!options.EnableCIVisibilityMode)
            {
                // Support for VSTest.Console.exe and dotcover
                if (args.Length > 0 && (
                    string.Equals(args[0], "VSTest.Console", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(args[0], "dotcover", StringComparison.OrdinalIgnoreCase)))
                {
                    options.EnableCIVisibilityMode = true;
                }

                // Support for dotnet test and dotnet vstest command
                if (args.Length > 1 && string.Equals(args[0], "dotnet", StringComparison.OrdinalIgnoreCase))
                {
                    if (string.Equals(args[1], "test", StringComparison.OrdinalIgnoreCase) ||
                       string.Equals(args[1], "vstest", StringComparison.OrdinalIgnoreCase))
                    {
                        options.EnableCIVisibilityMode = true;
                    }
                }
            }

            if (options.EnableCIVisibilityMode)
            {
                // Enable CI Visibility mode by configuration
                profilerEnvironmentVariables[Configuration.ConfigurationKeys.CIVisibilityEnabled] = "1";
            }

            if (options.SetEnvironmentVariables)
            {
                Console.WriteLine("Setting up the environment variables.");
                CIConfiguration.SetupCIEnvironmentVariables(profilerEnvironmentVariables);
            }
            else if (!string.IsNullOrEmpty(options.CrankImportFile))
            {
                return Crank.Importer.Process(options.CrankImportFile);
            }
            else
            {
                string cmdLine = string.Join(' ', args);
                if (!string.IsNullOrWhiteSpace(cmdLine))
                {
                    Console.WriteLine("Running: " + cmdLine);

                    ProcessStartInfo processInfo = Utils.GetProcessStartInfo(args[0], Environment.CurrentDirectory, profilerEnvironmentVariables);
                    if (args.Length > 1)
                    {
                        processInfo.Arguments = string.Join(' ', args.Skip(1).ToArray());
                    }

                    return Utils.RunProcess(processInfo, _tokenSource.Token);
                }
            }

            return 0;
        }

        private static int ParsedErrors(ParserResult<Options> result, IEnumerable<Error> errors)
        {
            HelpText helpText = null;
            int exitCode = 1;
            if (errors.IsVersion())
            {
                helpText = HelpText.AutoBuild(result);
                exitCode = 0;
            }
            else
            {
                helpText = HelpText.AutoBuild(
                    result,
                    h =>
                    {
                        h.Heading = "Datadog APM Auto-instrumentation Runner";
                        h.AddNewLineBetweenHelpSections = true;
                        h.AdditionalNewLineAfterOption = false;
                        return h;
                    },
                    e =>
                    {
                        return e;
                    });
            }

            Console.WriteLine(helpText);
            return exitCode;
        }

        private static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            e.Cancel = true;
            _tokenSource.Cancel();
        }

        private static void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            _tokenSource.Cancel();
        }

        public class OptionSettings : CommandSettings
        {
            [Description("Command to be wrapped by the cli tool.")]
            [CommandArgument(0, "[Command]")]
            public string Command { get; set; }

            [Description("Setup the clr profiler environment variables for the CI job and exit. (only supported in Azure Pipelines)")]
            [CommandOption("--set-ci")]
            [DefaultValue(false)]
            public bool SetCI { get; set; }

            [Description("Run the command in CI Visibility Mode")]
            [CommandOption("--ci-visibility")]
            [DefaultValue(false)]
            public bool CIVisibility { get; set; }

            [Description("Sets the environment name for the unified service tagging.")]
            [CommandOption("--dd-env <Environment>")]
            public string Environment { get; set; }

            [Description("Sets the service name for the unified service tagging.")]
            [CommandOption("--dd-service <ServiceName>")]
            public string Service { get; set; }

            [Description("Sets the version name for the unified service tagging.")]
            [CommandOption("--dd-version <Version>")]
            public string Version { get; set; }

            [Description("Datadog trace agent url.")]
            [CommandOption("--agent-url <Url>")]
            public string AgentUrl { get; set; }

            [Description("Sets the tracer home folder path.")]
            [CommandOption("--tracer-home <Path>")]
            public string TracerHomeFolder { get; set; }

            [Description("Sets environment variables to the target command.")]
            [CommandOption("--env-vars <Values>")]
            public string EnvironmentValues { get; set; }

            [Description("Import crank Json results file.")]
            [CommandOption("--crank-import <FilePath>")]
            public string CrankImportFile { get; set; }
        }

        public class OptionCommand : Command<OptionSettings>
        {
            public override int Execute(CommandContext context, OptionSettings settings)
            {
                return 0;
            }
        }
    }
}
