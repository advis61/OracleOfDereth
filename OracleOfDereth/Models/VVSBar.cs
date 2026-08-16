using System;
using System.Collections.Generic;
using System.Reflection;
using VirindiViewService.HudBar;

namespace OracleOfDereth
{
    // Reorders the Virindi View Service icon bar — the strip of plugin icons in the client — and
    // remembers that order across sessions. The only place anything reaches into VVS internals.
    //
    // How the bar works, established by disassembling VirindiViewService.dll:
    //
    //  - cHudBarHud holds a SortedList<int, sHudInfo>. Each sHudInfo carries a `group` (shared by
    //    every view one plugin owns) and a `zorder` (that view's place within its plugin). Icons
    //    are drawn in (group, zorder) order.
    //
    //  - Nothing about that is persisted. vvs.s3db stores position, size and theme per view but has
    //    no ordering column, and VVS regenerates the group values at every startup — which is why
    //    no registry or database edit can move an icon, and why a saved order has to be re-applied
    //    on each login.
    //
    //  - Reordering must NOT go through SetHudInfo. That makes VVS rebuild the bar, and the rebuild
    //    disposes every HudPictureBox and re-clones every icon — including images it does not own,
    //    which destroys plugin icons (Virindi Tank's warning glyph among them) and then throws on
    //    the next clone. Instead we set the group values and move the existing controls ourselves
    //    with HudFixedLayout.SetControlRect. See Reposition.
    public static class VVSBar
    {
        // Saved order, as plugin names joined by '|'. Names rather than group numbers, because the
        // numbers are regenerated every startup and mean nothing across sessions.
        private const string OrderSettingKey = "BarOrder";
        private const char Separator = '|';

        // VVS's own theme button. Not a plugin: it never appears in the Decal tab, is never part of
        // a saved order, and is pinned to the front. Identified by name — its group stops
        // identifying it the moment anything moves it.
        private const string VvsThemeEntry = "Change Global Theme";
        private const int VvsOwnGroup = int.MinValue;

        #region Decal tab

        // Plugin names in bar order — one entry per plugin, however many icons it puts up, because
        // its views share a group and always move together. Empty when VVS isn't up yet.
        public static List<string> PluginNames()
        {
            var names = new List<string>();

            var entries = Guard(Entries);
            if (entries == null) return names;

            foreach (PluginRow row in PluginsInBarOrder(entries)) names.Add(row.Name);
            return names;
        }

        // Moves a plugin one place along the bar, by swapping its group value with its neighbour's.
        //
        // Swapping rather than assigning means we only reuse numbers already on the bar, so a move
        // can't collide with a plugin that hasn't registered yet, and moving back is exactly the
        // inverse. Every view the plugin owns shares the group, so they all travel together and
        // their zorder is untouched.
        //
        // Returns whether anything moved, so the caller knows to repaint and persist.
        public static bool MovePlugin(string name, int direction)
        {
            var bar = Guard(Bar);
            var entries = Guard(Entries);
            if (bar == null || entries == null) return false;

            PinThemeFirst(entries);

            PluginRow plugin = FindPlugin(PluginsInBarOrder(entries), name);
            if (plugin == null || plugin.Group == VvsOwnGroup) return false;

            List<int> groups = PluginGroups(entries);
            int target = groups.IndexOf(plugin.Group) + direction;
            if (target < 0 || target >= groups.Count) return false;

            int otherGroup = groups[target];

            try
            {
                // The entries are VVS's own objects, so setting group here IS the change.
                int moved = 0;
                foreach (var pair in entries)
                {
                    sHudInfo info = pair.Value;
                    if (info == null) continue;

                    if (info.group == plugin.Group) { info.group = otherGroup; moved++; }
                    else if (info.group == otherGroup) { info.group = plugin.Group; moved++; }
                }

                Reposition(bar, entries);
                barSignature = Signature(entries);
                return moved > 0;
            }
            catch (Exception ex)
            {
                // The groups are written before anything moves on screen, so the new order IS
                // applied — only the repositioning failed. Reporting this as a failed move would
                // discard a change that really happened.
                Util.Chat("VVS bar: order changed, but the icons could not be repositioned.", Util.ColorPink);
                Util.Log(ex);
                return true;
            }
        }

        #endregion

        #region Save and apply

        // Remembers the bar's current order. The Decal tab calls this after every move, so the saved
        // order always matches the list and there is nothing to remember to press.
        public static void Save()
        {
            var entries = Guard(Entries);
            if (entries == null) return;

            var names = new List<string>();
            foreach (PluginRow row in PluginsInBarOrder(entries))
            {
                if (!string.IsNullOrEmpty(row.Name)) names.Add(row.Name);
            }

            SettingsFile.PutSetting(OrderSettingKey, string.Join(Separator.ToString(), names.ToArray()));
        }

        // Puts the saved plugins at the front of the bar in the saved order; anything not in the
        // list keeps its own group and follows on behind, in its natural order.
        //
        // Silent throughout: every caller is automatic — the login settle and the Decal tab — so
        // having no saved order yet, or a saved plugin since uninstalled, is nothing to report.
        private static void Apply()
        {
            string saved = SettingsFile.GetSetting(OrderSettingKey, "");
            if (string.IsNullOrEmpty(saved)) return;

            var bar = Guard(Bar);
            var entries = Guard(Entries);
            if (bar == null || entries == null) return;

            PinThemeFirst(entries);

            List<PluginRow> present = PluginsInBarOrder(entries);

            // Match saved names to plugins currently on the bar, preserving the saved order.
            var ordered = new List<PluginRow>();
            foreach (string name in saved.Split(Separator))
            {
                PluginRow match = FindPlugin(present, name);
                if (match != null && !ordered.Contains(match)) ordered.Add(match);
            }

            if (ordered.Count == 0)
            {
                return;
            }

            // The ordered block sits just above the theme button so it leads the bar. Unlisted
            // plugins keep their natural group, which is far higher, and follow on behind.
            int baseGroup = VvsOwnGroup + 1;
            int topOfBlock = baseGroup + ordered.Count - 1;

            foreach (PluginRow row in present)
            {
                // An unlisted plugin sitting inside the block we are about to write would collide
                // with it, so leave the bar alone rather than half-order it.
                if (ordered.Contains(row)) continue;
                if (row.Group <= topOfBlock) return;
            }

            try
            {
                var target = new Dictionary<int, int>(); // old group -> new group
                for (int i = 0; i < ordered.Count; i++) target[ordered[i].Group] = baseGroup + i;

                foreach (var pair in entries)
                {
                    sHudInfo info = pair.Value;
                    if (info == null) continue;

                    int fresh;
                    if (target.TryGetValue(info.group, out fresh)) info.group = fresh;
                }

                Reposition(bar, entries);
            }
            catch (Exception ex)
            {
                Util.Log(ex);
            }
        }

        #endregion

        #region Login settle

        // Applying at LoginComplete is too early: plugins are still bringing their views up, and one
        // that registers afterwards lands wherever VVS puts it — Virindi Window Tool consistently
        // ended up at the far end. So rather than guess at a delay, arm here and let Tick wait
        // until the bar has actually stopped changing.
        //
        // Gated by the "Order Decal Plugins on Startup" setting: PluginCore doesn't call this at all
        // when that is off, so nothing is armed and no timer is created.
        public static void ArmLoginApply()
        {
            loginApplyArmed = true;
            settleTicks = SettleTimeoutTicks;
            stableTicks = 0;
            barSignature = 0;
        }

        // Called from the plugin's 1s tick. Once the order has been applied this is a single bool
        // read and a return, which is the state it is in for all but the first few seconds of a
        // session — cheap enough not to deserve a timer of its own.
        public static void Tick()
        {
            if (!loginApplyArmed) return;

            settleTicks--;

            var entries = Guard(Entries);
            if (entries == null)
            {
                if (settleTicks <= 0) loginApplyArmed = false;
                return;
            }

            int signature = Signature(entries);
            if (signature != barSignature)
            {
                // Something is still registering. Reset the stability count and keep waiting.
                barSignature = signature;
                stableTicks = 0;
            }
            else
            {
                stableTicks++;
            }

            if (stableTicks < SettleStableTicks && settleTicks > 0) return;

            loginApplyArmed = false;
            Apply();

            // Record what we applied to, so the Decal tab doesn't immediately think it changed.
            var after = Guard(Entries);
            if (after != null) barSignature = Signature(after);
        }

        // Re-applies the saved order when the bar's make-up has changed since we last looked.
        //
        // A plugin that tears down and recreates its view — Virindi Window Tool does — comes back
        // with the group VVS generates for it, losing the place we gave it. Called from the Decal
        // tab's repaint, so it costs a hash of the bar's keys only while that tab is open.
        public static void ReapplyIfChanged()
        {
            var entries = Guard(Entries);
            if (entries == null) return;

            int signature = Signature(entries);
            if (signature == barSignature) return;

            barSignature = signature;
            Apply();
        }

        // Cheap fingerprint of what is on the bar. Keys are handed out incrementally, so a view
        // being recreated always shows up as a new key.
        private static int Signature(SortedList<int, sHudInfo> entries)
        {
            int hash = 17;
            foreach (var pair in entries) { hash = hash * 31 + pair.Key; }
            return hash;
        }

        // Oracle is torn down and rebuilt on every character switch, so this state must not carry
        // over — a stale fingerprint would make the first check after re-login decide nothing had
        // changed, and skip re-applying onto a bar VVS had just rebuilt from scratch.
        public static void Shutdown()
        {
            barSignature = 0;
            loginApplyArmed = false;

            // Drop the cached reflection handle too. It points at a type in VVS's assembly, and
            // holding it across a plugin reload keeps that Type alive for no reason — the lookup
            // costs nothing to repeat.
            entriesField = null;
        }

        // At a 1s timer: "unchanged for 3 seconds, and give up after 30".
        private const int SettleTimeoutTicks = 30;
        private const int SettleStableTicks = 3;

        private static bool loginApplyArmed;
        private static int settleTicks;
        private static int stableTicks;
        private static int barSignature;

        #endregion

        #region Bar contents

        // One plugin: its group, and the name of its primary (lowest-zorder) view.
        private sealed class PluginRow
        {
            public int Group;
            public string Name;
        }

        // The plugins on the bar, in bar order, one row per group. A plugin's identity comes from
        // its lowest-zorder view — "GoArrow" rather than "GoArrow: Dungeon Map".
        private static List<PluginRow> PluginsInBarOrder(SortedList<int, sHudInfo> entries)
        {
            var best = new Dictionary<int, KeyValuePair<int, string>>(); // group -> (zorder, name)

            foreach (var pair in entries)
            {
                sHudInfo info = pair.Value;
                if (info == null || IsTheme(info)) continue;

                KeyValuePair<int, string> current;
                if (!best.TryGetValue(info.group, out current) || info.zorder < current.Key)
                {
                    best[info.group] = new KeyValuePair<int, string>(info.zorder, info.EntryName ?? "");
                }
            }

            var rows = new List<PluginRow>();
            foreach (var kv in best) rows.Add(new PluginRow { Group = kv.Key, Name = kv.Value.Value });
            rows.Sort(delegate (PluginRow a, PluginRow b) { return a.Group.CompareTo(b.Group); });
            return rows;
        }

        // Distinct plugin groups in bar order. The theme button is excluded so it can never be a
        // swap target — leaving it in let the topmost plugin "move up" into it, taking int.MinValue
        // for itself and shoving VVS's own control down the bar.
        private static List<int> PluginGroups(SortedList<int, sHudInfo> entries)
        {
            var groups = new List<int>();

            foreach (var pair in entries)
            {
                sHudInfo info = pair.Value;
                if (info == null || info.group == VvsOwnGroup) continue;
                if (!groups.Contains(info.group)) groups.Add(info.group);
            }

            groups.Sort();
            return groups;
        }

        private static bool IsTheme(sHudInfo info)
        {
            return info != null && string.Equals(info.EntryName, VvsThemeEntry, StringComparison.OrdinalIgnoreCase);
        }

        // Keeps the theme button first and every plugin behind it: the button goes back to
        // int.MinValue if anything moved it, and no plugin may sit at or below that value.
        private static void PinThemeFirst(SortedList<int, sHudInfo> entries)
        {
            foreach (var pair in entries)
            {
                sHudInfo info = pair.Value;
                if (info == null) continue;

                if (IsTheme(info)) info.group = VvsOwnGroup;
                else if (info.group <= VvsOwnGroup) info.group = VvsOwnGroup + 1;
            }
        }

        // Exact name first, then with any version suffix stripped, so a plugin that puts its version
        // in its title still matches after it updates.
        private static PluginRow FindPlugin(List<PluginRow> rows, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (PluginRow row in rows)
            {
                if (string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase)) return row;
            }

            string wanted = StripVersion(name);
            if (wanted.Length < 3) return null;

            foreach (PluginRow row in rows)
            {
                if (string.Equals(StripVersion(row.Name), wanted, StringComparison.OrdinalIgnoreCase)) return row;
            }

            return null;
        }

        // "UtilityBelt - v0.2.7" -> "UtilityBelt", "Virindi Tank v.1.0.0.0" -> "Virindi Tank",
        // "pkRadar v1.4" -> "pkRadar". Both sides of a comparison go through this, so it only has to
        // be consistent, not perfect.
        private static string StripVersion(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            string s = name;

            int dash = s.IndexOf(" - ", StringComparison.Ordinal);
            if (dash > 0) s = s.Substring(0, dash);

            // " v" or " V" followed by a digit or dot starts a version: " v1.4", " v.1.0".
            for (int i = 0; i < s.Length - 2; i++)
            {
                if (s[i] != ' ') continue;
                if (s[i + 1] != 'v' && s[i + 1] != 'V') continue;

                if (char.IsDigit(s[i + 2]) || s[i + 2] == '.') { s = s.Substring(0, i); break; }
            }

            return s.Trim();
        }

        private static int CompareDrawOrder(KeyValuePair<int, sHudInfo> a, KeyValuePair<int, sHudInfo> b)
        {
            int ag = a.Value?.group ?? 0, bg = b.Value?.group ?? 0;
            if (ag != bg) return ag.CompareTo(bg);

            int az = a.Value?.zorder ?? 0, bz = b.Value?.zorder ?? 0;
            if (az != bz) return az.CompareTo(bz);

            return a.Key.CompareTo(b.Key);
        }

        #endregion

        #region Moving the icons

        // Moves the icons on screen to match the current group order, by rewriting the rectangles of
        // the controls the bar already has.
        //
        // Nothing here touches an ACImage. The icons keep their existing controls and images; only
        // their positions are permuted, so there is nothing to dispose and nothing to clone — which
        // is the whole reason this exists instead of SetHudInfo. Reusing the bar's own rectangles
        // also preserves its spacing and wrapping; we only choose which icon occupies which.
        private static bool Reposition(cHudBarHud bar, SortedList<int, sHudInfo> entries)
        {
            var layout = FieldByType<VirindiViewService.Controls.HudFixedLayout>(bar);
            System.Collections.IDictionary map = ControlMap(bar);
            if (layout == null || map == null) return false;

            var byKey = new Dictionary<int, VirindiViewService.Controls.HudControl>();
            var slots = new List<System.Drawing.Rectangle>();

            foreach (System.Collections.DictionaryEntry e in map)
            {
                var control = e.Key as VirindiViewService.Controls.HudControl;
                if (control == null || !(e.Value is int)) continue;

                byKey[(int)e.Value] = control;
                slots.Add(layout.GetControlRect(control));
            }

            if (slots.Count == 0) return false;

            bool horizontal = true;
            try { horizontal = bar.Horizontal; } catch { }

            // The positions the bar already uses, in visual order.
            slots.Sort(delegate (System.Drawing.Rectangle x, System.Drawing.Rectangle y)
            {
                int primary = horizontal ? x.X.CompareTo(y.X) : x.Y.CompareTo(y.Y);
                if (primary != 0) return primary;
                return horizontal ? x.Y.CompareTo(y.Y) : x.X.CompareTo(y.X);
            });

            var wanted = new List<KeyValuePair<int, sHudInfo>>(entries);
            wanted.Sort(CompareDrawOrder);

            int slot = 0;
            foreach (var pair in wanted)
            {
                VirindiViewService.Controls.HudControl control;
                if (!byKey.TryGetValue(pair.Key, out control)) continue;
                if (slot >= slots.Count) break;

                layout.SetControlRect(control, slots[slot]);
                slot++;
            }

            return slot > 0;
        }

        // The bar's control -> hud key map. Found by shape rather than by its obfuscated name, and
        // walked as a non-generic IDictionary so the generic arguments never have to be named.
        private static System.Collections.IDictionary ControlMap(cHudBarHud bar)
        {
            foreach (FieldInfo f in bar.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!typeof(System.Collections.IDictionary).IsAssignableFrom(f.FieldType)) continue;

                var candidate = f.GetValue(bar) as System.Collections.IDictionary;
                if (candidate == null || candidate.Count == 0) continue;

                foreach (System.Collections.DictionaryEntry e in candidate)
                {
                    if (e.Key is VirindiViewService.Controls.HudControl) return candidate;
                    break;
                }
            }

            return null;
        }

        private static T FieldByType<T>(object owner) where T : class
        {
            foreach (FieldInfo f in owner.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(T).IsAssignableFrom(f.FieldType)) return f.GetValue(owner) as T;
            }
            return null;
        }

        #endregion

        #region VVS access

        // The live bar. Static property on VVS's Service, so there is no instance to track.
        private static cHudBarHud Bar()
        {
            return VirindiViewService.Service.HudBarInstance;
        }

        // The bar's contents. The backing field is private with an obfuscated name ("j" in the
        // shipped build), so it is located by field TYPE — that survives a rename, which a
        // hardcoded name would not.
        private static SortedList<int, sHudInfo> Entries()
        {
            cHudBarHud bar = Bar();
            if (bar == null) return null;

            if (entriesField == null) entriesField = FindEntriesField(bar);
            if (entriesField == null) return null;

            return entriesField.GetValue(bar) as SortedList<int, sHudInfo>;
        }

        private static FieldInfo FindEntriesField(cHudBarHud bar)
        {
            foreach (FieldInfo f in bar.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (typeof(SortedList<int, sHudInfo>).IsAssignableFrom(f.FieldType)) return f;
            }
            return null;
        }

        // Resolved on first use and reused. Null means "look it up again" rather than "absent", so
        // a lookup attempted before the bar existed isn't cached as a permanent failure.
        private static FieldInfo entriesField;

        // The single place VVS's internals are absorbed, so no caller sees a reshuffled VVS as an
        // exception.
        private static T Guard<T>(Func<T> probe) where T : class
        {
            try { return probe(); }
            catch { return null; }
        }

        #endregion
    }
}
