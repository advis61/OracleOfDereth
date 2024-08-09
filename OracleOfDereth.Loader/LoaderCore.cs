using Decal.Adapter;
using Microsoft.Win32;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace OracleOfDereth
{
    [FriendlyName("Oracle Of Dereth.Loader")]
    public class LoaderCore : FilterBase
    {
        private Assembly pluginAssembly;
        private Type pluginType;
        private object pluginInstance;
        private FileSystemWatcher pluginWatcher;
        private bool isSubscribedToRenderFrame = false;
        private bool needsReload;

        public static string PluginAssemblyNamespace => typeof(LoaderCore).Namespace.Replace(".Loader", "");
        public static string PluginAssemblyName => $"{PluginAssemblyNamespace}.dll";
        public static string PluginAssemblyGuid => "976132b4-57ba-470c-9117-55514470a866";

        public static bool IsPluginLoaded { get; private set; }

        /// <summary>
        /// Assembly directory (contains both loader and plugin dlls)
        /// </summary>
        public static string AssemblyDirectory => System.IO.Path.GetDirectoryName(Assembly.GetAssembly(typeof(LoaderCore)).Location);

        public DateTime LastDllChange { get; private set; }

        #region Event Handlers
        protected override void Startup()
        {
            try
            {
                Core.PluginInitComplete += Core_PluginInitComplete;
                Core.PluginTermComplete += Core_PluginTermComplete;
                Core.FilterInitComplete += Core_FilterInitComplete;

                // watch the AssemblyDirectory for any .dll file changes
                pluginWatcher = new FileSystemWatcher();
                pluginWatcher.Path = AssemblyDirectory;
                pluginWatcher.NotifyFilter = NotifyFilters.LastWrite;
                pluginWatcher.Filter = "*.dll";
                pluginWatcher.Changed += PluginWatcher_Changed;
                pluginWatcher.EnableRaisingEvents = true;
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void Core_FilterInitComplete(object sender, EventArgs e)
        {
            Core.EchoFilter.ClientDispatch += EchoFilter_ClientDispatch;
        }

        private void EchoFilter_ClientDispatch(object sender, NetworkMessageEventArgs e)
        {
            try
            {
                // Login_SendEnterWorldRequest
                if (e.Message.Type == 0xF7C8)
                {
                    //EnsurePluginIsDisabledInRegistry();
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void Core_PluginInitComplete(object sender, EventArgs e)
        {
            try
            {
                LoadPluginAssembly();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void Core_PluginTermComplete(object sender, EventArgs e)
        {
            try
            {
                UnloadPluginAssembly();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        protected override void Shutdown()
        {
            try
            {
                Core.PluginInitComplete -= Core_PluginInitComplete;
                Core.PluginTermComplete -= Core_PluginTermComplete;
                Core.FilterInitComplete -= Core_FilterInitComplete;
                UnloadPluginAssembly();
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void Core_RenderFrame(object sender, EventArgs e)
        {
            try
            {
                if (IsPluginLoaded && needsReload && DateTime.UtcNow - LastDllChange > TimeSpan.FromSeconds(1))
                {
                    needsReload = false;
                    Core.RenderFrame -= Core_RenderFrame;
                    isSubscribedToRenderFrame = false;
                    LoadPluginAssembly();
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void PluginWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            try
            {
                LastDllChange = DateTime.UtcNow;
                needsReload = true;

                if (!isSubscribedToRenderFrame)
                {
                    isSubscribedToRenderFrame = true;
                    Core.RenderFrame += Core_RenderFrame;
                }
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }
        #endregion

        #region Plugin Loading/Unloading
        internal void LoadPluginAssembly()
        {
            try
            {
                if (IsPluginLoaded)
                {
                    UnloadPluginAssembly();
                    try
                    {
                        CoreManager.Current.Actions.AddChatText($"Reloading {PluginAssemblyName}", 1);
                    }
                    catch { }
                }

                pluginAssembly = Assembly.Load(File.ReadAllBytes(System.IO.Path.Combine(AssemblyDirectory, PluginAssemblyName)));
                pluginType = pluginAssembly.GetType($"{PluginAssemblyNamespace}.PluginCore");
                pluginInstance = Activator.CreateInstance(pluginType);

                var assemblyDirAttr = pluginType.GetProperty("AssemblyDirectory", BindingFlags.Public | BindingFlags.Static);
                assemblyDirAttr?.SetValue(null, AssemblyDirectory);

                var startupMethod = pluginType.GetMethod("Startup", BindingFlags.NonPublic | BindingFlags.Instance);
                startupMethod.Invoke(pluginInstance, new object[] { });

                IsPluginLoaded = true;
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }

        private void UnloadPluginAssembly()
        {
            try
            {
                if (pluginInstance != null && pluginType != null)
                {
                    MethodInfo shutdownMethod = pluginType.GetMethod("Shutdown", BindingFlags.NonPublic | BindingFlags.Instance);
                    shutdownMethod.Invoke(pluginInstance, null);
                    pluginInstance = null;
                    pluginType = null;
                    pluginAssembly = null;
                }
                IsPluginLoaded = false;
            }
            catch (Exception ex)
            {
                Log(ex);
            }
        }
        #endregion

        private void Log(Exception ex)
        {
            Log(ex.ToString());
        }

        private void Log(string message)
        {
            File.AppendAllText(System.IO.Path.Combine(AssemblyDirectory, "log.txt"), $"{message}\n");
            try
            {
                CoreManager.Current.Actions.AddChatText(message, 1);
            }
            catch { }
        }
    }
}
