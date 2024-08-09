
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;

namespace OracleOfDereth
{
    /// <summary>
    /// This is the main plugin class. When your plugin is loaded, Startup() is called, and when it's unloaded Shutdown() is called.
    /// </summary>
    [FriendlyName("Oracle Of Dereth")]
    public class PluginCore : PluginBase
    {
        private static string _assemblyDirectory = null;

        /// <summary>
        /// Assembly directory containing the plugin dll
        /// </summary>
        public static string AssemblyDirectory
        {
            get
            {
                if (_assemblyDirectory == null)
                {
                    try
                    {
                        _assemblyDirectory = System.IO.Path.GetDirectoryName(typeof(PluginCore).Assembly.Location);
                    }
                    catch
                    {
                        _assemblyDirectory = Environment.CurrentDirectory;
                    }
                }
                return _assemblyDirectory;
            }
            set
            {
                _assemblyDirectory = value;
            }
        }

        /// <summary>
        /// Called when your plugin is first loaded.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                var isHotReload = CoreManager.Current.CharacterFilter.LoginStatus == 3;

                // subscribe to CharacterFilter_LoginComplete event, make sure to unsubcribe later.
                // note: if the plugin was reloaded while ingame, this event will never trigger on the newly reloaded instance.
                CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete;
                
                // Commands
                CoreManager.Current.CommandLineText += new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);

                // this adds text to the chatbox. it's output is local only, other players do not see this.
                CoreManager.Current.Actions.AddChatText($"OracleOfDereth Startup. Hotreload? {isHotReload}", 1);

                //ui = new ExampleUI();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        /// <summary>
        /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                // make sure to unsubscribe from any events we were subscribed to. Not doing so
                // can cause the old plugin to stay loaded between hot reloads.
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

                // Commands
                CoreManager.Current.CommandLineText -= new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);

                // clean up our ui view
                // ui.Dispose();
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        /// <summary>
        /// CharacterFilter_LoginComplete event handler.
        /// </summary>
        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            // it's generally a good idea to use try/catch blocks inside of decal event handlers.
            // throwing an uncaught exception inside one will generally hard crash the client.
            try
            {
                CoreManager.Current.Actions.AddChatText($"This is my new decal plugin. CharacterFilter_LoginComplete SIS", 1);
            }
            catch (Exception ex) { Debug.Log(ex); }
        }

        /// <summary>
        /// Current_CommandLineText event handler.
        /// </summary>
        private void Current_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            try
            {
                if (e.Text == null) return;

                if (OracleOfDereth.CommandLineText.Process(e.Text)) 
                    e.Eat = true;
            }
            catch (Exception ex) { Debug.Log(ex); }
        }
    }
}
