using Sentry.Protocol;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using CompressionLevel = System.IO.Compression.CompressionLevel;

namespace Sentry.Unity
{
    // https://94677106febe46b88b9b9ae5efd18a00@o447951.ingest.sentry.io/5439417
    public static class SentryInitialization
    {
        private static long _timeLastError;
        public static long MinTime { get; } = TimeSpan.FromMilliseconds(500).Ticks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Init()
        {
            if (!(Resources.Load("Sentry/SentryOptions") is UnitySentryOptions options))
            {
                Debug.LogWarning("Sentry Options asset not found. Did you configure it on Component/Sentry?");
                return;
            }

            if (!options.Enabled)
            {
                options.Logger?.Log(SentryLevel.Debug, "Disabled In Options.");
                return;
            }

            if (!options.CaptureInEditor && Application.isEditor)
            {
                options.Logger?.Log(SentryLevel.Info, "Disabled while in the Editor.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.Dsn))
            {
                options.Logger?.Log(SentryLevel.Warning, "No Sentry DSN configured. Sentry will be disabled.");
                return;
            }

            Init(options);
        }

        private static void Init(UnitySentryOptions options)
        {
            _ = SentrySdk.Init(o =>
            {
                o.Dsn = options.Dsn;
                // TODO: StackTraceMode.Original didn't help for Path.GetFileName(), https://github.com/getsentry/sentry-unity/issues/46
                o.StackTraceMode = StackTraceMode.Original;
                
                if (options.Logger != null)
                {
                    o.Debug = true;
                    o.DiagnosticLogger = options.Logger;
                    // o.DiagnosticsLevel = options.DiagnosticsLevel;
                }

                o.SampleRate = options.SampleRate;

                // Uses the game `version` as Release
                o.Release = options.Release is { } release
                    ? release
                    : Application.version;

                o.Environment = options.Environment is { } environment
                    ? environment
                    : Application.isEditor
                        ? "editor"
#if DEVELOPMENT_BUILD
                        : "development";
#else
                        : "production";
#endif

                // If PDBs are available, CaptureMessage also includes a stack trace
                o.AttachStacktrace = options.AttachStacktrace;

                // Required configurations to integrate with Unity
                o.AddInAppExclude("UnityEngine");
                o.AddInAppExclude("UnityEditor");

                o.RequestBodyCompressionLevel = options.RequestBodyCompressionLevel switch
                {
                    SentryUnityCompression.Fastest => CompressionLevel.Fastest,
                    SentryUnityCompression.Optimal => CompressionLevel.Optimal,
                    // The target platform is known when building the player, so 'auto' should resolve there.
                    // Since some platforms don't support GZipping fallback no no compression.
                    SentryUnityCompression.Auto => CompressionLevel.NoCompression,
                    SentryUnityCompression.NoCompression => CompressionLevel.NoCompression,
                    _ => CompressionLevel.NoCompression
                };
                o.AddEventProcessor(new UnityEventProcessor());
                o.AddExceptionProcessor(new UnityEventExceptionProcessor());
            });

            // TODO: Consider ensuring this code path doesn't require UI thread
            // Then use logMessageReceivedThreaded instead
            void OnApplicationOnLogMessageReceived(string condition, string stackTrace, LogType type) => OnLogMessageReceived(condition, stackTrace, type, options);

            Application.logMessageReceived += OnApplicationOnLogMessageReceived;

            Application.quitting += () =>
            {
                // Note: iOS applications are usually suspended and do not quit. You should tick "Exit on Suspend" in Player settings for iOS builds to cause the game to quit and not suspend, otherwise you may not see this call.
                //   If "Exit on Suspend" is not ticked then you will see calls to OnApplicationPause instead.
                // Note: On Windows Store Apps and Windows Phone 8.1 there is no application quit event. Consider using OnApplicationFocus event when focusStatus equals false.
                // Note: On WebGL it is not possible to implement OnApplicationQuit due to nature of the browser tabs closing.
                Application.logMessageReceived -= OnApplicationOnLogMessageReceived;
                SentrySdk.Close();
            };

            IDictionary<string, string>? data = null;
            if (SceneManager.GetActiveScene().name is { } name)
            {
                data = new Dictionary<string, string> {{"scene", name}};
            }
            SentrySdk.AddBreadcrumb("BeforeSceneLoad", data: data);

            options.Logger?.Log(SentryLevel.Debug, "Complete Sentry SDK initialization.");
        }

        // Happens with Domain Reloading
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void SubsystemRegistration() => SentrySdk.AddBreadcrumb("SubsystemRegistration");

        private static void OnLogMessageReceived(string condition, string stackTrace, LogType type, UnitySentryOptions options)
        {
            var time = DateTime.UtcNow.Ticks;

            if (time - _timeLastError <= MinTime)
            {
                return;
            }

            _timeLastError = time;

            if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert)
            {
                // TODO: MinBreadcrumbLevel
                // options.MinBreadcrumbLevel
                SentrySdk.AddBreadcrumb(condition, level: ToBreadcrumbLevel(type));
                return;
            }

            var evt = new SentryEvent(new UnityLogException(condition, stackTrace));
            evt.SetTag("log.type", ToEventTagType(type));
            _ = SentrySdk.CaptureEvent(evt);
            SentrySdk.AddBreadcrumb(condition, level: ToBreadcrumbLevel(type));
        }

        private static string ToEventTagType(LogType type) =>
            type switch
            {
                LogType.Assert => "assert",
                LogType.Error => "error",
                LogType.Exception => "exception",
                LogType.Log => "log",
                LogType.Warning => "warning",
                _ => "unknown"
            };

        private static BreadcrumbLevel ToBreadcrumbLevel(LogType type) =>
            type switch
            {
                LogType.Assert => BreadcrumbLevel.Error,
                LogType.Error => BreadcrumbLevel.Error,
                LogType.Exception => BreadcrumbLevel.Error,
                LogType.Log => BreadcrumbLevel.Info,
                LogType.Warning => BreadcrumbLevel.Warning,
                _ => BreadcrumbLevel.Info
            };
    }
}

