// <copyright file="LiveDebugger.cs" company="Datadog">
// Unless explicitly stated otherwise all files in this repository are licensed under the Apache 2 License.
// This product includes software developed at Datadog (https://www.datadoghq.com/). Copyright 2017 Datadog, Inc.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Datadog.Trace.Agent;
using Datadog.Trace.Agent.DiscoveryService;
using Datadog.Trace.Debugger.Configurations;
using Datadog.Trace.Debugger.Configurations.Models;
using Datadog.Trace.Debugger.Helpers;
using Datadog.Trace.Debugger.Models;
using Datadog.Trace.Debugger.PInvoke;
using Datadog.Trace.Debugger.ProbeStatuses;
using Datadog.Trace.Debugger.Sink;
using Datadog.Trace.Logging;
using Datadog.Trace.RemoteConfigurationManagement;
using Datadog.Trace.Vendors.Newtonsoft.Json;

namespace Datadog.Trace.Debugger
{
    internal class LiveDebugger
    {
        private static readonly IDatadogLogger Log = DatadogLogging.GetLoggerFor(typeof(LiveDebugger));
        private static readonly object GlobalLock = new();

        private readonly ImmutableDebuggerSettings _settings;
        private readonly IDiscoveryService _discoveryService;
        private readonly IRemoteConfigurationManager _remoteConfigurationManager;
        private readonly IDebuggerSink _debuggerSink;
        private readonly ILineProbeResolver _lineProbeResolver;
        private readonly List<ProbeDefinition> _unboundProbes;
        private readonly IProbeStatusPoller _probeStatusPoller;
        private readonly ConfigurationUpdater _configurationUpdater;
        private readonly object _instanceLock = new();
        private bool _isInitialized;

        private LiveDebugger(
            ImmutableDebuggerSettings settings,
            string serviceName,
            IDiscoveryService discoveryService,
            IRemoteConfigurationManager remoteConfigurationManager,
            ILineProbeResolver lineProbeResolver,
            IDebuggerSink debuggerSink,
            IProbeStatusPoller probeStatusPoller,
            ConfigurationUpdater configurationUpdater)
        {
            _settings = settings;
            _discoveryService = discoveryService;
            _lineProbeResolver = lineProbeResolver;
            _debuggerSink = debuggerSink;
            _probeStatusPoller = probeStatusPoller;
            _remoteConfigurationManager = remoteConfigurationManager;
            _configurationUpdater = configurationUpdater;
            _unboundProbes = new List<ProbeDefinition>();
            ServiceName = serviceName;
        }

        public static LiveDebugger Instance { get; private set; }

        public static Product Product { get; } = new("LIVE_DEBUGGING");

        public string ServiceName { get; }

        public static LiveDebugger Create(
            ImmutableDebuggerSettings settings,
            string serviceName,
            IDiscoveryService discoveryService,
            IRemoteConfigurationManager remoteConfigurationManager,
            ILineProbeResolver lineProbeResolver,
            IDebuggerSink debuggerSink,
            IProbeStatusPoller probeStatusPoller,
            ConfigurationUpdater configurationUpdater)
        {
            lock (GlobalLock)
            {
                return Instance ??= new LiveDebugger(settings, serviceName, discoveryService, remoteConfigurationManager, lineProbeResolver, debuggerSink, probeStatusPoller, configurationUpdater);
            }
        }

        public async Task InitializeAsync()
        {
            if (!ShouldInitialize())
            {
                // Log.Information("Live Debugger is disabled. To enable it, please set DD_DEBUGGER_ENABLED environment variable to 'true'.");
                return;
            }

            try
            {
                Log.Information("Live Debugger initialization started");

                _remoteConfigurationManager.RegisterProduct(Product);

                Product.ConfigChanged += (sender, args) => AcceptConfiguration(args);
                AppDomain.CurrentDomain.AssemblyLoad += (sender, args) => CheckUnboundProbes();
                AppDomain.CurrentDomain.DomainUnload += (sender, args) => _lineProbeResolver.OnDomainUnloaded();

                await StartAsync().ConfigureAwait(false);
            }
            catch (Exception e)
            {
                Log.Error(e, "Initializing Live Debugger failed.");
            }

            bool ShouldInitialize()
            {
                lock (GlobalLock)
                {
                    if (_isInitialized)
                    {
                        Log.Information("Live Debugger is already initialized.");
                        return false;
                    }

                    if (!_settings.Enabled)
                    {
                        Log.Information("Live Debugger is disabled. To enable it, please set DD_DEBUGGER_ENABLED environment variable to 'true'.");
                        return false;
                    }

                    if (_settings.TransportType != TracesTransportType.Default)
                    {
                        Log.Information("Live Debugger is not supported on UDS or named pipelines.");
                        return false;
                    }

                    var isConfigurationSupported = !string.IsNullOrWhiteSpace(_discoveryService.ConfigurationEndpoint);
                    if (!isConfigurationSupported)
                    {
                        Log.Warning("You must upgrade datadog-agent in order to leverage the Live Debugger. All debugging features will be disabled.");
                        return false;
                    }

                    _isInitialized = true;
                    return true;
                }
            }

            Task StartAsync()
            {
                LifetimeManager.Instance.AddShutdownTask(OnShutdown);

                _probeStatusPoller.StartPolling();
                return _debuggerSink.StartFlushingAsync();
            }

            void OnShutdown()
            {
                _debuggerSink.Dispose();
                _probeStatusPoller.Dispose();
            }
        }

        internal void UpdateProbeInstrumentations(IReadOnlyList<ProbeDefinition> addedProbes, IReadOnlyList<ProbeDefinition> removedProbes)
        {
            lock (_instanceLock)
            {
                if (addedProbes.Count == 0 && removedProbes.Count == 0)
                {
                    return;
                }

                Log.Information($"Live Debugger.InstrumentProbes: Request to instrument {addedProbes.Count} probes definitions and remove {removedProbes.Count} definitions");

                var methodProbes = new List<NativeMethodProbeDefinition>();
                var lineProbes = new List<NativeLineProbeDefinition>();
                foreach (var probe in addedProbes)
                {
                    switch (GetProbeLocationType(probe))
                    {
                        case ProbeLocationType.Line:
                            var lineProbeResult = _lineProbeResolver.TryResolveLineProbe(probe, out var location);

                            var status = lineProbeResult.Status;
                            var message = lineProbeResult.Message;

                            switch (status)
                            {
                                case LiveProbeResolveStatus.Bound:
                                    lineProbes.Add(new NativeLineProbeDefinition(location.ProbeDefinition.Id, location.MVID, location.MethodToken, (int)(location.BytecodeOffset), location.LineNumber, location.ProbeDefinition.Where.SourceFile));
                                    break;
                                case LiveProbeResolveStatus.Unbound:
                                    Log.Information(message);
                                    _unboundProbes.Add(probe);
                                    break;
                                case LiveProbeResolveStatus.Error:
                                    Log.Error(message);
                                    AddErrorProbeStatus(probe.Id, errorMessage: message);
                                    break;
                            }

                            break;
                        case ProbeLocationType.Method:
                            var nativeDefinition = new NativeMethodProbeDefinition(probe.Id, probe.Where.TypeName, probe.Where.MethodName, probe.Where.Signature?.Split(separator: ','));
                            methodProbes.Add(nativeDefinition);
                            break;
                        case ProbeLocationType.Unrecognized:
                            break;
                    }
                }

                var revertProbes = removedProbes.Select(probe => new NativeRemoveProbeRequest(probe.Id));
                RemoveUnboundProbes(removedProbes);
                using var disposable = new DisposableEnumerable<NativeMethodProbeDefinition>(methodProbes);
                DebuggerNativeMethods.InstrumentProbes(methodProbes.ToArray(), lineProbes.ToArray(), revertProbes.ToArray());

                _probeStatusPoller.AddProbes(addedProbes.Select(probe => probe.Id).ToArray());
                _probeStatusPoller.RemoveProbes(removedProbes.Select(probe => probe.Id).ToArray());

                // This log entry is being checked in integration test
                Log.Information("Live Debugger.InstrumentProbes: Request to instrument probes definitions completed.");
            }
        }

        private ProbeLocationType GetProbeLocationType(ProbeDefinition probe)
        {
            if (!string.IsNullOrEmpty(probe.Where.MethodName))
            {
                return ProbeLocationType.Method;
            }

            if (!string.IsNullOrEmpty(probe.Where.SourceFile))
            {
                return ProbeLocationType.Line;
            }

            return ProbeLocationType.Unrecognized;
        }

        private void RemoveUnboundProbes(IReadOnlyList<ProbeDefinition> removedDefinitions)
        {
            lock (_instanceLock)
            {
                foreach (var probeDefinition in removedDefinitions)
                {
                    _unboundProbes.RemoveAll(m => m.Id == probeDefinition.Id);
                }
            }
        }

        private void CheckUnboundProbes()
        {
            // A new assembly was loaded, so re-examine whether the probe can now be resolved.
            lock (_instanceLock)
            {
                if (_unboundProbes.Count == 0)
                {
                    return;
                }

                foreach (var unboundProbe in _unboundProbes)
                {
                    var result = _lineProbeResolver.TryResolveLineProbe(unboundProbe, out var bytecodeLocation);
                    if (result.Status == LiveProbeResolveStatus.Bound)
                    {
                        // TODO: Install the line probe.
                    }
                }
            }
        }

        private void AcceptConfiguration(ProductConfigChangedEventArgs args)
        {
            var uuid = ServiceName.ToUUID();
            var config = args.NewConfigs.FirstOrDefault(rcmConfig => rcmConfig.Path.Id == ServiceName || rcmConfig.Path.Id == uuid);
            if (config == null)
            {
                Log.Warning("No matching probe configurations found in target paths.");
                return;
            }

            var content = Encoding.UTF8.GetString(config.Contents);
            var probeConfig = JsonConvert.DeserializeObject<ProbeConfiguration>(content);
            _configurationUpdater.Accept(probeConfig);
        }

        internal void AddSnapshot(string snapshot)
        {
            _debuggerSink.AddSnapshot(snapshot);
        }

        internal void AddReceivedProbeStatus(string probeId)
        {
            _debuggerSink.AddReceivedProbeStatus(probeId);
        }

        internal void AddInstalledProbeStatus(string probeId)
        {
            _debuggerSink.AddInstalledProbeStatus(probeId);
        }

        internal void AddBlockedProbeStatus(string probeId)
        {
            _debuggerSink.AddBlockedProbeStatus(probeId);
        }

        internal void AddErrorProbeStatus(string probeId, Exception exception = null, string errorMessage = null)
        {
            _debuggerSink.AddErrorProbeStatus(probeId, exception, errorMessage);
        }
    }
}

internal record BoundLineProbeLocation
{
    public BoundLineProbeLocation(ProbeDefinition probe, Guid mvid, int methodToken, int? bytecodeOffset, int lineNumber)
    {
        ProbeDefinition = probe;
        MVID = mvid;
        MethodToken = methodToken;
        BytecodeOffset = bytecodeOffset;
        LineNumber = lineNumber;
    }

    public ProbeDefinition ProbeDefinition { get; set; }

    public Guid MVID { get; set; }

    public int MethodToken { get; set; }

    public int? BytecodeOffset { get; set; }

    public int LineNumber { get; set; }
}
