
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

using WindowsTimer = System.Windows.Forms.Timer;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using MyClasses.MetaViewWrappers;

[assembly: Guid("153809C7-5D30-12E1-8730-11111104AC1E")]

// Remember to update installer.nsi to match
[assembly: AssemblyVersion("1.2.0.0")]
[assembly: AssemblyFileVersion("1.2.0.0")]

namespace OracleOfDereth
{
    /// <summary>
    /// This is the main plugin class. When your plugin is loaded, Startup() is called, and when it's unloaded Shutdown() is called.
    /// </summary>
    /// 
    [FriendlyName("Oracle Of Dereth")]
    public class PluginCore : PluginBase
    {
        private static string _assemblyDirectory = null;
        private bool didInit = false;

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

        private WindowsTimer timer;

        // Views, depends on VirindiViewService.dll
        private MainView mainView;

        /// <summary>
        /// Called when your plugin is first loaded.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                // Commands
                CoreManager.Current.CommandLineText += new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);

                // Chat
                CoreManager.Current.ChatBoxMessage += new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);

                // Events
                CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete; // Not run on hot reload

                // Initialize
                if (CoreManager.Current.CharacterFilter.LoginStatus >= 1)
                {
                    Init();
                    Util.Chat($"Hot Reloaded", Util.ColorOrange);
                }
                else
                {
                    CoreManager.Current.CharacterFilter.Login += CharacterFilter_Login;
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void CharacterFilter_Login(object sender, EventArgs e)
        {
            try
            {
                Core.CharacterFilter.Login -= CharacterFilter_Login;
                Init();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
               Util.Chat("Running", Util.ColorOrange);
               QuestFlag.Reset();
                //Util.Chat("Resetting Quest Flags", Util.ColorOrange);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void Init()
        {
            // CharacterFilter_Login will be called multiple times if the character was already in the world
            if (didInit) return;
            didInit = true;

            // Load Data
            JohnQuest.LoadJohnQuestsCSV();

            // Initialize Views
            mainView = new MainView();

            // Initialize 1second update timer
            timer = new WindowsTimer();
            timer.Tick += new EventHandler(Tick);
            timer.Interval = 1000; // 1 second
            timer.Start();
        }

        private void Tick(object sender, EventArgs e)
        {
            try
            {
                mainView.Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        /// <summary>
        /// Called when your plugin is unloaded. Either when logging out, closing the client, or hot reloading.
        /// </summary>
        protected override void Shutdown()
        {
            try
            {
                // Commands
                CoreManager.Current.CommandLineText -= new EventHandler<ChatParserInterceptEventArgs>(Current_CommandLineText);

                // Chat
                CoreManager.Current.ChatBoxMessage -= new EventHandler<ChatTextInterceptEventArgs>(Current_ChatBoxMessage);

                // Cleanup Events
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;

                // Shutdown timer
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= Tick;
                    timer.Dispose();
                    timer = null;
                }

                // Dispose all views
                mainView?.Dispose();

            } catch (Exception ex) { Util.Log(ex); }
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
            catch (Exception ex) { Util.Log(ex); }
        }
        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            try
            {
                if (e.Text == null) return;

                OracleOfDereth.ChatBoxMessage.Process(e.Text);
            }
            catch (Exception ex) { Util.Log(ex); }
        }
    }
}
