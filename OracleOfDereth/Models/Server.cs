using Decal.Adapter;

namespace OracleOfDereth
{
    // Central place to ask which world the character is on, so the "Conquest" / "Levistras"
    // string literals don't get scattered across the codebase. Wraps CharacterFilter.Server.
    public static class Server
    {
        public const string Conquest = "Conquest";
        public const string Levistras = "Levistras";

        // CharacterFilter.Server is a COM property that marshals a fresh string on every read, and
        // the chat handler asks five models whether we're on Conquest for every line the game
        // prints — so this was one of the hottest reads in the plugin. The world can't change
        // without a login, so read it once and hold it; PluginCore clears this on login and again
        // on LoginComplete, by which point the filter definitely knows the answer.
        //
        // Empty is never actually cached: until the filter has a world to report, every read falls
        // through and tries again. That also means we stop touching COM entirely once logged in,
        // which matters at character-select — the 1s timer keeps ticking there with the client's
        // structures gone.
        private static string cached = "";

        public static string Name
        {
            get
            {
                if (cached.Length == 0) { cached = CoreManager.Current.CharacterFilter.Server ?? ""; }
                return cached;
            }
        }

        public static void Init() { cached = ""; }

        public static bool Is(string name) => Name == name;

        public static bool IsConquest => Is(Conquest);
        public static bool IsLevistras => Is(Levistras);
    }
}
