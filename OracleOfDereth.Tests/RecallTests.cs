using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Reflection;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;
using System.Runtime.Serialization;
using Decal.Adapter;
using Decal.Adapter.Wrappers;
using OracleOfDereth;

internal static class RecallTests
{
    public static void Run()
    {
        const BindingFlags hidden = BindingFlags.Instance | BindingFlags.NonPublic;
        FieldInfo current = typeof(CoreManager).GetField("myService", BindingFlags.Static | BindingFlags.NonPublic);
        object savedCore = current.GetValue(null);
        List<Recall> savedRecalls = Recall.Recalls;
        var core = (CoreManager)Empty(typeof(CoreManager));
        var character = (CharacterFilter)Empty(typeof(CharacterFilter));
        FieldInfo underlying = typeof(CharacterFilter).BaseType.GetField("myObj", hidden);
        var live = new LearnedSpells(underlying.FieldType);
        underlying.SetValue(character, live.GetTransparentProxy());
        // Match Decal's cached snapshot taken before Bur Recall was learned.
        typeof(CharacterFilter).GetField("mySpellBook", hidden).SetValue(character,
            new Collection<int>(Array.Empty<int>()));
        typeof(CoreManager).GetField("myCharacterFilter", hidden).SetValue(core, character);
        current.SetValue(null, core);
        try
        {
            Recall.Recalls = new List<Recall>();
            Recall.Init();
            Recall bur = Recall.Recalls.Find(r => r.Name == "Bur");
            if (bur == null || bur.SpellId != 4084)
                throw new InvalidOperationException("The Bur Recall fixture no longer matches the recall catalog.");
            if (bur.IsComplete()) throw new InvalidOperationException("An unlearned recall appeared complete.");

            live.Known.Add(bur.SpellId);
            if (!bur.IsComplete() || character.SpellBook.Count != 0)
                throw new InvalidOperationException("Learning Bur Recall did not bypass the stale spellbook snapshot.");
            live.Known.Remove(bur.SpellId);
            if (bur.IsComplete()) throw new InvalidOperationException("Recall completion retained an obsolete learned spell.");
            int calls = live.Queries;
            if (new Recall().IsComplete() || live.Queries != calls)
                throw new InvalidOperationException("An invalid spell ID reached the live spell query.");
        }
        finally
        {
            current.SetValue(null, savedCore);
            Recall.Recalls = savedRecalls;
        }
    }

    // Stand in for Decal's COM character-stats interface while exercising the real
    // CharacterFilter.IsSpellKnown implementation. No native client is needed.
    private sealed class LearnedSpells : RealProxy
    {
        public readonly HashSet<int> Known = new HashSet<int>();
        public int Queries;
        public LearnedSpells(Type contract) : base(contract) { }
        public override IMessage Invoke(IMessage message)
        {
            var call = (IMethodCallMessage)message;
            if (call.MethodName != "get_SpellLearned")
                return new ReturnMessage(new InvalidOperationException("Unexpected character query: " + call.MethodName), call);
            Queries++;
            return new ReturnMessage(Known.Contains((int)call.Args[0]) ? 1 : 0, null, 0, call.LogicalCallContext, call);
        }
    }

    private static object Empty(Type type)
    {
        object value = FormatterServices.GetUninitializedObject(type);
        GC.SuppressFinalize(value);
        return value;
    }
}
