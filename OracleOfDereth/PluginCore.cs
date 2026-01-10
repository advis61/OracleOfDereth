using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsTimer = System.Windows.Forms.Timer;

[assembly: Guid("153809C7-5D30-12E1-8730-11111104AC1E")]

// Remember to update installer.nsi to match
[assembly: AssemblyVersion("1.10.0.0")]
[assembly: AssemblyFileVersion("1.10.0.0")]

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

        // Tools
        private WorldObjectIdentifier worldObjectIdentifier;

        // Views, depends on VirindiViewService.dll
        private MainView mainView;
        private TargetView targetView;

        /// <summary>
        /// Called when your plugin is first loaded.
        /// </summary>
        protected override void Startup()
        {
            try
            {
                CoreManager.Current.CommandLineText += Current_CommandLineText;
                CoreManager.Current.ChatBoxMessage += Current_ChatBoxMessage;
                CoreManager.Current.ItemSelected += Current_ItemSelected;
                CoreManager.Current.CharacterFilter.LoginComplete += CharacterFilter_LoginComplete; // Not run on hot reload
                CoreManager.Current.CharacterFilter.SpellCast += CharacterFilter_SpellCast;
                CoreManager.Current.EchoFilter.ServerDispatch += EchoFilter_ServerDispatch;

                worldObjectIdentifier = new WorldObjectIdentifier();
                worldObjectIdentifier.Identified += WorldObjectIdentifier_Identified;

                // Initialize
                if (CoreManager.Current.CharacterFilter.LoginStatus >= 1) {
                    Util.Chat($"Hot Reloaded", Util.ColorOrange);
                    Init();
                } else {
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
                Util.Chat($"{Hud.BuffNowText()}", Util.ColorOrange);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void Init()
        {
            // CharacterFilter_Login will be called multiple times if the character was already in the world
            if (didInit) return;
            didInit = true;

            // Initialize Collection
            Augmentation.Init();
            AugQuest.Init();
            Cantrip.Init();
            CreditQuest.Init();
            FacilityQuest.Init();
            Fellow.Init();
            FlagQuest.Init();
            JohnQuest.Init();
            Marker.Init();
            QuestFlag.Init();
            Recall.Init();
            Target.Init();
            Title.Init();

            // Initialize Views
            mainView = new MainView();
            targetView = new TargetView();

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
                Target.RemoveAllExpired();
                Fellow.Update();

                mainView.Update();
                targetView.Update();
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
                CoreManager.Current.CommandLineText -= Current_CommandLineText;
                CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
                CoreManager.Current.ItemSelected -= Current_ItemSelected;
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                CoreManager.Current.CharacterFilter.SpellCast -= CharacterFilter_SpellCast;
                CoreManager.Current.EchoFilter.ServerDispatch -= EchoFilter_ServerDispatch;
                worldObjectIdentifier.Identified -= WorldObjectIdentifier_Identified;

                // Shutdown timer
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= Tick;
                    timer.Dispose();
                    timer = null;
                }

                // Dispose all tools
                worldObjectIdentifier?.Dispose();

                // Dispose all views
                mainView?.Dispose();
                targetView?.Dispose();

            } catch (Exception ex) { Util.Log(ex); }
        }

        public void Current_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            if (e.Text == null) return;
            string cmd = e.Text.ToLower().Trim();

            try 
            {
                if (cmd == "/od" || cmd == "/ood") { Util.Chat($"Oracle of Dereth v{Assembly.GetExecutingAssembly().GetName().Version}", 1); }
                else if (cmd == "/od exception") { throw new InvalidOperationException("An error occurred."); }
                else if (cmd == "/od logout") { CoreManager.Current.Actions.Logout(); }
                else if (cmd == "/od fellow open") { Fellow.Open(); }
                else if (cmd == "/od fellow close") { Fellow.Close(); }
                else if (cmd == "/od fellow disband") { Fellow.Disband(); }
                else if (cmd == "/od fellow create") { Fellow.Create("eveldan"); }
                else if (cmd == "/od fellow quit") { Fellow.Quit(); }
                else if (cmd == "/od fellow isin") { Util.Chat($"In fellow? {Fellow.IsInFellowship()}"); }
                else if (cmd == "/od fellow isopen") { Util.Chat($"Is open? {Fellow.IsOpen()}"); }
                else if (cmd == "/od fellow isleader") { Util.Chat($"Is leader? {Fellow.IsLeader()}"); }
                else if (cmd == "/od fellow count") { Util.Chat($"Fellow count {Fellow.FellowCount()}"); }
                else if (cmd.StartsWith("/od fellow recruit ")) { Fellow.Recruit(cmd.Substring(19, cmd.Length - 19)); }

                else { return; }

                e.Eat = true;
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        private void Current_ChatBoxMessage(object sender, ChatTextInterceptEventArgs e)
        {
            if (e.Text == null) return;

            try 
            {
                // Track /myquests output
                if (QuestFlag.MyQuestRegex.IsMatch(e.Text)) 
                {
                    QuestFlag.Add(e.Text);
                }
                else if (Target.YouCastRegex.IsMatch(e.Text))
                {
                    Target.SpellStarted(e.Text);
                    targetView.Update();
                }
                else if (Target.PeriodicNetherRegex.IsMatch(e.Text))
                {
                    Target.SpellTicked(e.Text);
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        private void Current_ItemSelected(object sender, ItemSelectedEventArgs e)
        {
            try
            {
                Target.SetCurrent(e.ItemGuid);
                targetView.Update();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void CharacterFilter_SpellCast(object sender, SpellCastEventArgs e)
        {
            try
            {
                Target.SpellCast(e.TargetId, e.SpellId);
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        private void WorldObjectIdentifier_Identified(object sender, WorldObject item)
        {
            Summon.Identified(item);
        }

        // https://github.com/ACEmulator/ACE/blob/master/Source/ACE.Server/Network/GameEvent/GameEventType.cs
        private void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            try {
                if (e.Message.Type != 0xF7B0) { return; } // Game Event

                int eventType = (int)e.Message["event"];

                if (eventType == 0x0029) // Titles list
                {
                    Title.Parse(e.Message.Struct("titles"));
                }

                else if (eventType == 0x002B) // Update titles
                {
                    Title.ParseUpdate(e.Message.Value<Int32>("title"));
                }

                else if (eventType == 0x00C9) // Identify Response
                {
                    Fellow.Parse(e.Message.RawData);
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
    }
}
