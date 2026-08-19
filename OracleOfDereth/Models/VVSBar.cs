using System;
using System.Collections.Generic;
using System.Reflection;
using VirindiViewService.HudBar;

namespace OracleOfDereth
{
    // Reverse-engineered from VirindiViewService.dll: cHudBarHud sorts sHudInfo by (group, zorder),
    // and every view from one plugin shares a group. VVS regenerates groups at startup. SetHudInfo
    // is unsafe here because its rebuild disposes images owned by other plugins, so this updates
    // groups directly and repositions existing controls with HudFixedLayout.SetControlRect.
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

            // Carry forward anything in the saved order that isn't on the bar right now — disabled,
            // failed to load, or uninstalled. Writing only what is present would drop it, so a
            // single reorder made while a plugin happened to be off would cost that plugin its
            // place permanently. Re-inserted at the index it used to hold, so if it comes back it
            // lands roughly where it was rather than at the end with the new arrivals.
            string[] previous = SettingsFile.GetSetting(OrderSettingKey, "").Split(Separator);
            for (int i = 0; i < previous.Length; i++)
            {
                string name = CleanName(previous[i]);
                if (name.Length == 0 || Contains(names, name)) continue;

                names.Insert(i < names.Count ? i : names.Count, name);
            }

            SettingsFile.PutSetting(OrderSettingKey, string.Join(Separator.ToString(), names.ToArray()));
        }

        private static bool Contains(List<string> names, string name)
        {
            foreach (string n in names)
            {
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase)) return true;
            }
            return false;
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

        // LoginComplete precedes some VVS registrations, so Tick waits for stability and watches
        // briefly for late or recreated views.
        public static void ArmLoginApply()
        {
            phase = BarPhase.Settling;
            settleTicks = SettleTimeoutTicks;
            stableTicks = 0;
            watchTicks = 0;
            barSignature = 0;
        }

        // Called from the plugin's 1s tick. Once the bar has gone quiet this is a single enum read
        // and a return, which is the state it is in for all but the opening stretch of a session —
        // cheap enough not to deserve a timer of its own.
        public static void Tick()
        {
            if (phase == BarPhase.Idle) return;

            var entries = Guard(Entries);
            if (entries == null)
            {
                // No bar to look at. Only the settle phase waits for one to appear, and it gives up
                // at the timeout, so a client running without VVS stops checking.
                if (phase == BarPhase.Settling && --settleTicks <= 0) phase = BarPhase.Idle;
                return;
            }

            int signature = Signature(entries);

            if (phase == BarPhase.Settling)
            {
                settleTicks--;

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

                ApplyAndRecord();
                phase = BarPhase.Watching;
                watchTicks = WatchQuietTicks;
                return;
            }

            // Watching. Settling only proves the bar held still for three seconds, which it does
            // between plugins as easily as after the last one — so a plugin that finishes loading
            // later (Chaos Helper does, and Virindi Window Tool when it recreates its view) still
            // registers with a fresh group and lands at the far end. Waiting longer before applying
            // would only make every icon jump late instead, so apply early and keep looking: re-apply
            // whenever the bar's make-up changes, restarting the window each time, and stop once it
            // has been quiet for the whole of one.
            if (signature != barSignature)
            {
                barSignature = signature;
                ApplyAndRecord();
                watchTicks = WatchQuietTicks;
                return;
            }

            if (--watchTicks <= 0) phase = BarPhase.Idle;
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
            ApplyAndRecord();
        }

        // Applies, then records what the bar looks like afterwards, so the next change is measured
        // against the applied state and neither the watch nor the Decal tab immediately re-fires.
        private static void ApplyAndRecord()
        {
            Apply();

            var after = Guard(Entries);
            if (after != null) barSignature = Signature(after);
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
            phase = BarPhase.Idle;
            watchTicks = 0;

            // Drop the cached reflection handle too. It points at a type in VVS's assembly, and
            // holding it across a plugin reload keeps that Type alive for no reason — the lookup
            // costs nothing to repeat.
            entriesField = null;
        }

        // At a 1s timer: settle is "unchanged for 3 seconds, and give up after 30". The watch that
        // follows then wants 10 seconds of quiet before it lets go, measured from the last change
        // rather than from login — every plugin observed here is up within 10 seconds of the bar
        // appearing, so a window that long past the final registration covers the lot.
        private const int SettleTimeoutTicks = 30;
        private const int SettleStableTicks = 3;
        private const int WatchQuietTicks = 10;

        private enum BarPhase { Idle, Settling, Watching }

        private static BarPhase phase;
        private static int settleTicks;
        private static int stableTicks;
        private static int watchTicks;
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
                    // Cleaned here, once, so the display, the saved order and the matching all use
                    // the same stable name.
                    best[info.group] = new KeyValuePair<int, string>(info.zorder, CleanName(info.EntryName));
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

        // Exact name first. Rows are already cleaned, so that hits for anything saved since; the
        // second pass cleans the incoming name too, which is what lets an order saved before this
        // — holding "Virindi Global Inventory (44 to read)" or "UtilityBelt - v0.2.7" — still find
        // its plugin instead of silently dropping it.
        private static PluginRow FindPlugin(List<PluginRow> rows, string name)
        {
            if (string.IsNullOrEmpty(name)) return null;

            foreach (PluginRow row in rows)
            {
                if (string.Equals(row.Name, name, StringComparison.OrdinalIgnoreCase)) return row;
            }

            string wanted = CleanName(name);
            if (wanted.Length < 3) return null;

            foreach (PluginRow row in rows)
            {
                if (string.Equals(row.Name, wanted, StringComparison.OrdinalIgnoreCase)) return row;
            }

            return null;
        }

        // Reduces a bar entry to the plugin's stable name:
        //
        //   "Virindi Global Inventory (44 to read)" -> "Virindi Global Inventory"
        //   "UtilityBelt - v1.0.0beta"              -> "UtilityBelt"
        //   "Virindi Tank v.1.0.0.0"                -> "Virindi Tank"
        //   "pkRadar v1.4"                          -> "pkRadar"
        //
        // This is not just tidier to read. The saved order is keyed on the name, and a live counter
        // like "(44 to read)" changes between sessions — so without this the saved entry stops
        // matching and that plugin quietly loses its place on the next login.
        //
        // The dash test needs the spaces around it: "Mag-Tools" is a name, " - v1.0" is a suffix.
        private static string CleanName(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";

            string s = name;

            int paren = s.IndexOf('(');
            if (paren > 0) s = s.Substring(0, paren);

            int dash = s.IndexOf(" - ", StringComparison.Ordinal);
            if (dash > 0) s = s.Substring(0, dash);

            // " v" or " V" followed by a digit or dot starts a version: " v1.4", " v.1.0".
            for (int i = 0; i < s.Length - 2; i++)
            {
                if (s[i] != ' ') continue;
                if (s[i + 1] != 'v' && s[i + 1] != 'V') continue;

                if (char.IsDigit(s[i + 2]) || s[i + 2] == '.') { s = s.Substring(0, i); break; }
            }

            s = s.Trim();

            // Never reduce a name to nothing — a plugin whose whole title is a suffix keeps its
            // original, which is still better than a blank row.
            return s.Length > 0 ? s : name.Trim();
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

        // The bar's icon-control -> hud key map, found by shape rather than by its obfuscated name.
        //
        // Matched on the exact generic arguments, HudPictureBox -> int. The bar holds TWO control
        // dictionaries — one of HudPictureBox (the icons, which is this one) and one of
        // HudThemeElement — and both are keyed by a HudControl subclass. Matching on "key is a
        // HudControl" therefore picks whichever the runtime happens to return first, and would
        // silently reposition theme elements instead of icons if that order ever changed.
        //
        // The icons are picture boxes: cHudBarHud.a() does newobj HudPictureBox followed by
        // set_Image on each entry's cloned icon.
        private static System.Collections.IDictionary ControlMap(cHudBarHud bar)
        {
            foreach (FieldInfo f in bar.GetType().GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (!typeof(System.Collections.IDictionary).IsAssignableFrom(f.FieldType)) continue;

                Type[] args = f.FieldType.GetGenericArguments();
                if (args.Length != 2) continue;
                if (!typeof(VirindiViewService.Controls.HudPictureBox).IsAssignableFrom(args[0])) continue;
                if (args[1] != typeof(int)) continue;

                return f.GetValue(bar) as System.Collections.IDictionary;
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

        // Only one field has this exact shape, so unlike the control map there is nothing to
        // disambiguate here.
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
