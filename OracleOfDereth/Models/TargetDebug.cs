using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;

namespace OracleOfDereth
{
    // "/od targetdebug" — everything needed to work out why the Void Target View isn't showing for
    // a mob.
    //
    // The important line is the third one. Target.CurrentTargetId is only ever set from Decal's
    // ItemSelected event, whereas Actions.CurrentSelection is what the client actually has selected
    // right now. If those disagree, the plugin never heard about your selection and nothing in
    // IsTarget() can help — that's the bug, and it's upstream of every rule in there.
    public static class TargetDebug
    {
        public static void Run()
        {
            int pluginId = Target.CurrentTargetId;
            int clientId = 0;

            try { clientId = CoreManager.Current.Actions.CurrentSelection; }
            catch (Exception ex) { Util.Log(ex); }

            Util.Chat($"plugin target   {Describe(pluginId)}", Util.ColorPink);
            Util.Chat($"client selected {Describe(clientId)}", Util.ColorPink);

            if (pluginId != clientId)
            {
                Util.Chat("=> MISMATCH: ItemSelected never reached the plugin for this target.", Util.ColorPink);
            }
            else if (clientId == 0)
            {
                Util.Chat("=> Nothing selected.", Util.ColorPink);
            }
            else
            {
                Util.Chat("=> Match: the plugin is pointed at what you have selected.", Util.ColorPink);
            }

            Detail(clientId);

            bool voidKnown = !new Skill(CharFilterSkillType.VoidMagic).IsUnKnown();
            Util.Chat($"void known {voidKnown}   IsTarget(plugin) {new Target { Id = pluginId }.IsTarget()}   IsTarget(client) {new Target { Id = clientId }.IsTarget()}", Util.ColorPink);
        }

        private static string Describe(int id)
        {
            if (id == 0) return "none";

            WorldObject item = Lookup(id);
            if (item == null) return $"[{id}] <not in WorldFilter>";

            return $"[{id}] {item.Name} ({item.ObjectClass}/{(int)item.ObjectClass})";
        }

        private static void Detail(int id)
        {
            WorldObject item = Lookup(id);
            if (item == null) return;

            int behavior = item.Values(LongValueKey.Behavior, 0);

            Util.Chat($"behavior {behavior} [{FlagNames(behavior)}]", Util.ColorPink);
            Util.Chat($"level {item.Values(LongValueKey.CreatureLevel, 0)}   species {item.Values(LongValueKey.Species, 0)}   type {item.Values(LongValueKey.Type, 0)}   pkstatus {item.Values((LongValueKey)134, 0)}", Util.ColorPink);
        }

        private static WorldObject Lookup(int id)
        {
            if (id == 0) return null;
            try { return CoreManager.Current.WorldFilter[id]; } catch { return null; }
        }

        // ObjectDescriptionFlag, the field VTank prints as "Behavior".
        private static readonly Dictionary<int, string> BehaviorFlags = new Dictionary<int, string>
        {
            { 0x1, "Openable" }, { 0x2, "Inscribable" }, { 0x4, "Stuck" }, { 0x8, "Player" },
            { 0x10, "Attackable" }, { 0x20, "PlayerKiller" }, { 0x40, "HiddenAdmin" }, { 0x80, "UiHidden" },
            { 0x100, "Book" }, { 0x200, "Vendor" }, { 0x400, "PkSwitch" }, { 0x800, "NpkSwitch" },
            { 0x1000, "Door" }, { 0x2000, "Corpse" }, { 0x4000, "LifeStone" }, { 0x8000, "Food" },
            { 0x10000, "Healer" }, { 0x20000, "Lockpick" }, { 0x40000, "Portal" }, { 0x80000, "Admin" },
            { 0x100000, "FreePkStatus" }, { 0x200000, "ImmuneCellRestrictions" }, { 0x400000, "RequiresPackSlot" },
            { 0x800000, "Retained" }, { 0x1000000, "PkLiteStatus" }, { 0x2000000, "IncludesSecondHeader" },
            { 0x4000000, "BindStone" }, { 0x8000000, "VolatileRare" }, { 0x10000000, "WieldOnUse" },
            { 0x20000000, "WieldLeft" },
        };

        private static string FlagNames(int behavior)
        {
            List<string> names = new List<string>();
            foreach (KeyValuePair<int, string> flag in BehaviorFlags)
            {
                if ((behavior & flag.Key) != 0) { names.Add(flag.Value); }
            }

            return names.Count == 0 ? "none" : string.Join("|", names);
        }
    }
}
