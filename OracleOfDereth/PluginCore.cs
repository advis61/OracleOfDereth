using Decal.Adapter;
using Decal.Adapter.Wrappers;
using OracleOfDereth.Models;
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using WindowsTimer = System.Windows.Forms.Timer;

[assembly: Guid("153809C7-5D30-12E1-8730-11111104AC1E")]

[assembly: AssemblyVersion("2.1.0.0")]
[assembly: AssemblyFileVersion("2.1.0.0")]

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
        private TradeView tradeView;

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
                CoreManager.Current.CharacterFilter.ChangePortalMode += CharacterFilter_ChangePortalMode;
                CoreManager.Current.EchoFilter.ServerDispatch += EchoFilter_ServerDispatch;
                CoreManager.Current.WorldFilter.CreateObject += WorldFilter_CreateObject;
                CoreManager.Current.WorldFilter.ReleaseObject += WorldFilter_ReleaseObject;
                CoreManager.Current.WorldFilter.ChangeObject += WorldFilter_ChangeObject;
                CoreManager.Current.WorldFilter.EnterTrade += WorldFilter_EnterTrade;
                CoreManager.Current.WorldFilter.EndTrade += WorldFilter_EndTrade;
                CoreManager.Current.WorldFilter.AddTradeItem += WorldFilter_AddTradeItem;
                CoreManager.Current.WorldFilter.ResetTrade += WorldFilter_ResetTrade;

                worldObjectIdentifier = new WorldObjectIdentifier();
                worldObjectIdentifier.Identified += WorldObjectIdentifier_Identified;

                // Initialize
                if (CoreManager.Current.CharacterFilter.LoginStatus >= 1) {
                    Util.Chat($"Hot Reloaded", Util.ColorOrange, "[Oracle of Dereth] ");
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
                CoreManager.Current.CharacterFilter.Login -= CharacterFilter_Login;
                Init();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void CharacterFilter_LoginComplete(object sender, EventArgs e)
        {
            try
            {
                if(Setting.BuffsRemaining.IsYes) Util.Chat($"{Hud.BuffNowText()}", Util.ColorOrange, "[Oracle of Dereth] ");
                if(Setting.CheckForUpdates.IsYes) UpdateChecker.Arm();
                ConquestAugmentation.Refresh();
                // Doesn't apply the saved plugin bar order here — plugins are still bringing their
                // views up, and one that registers afterwards (Virindi Window Tool) would land
                // wherever VVS puts it. Arms instead; Tick applies it once the bar stops changing.
                if (Setting.OrderDecalPlugins.IsYes) VVSBar.ArmLoginApply();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Verified in-game: the loader replaces PluginCore on the next login, not at logout. Init
        // resets static stores. Tick must tolerate character select because native structures are
        // already gone and their access violations are not catchable on .NET Framework 4.8.
        private void Init()
        {
            // CharacterFilter_Login will be called multiple times if the character was already in the world
            if (didInit) return;
            didInit = true;

            // Initialize Settings
            SettingsFile.Init();
            Setting.Init();

            // Initialize Collection
            Augmentation.Init();
            AugQuest.Init();
            Cantrip.Init();
            CreditQuest.Init();
            FacilityQuest.Init();
            FellowshipTracker.Init();
            Fellowship.Init();
            FlagQuest.Init();
            JohnQuest.Init();
            SocietyQuest.Init();
            CustomQuest.Init();
            QuestCatalog.Init();
            QuestCatalogUpdater.Init();
            Marker.Init();
            Nearby.Init();
            QuestFlag.Init();
            QuestAccountFlag.Init();
            Recall.Init();
            Target.Init();
            Title.Init();
            ItemList.Init();
            ItemCache.Init();
            Trade.Init();
            ConquestAugmentation.Init();
            ConquestEnlAugmentation.Init();
            ConquestBank.Init();
            ConquestBonus.Init();
            TopBoard.Init();


            // Initialize Views
            mainView = new MainView();
            targetView = new TargetView();
            tradeView = new TradeView();

            // Initialize 1second update timer
            timer = new WindowsTimer();
            timer.Tick += new EventHandler(Tick);
            timer.Interval = 1000; // 1 second
            timer.Start();
        }

        [System.Runtime.ExceptionServices.HandleProcessCorruptedStateExceptions]
        [System.Security.SecurityCritical]
        private void Tick(object sender, EventArgs e)
        {
            try
            {
                if (CoreManager.Current.CharacterFilter.LoginStatus < 1) return;

                Target.RemoveAllExpired();
                // Pause player identify requests while the Items tab or a trade window is open,
                // so they don't compete with the item/trade appraisal queue for the server's
                // shared identify channel.
                FellowshipTracker.Update(suppressIdentify: mainView.IsItemsTabActive() || Trade.IsOpen);
                Fellowship.AutoOpenFellow();
                Nearby.Tick(); // reconcile tracked objects vs the world (drops any missed by ReleaseObject)
                UpdateChecker.Tick();
                QuestCatalogUpdater.Tick();
                QuestFlagLookup.Tick();
                QuestSubmit.Tick();
                QuestState.Tick();
                QuestAccountFlag.Tick();
                ItemList.TickAll();
                Trade.Tick();
                VVSBar.Tick();
                WorldObjectVisibility.Tick();

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
            ShutdownComponent(() =>
            {
                CoreManager.Current.CommandLineText -= Current_CommandLineText;
                CoreManager.Current.ChatBoxMessage -= Current_ChatBoxMessage;
                CoreManager.Current.ItemSelected -= Current_ItemSelected;
                CoreManager.Current.CharacterFilter.Login -= CharacterFilter_Login;
                CoreManager.Current.CharacterFilter.LoginComplete -= CharacterFilter_LoginComplete;
                CoreManager.Current.CharacterFilter.SpellCast -= CharacterFilter_SpellCast;
                CoreManager.Current.CharacterFilter.ChangePortalMode -= CharacterFilter_ChangePortalMode;
                CoreManager.Current.EchoFilter.ServerDispatch -= EchoFilter_ServerDispatch;
                CoreManager.Current.WorldFilter.CreateObject -= WorldFilter_CreateObject;
                CoreManager.Current.WorldFilter.ReleaseObject -= WorldFilter_ReleaseObject;
                CoreManager.Current.WorldFilter.ChangeObject -= WorldFilter_ChangeObject;
                CoreManager.Current.WorldFilter.EnterTrade -= WorldFilter_EnterTrade;
                CoreManager.Current.WorldFilter.EndTrade -= WorldFilter_EndTrade;
                CoreManager.Current.WorldFilter.AddTradeItem -= WorldFilter_AddTradeItem;
                CoreManager.Current.WorldFilter.ResetTrade -= WorldFilter_ResetTrade;
                if (worldObjectIdentifier != null)
                    worldObjectIdentifier.Identified -= WorldObjectIdentifier_Identified;
            });

            ShutdownComponent(() =>
            {
                if (timer != null)
                {
                    timer.Stop();
                    timer.Tick -= Tick;
                    timer.Dispose();
                    timer = null;
                }
            });

            ShutdownComponent(() => worldObjectIdentifier?.Dispose());
            ShutdownComponent(Screenshot.Cancel);
            ShutdownComponent(UpdateChecker.Shutdown);
            ShutdownComponent(QuestCatalogUpdater.Shutdown);
            ShutdownComponent(QuestFlagLookup.Shutdown);
            ShutdownComponent(QuestSubmit.Shutdown);
            ShutdownComponent(VVSBar.Shutdown);

            ShutdownComponent(() => tradeView?.Dispose());
            ShutdownComponent(() => targetView?.Dispose());
            ShutdownComponent(() => mainView?.Dispose());
        }

        private static void ShutdownComponent(Action shutdown)
        {
            try { shutdown(); }
            catch (Exception ex) { Util.Log(ex); }
        }

        public unsafe void Current_CommandLineText(object sender, ChatParserInterceptEventArgs e)
        {
            if (e.Text == null) return;
            string cmd = e.Text.ToLower().Trim();

            try 
            {
                if (cmd == "/od" || cmd == "/ood" || cmd == "/od version") { Util.Chat($"Oracle of Dereth v{Assembly.GetExecutingAssembly().GetName().Version}", 1); }
                else if (cmd == "/od exception") { throw new InvalidOperationException("An error occurred."); }
                else if (cmd == "/od targetdebug") { TargetDebug.Run(); }
                else if (cmd == "/od vtank") { VTank.Debug(); }
                else if (cmd == "/od screenshot") { Screenshot.Take(); }
                else if (cmd == "/od vistashot") { Screenshot.TakeVista(); }
                else if (cmd == "/od deletesummons" || cmd.StartsWith("/od deletesummons ")) { WorldObjectVisibility.Command(cmd); }
                else if (cmd == "/od deletepets" || cmd.StartsWith("/od deletepets ")) { WorldObjectVisibility.Command(cmd); }
                else if (cmd == "/od landblock") { Util.Chat($"Current landblock: {Util.CurrentLandblockHex()} (block 0x{Util.CurrentLandblock():X4})"); }
                else if (cmd == "/od logout") { CoreManager.Current.Actions.Logout(); }
                else if (cmd == "/od fellow open") { Fellowship.Open(); }
                else if (cmd == "/od fellow close") { Fellowship.Close(); }
                else if (cmd == "/od fellow disband") { Fellowship.Disband(); }
                else if (cmd == "/od fellow create") { Fellowship.Create(); }
                else if (cmd == "/od fellow quit") { Fellowship.Quit(); }
                else if (cmd.StartsWith("/od fellow recruit ")) { Fellowship.Recruit(cmd.Substring(19, cmd.Length - 19)); }
                else if (cmd == "/od checkbank") { Bank.Check(); }
                else if (cmd == "/od questflag") { QuestFlagLookup.Execute(); }
                else if (cmd == "/od update") { UpdateChecker.Check(true); }
                else if (cmd == "/od quests update") { QuestCatalogUpdater.UpdateNow(); }
                else if (cmd == "/od quests reset") { QuestCatalog.Reset(); }
                else if (cmd == "/od quests send test") { mainView.SendTestQuestFlag(); }
                else if (cmd == "/od quests send") { mainView.SendQuestFlags(); }
                else if (cmd == "/od quests send clear") { mainView.ClearSentQuestFlags(); }
                else if (cmd == "/myquests") { QuestFlag.ManualRefresh(); return; }
                else if (cmd == "/myqstlist") { QuestAccountFlag.ManualRefresh(); return; }
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
                if (Target.YouCastRegex.IsMatch(e.Text))
                {
                    Target.SpellStarted(e.Text);
                    targetView.Update();
                }
                else if (Target.PeriodicNetherRegex.IsMatch(e.Text))
                {
                    Target.SpellTicked(e.Text);
                }
                else if (QuestFlag.MyQuestRegex.IsMatch(e.Text))
                {
                    QuestFlag.Add(e.Text);
                    Suppress(e, QuestFlag.SuppressChat);
                }
                else if (QuestAccountFlag.Capture(e.Text))
                {
                    Suppress(e, QuestAccountFlag.SuppressChat);
                }
                else if (QuestFlag.StampedRegex.IsMatch(e.Text))
                {
                    // Read but never eaten: this is the game telling the player they just earned a
                    // flag, not a reply to something the plugin asked for. No Suppress() here.
                    QuestFlag.Stamped(e.Text);
                }
                else if (Bank.WithdrawConfirmRegex.IsMatch(e.Text))
                {
                    Trade.RecheckFunds();
                }
                else if (Bank.MatchesAutoDeposit(e.Text))
                {
                    Suppress(e, true);
                }
                else if (ConquestBank.Matches(e.Text))
                {
                    Suppress(e, ConquestBank.NoteChat(e.Text));
                }
                else if (Bank.Matches(e.Text))
                {
                    Bank.NoteChat(e.Text);
                }
                else if (ConquestAugmentation.Matches(e.Text))
                {
                    Suppress(e, ConquestAugmentation.NoteChat(e.Text));
                }
                else if (ConquestEnlAugmentation.Matches(e.Text))
                {
                    Suppress(e, ConquestEnlAugmentation.NoteChat(e.Text));
                }
                else if (ConquestBonus.Matches(e.Text))
                {
                    Suppress(e, ConquestBonus.NoteChat(e.Text));
                }
                else if (ConquestFship.Matches(e.Text))
                {
                    Suppress(e, ConquestFship.NoteChat(e.Text));
                }
                else if (TopBoard.Matches(e.Text))
                {
                    Suppress(e, TopBoard.NoteChat(e.Text));
                }
                else if (Trade.CheckPriceRegex.IsMatch(e.Text))
                {
                    Trade.NotePriceTell(e.Text);
                }
                else if (Trade.PointsReplyRegex.IsMatch(e.Text))
                {
                    Trade.NotePointsTell(e.Text);
                }
                else if (Trade.TradeStartedRegex.IsMatch(e.Text))
                {
                    Trade.NoteBotTell(e.Text);
                }
                else if (ChatFilter.ShouldSuppress(e.Text))
                {
                    e.Eat = true;
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
        // Keep a chat line out of the window when it was the reply to a command the plugin issued
        // on your behalf — the tab already shows that data. `ours` comes from the model that just
        // parsed the line; it is false for the same command typed by hand, which always prints.
        private void Suppress(ChatTextInterceptEventArgs e, bool ours)
        {
            if (ours && Setting.SuppressPluginRefreshChat.IsYes) { e.Eat = true; }
        }

        private void Current_ItemSelected(object sender, ItemSelectedEventArgs e)
        {
            try
            {
                Target.SetCurrent(e.ItemGuid);
                targetView.Update();
                mainView.UpdateTarget();

                if (ItemList.Inventory.AutoAddEnabled && mainView.IsItemsTabActive())
                {
                    ItemList.Inventory.RequestAdd(e.ItemGuid);
                }

                // Selection changed — repaint the visible lists so the matching row highlights.
                if (mainView.IsItemsTabActive()) mainView.UpdateItemsList();
                if (Trade.IsOpen) tradeView.UpdateList();
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

        // Fires on entering and exiting portal space (recall / portal / dungeon transition). We don't
        // distinguish the two — either way we're zoning, so drop the now-stale appraisal cache and prime
        // the auto-recruit pause. Clearing twice per zone is harmless, and the pause window ends up
        // anchored to the arrival (the later event), which is what we want.
        private void CharacterFilter_ChangePortalMode(object sender, ChangePortalModeEventArgs e)
        {
            try
            {
                Screenshot.Cancel();
                ItemCache.Clear();
                if (e.Type.ToString() == "EnterPortal") { Nearby.ClearObjects(); }
                Fellowship.NoteZoned();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void WorldFilter_CreateObject(object sender, CreateObjectEventArgs e)
        {
            FellowshipTracker.Add(e.New);
            Nearby.Add(e.New);
            Trade.OnObjectCreated(e.New); // trade the stack split off for an auto-payment
        }

        private void WorldFilter_ReleaseObject(object sender, ReleaseObjectEventArgs e)
        {
            Nearby.Remove(e.Released);
        }

        // Central identify funnel: forward completed appraisals to the Items list,
        // which matches them against its pending/queued requests.
        private void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e)
        {
            try
            {
                if (e.Change == WorldChangeType.IdentReceived) { ItemList.IdentReceivedAll(e.Changed); }
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Opening a trade window with another player. The Trade model snapshots our inventory,
        // works out the partner, and resets the item list; we just show the window. The whole
        // feature is off when "Show Trade Window" is No — we don't begin or show anything, so the
        // other trade handlers stay idle (Trade.IsOpen never becomes true).
        private void WorldFilter_EnterTrade(object sender, EnterTradeEventArgs e)
        {
            try
            {
                if (Setting.ShowTradeWindow.IsNo) return;

                Trade.Begin(e);
                tradeView.Show();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // An item was dropped into the trade window. Trade decides whether it's the partner's
        // (vs. our own offer) and adds it to the list.
        private void WorldFilter_AddTradeItem(object sender, AddTradeItemEventArgs e)
        {
            try
            {
                Trade.AddItem(e.ItemId);
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // Either side cleared their offered items; the trade window stays open.
        private void WorldFilter_ResetTrade(object sender, ResetTradeEventArgs e)
        {
            try
            {
                Trade.Reset();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        private void WorldFilter_EndTrade(object sender, EndTradeEventArgs e)
        {
            try
            {
                Trade.End();
                tradeView.Hide();
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        // User-identified items (manual clicks). Item-list completion no longer
        // flows through here — it's handled centrally by WorldFilter_ChangeObject.
        private void WorldObjectIdentifier_Identified(object sender, WorldObject item)
        {
            Summon.Identified(item);
            ItemInfo.WeaponIdentified(item);
        }

        // https://github.com/ACEmulator/ACE/blob/master/Source/ACE.Server/Network/GameEvent/GameEventType.cs
        private void EchoFilter_ServerDispatch(object sender, NetworkMessageEventArgs e)
        {
            try {
                if (e.Message.Type != 0xF7B0) { return; } // Game Event

                int eventType = (int)e.Message["event"];

                if (eventType == 0x0029) {
                    Title.Parse(e.Message.Struct("titles")); // Titles list
                }

                else if (eventType == 0x002B) {
                    Title.ParseUpdate(e.Message.Value<Int32>("title")); // Update titles
                }

                else if (eventType == 0x00C9) {
                    FellowshipTracker.Parse(e.Message.RawData); // Identify Response
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
    }
}
