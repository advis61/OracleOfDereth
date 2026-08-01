using Decal.Adapter;

namespace OracleOfDereth
{
    // Central place to ask which world the character is on, so the "Conquest" / "Levistras"
    // string literals don't get scattered across the codebase. Wraps CharacterFilter.Server.
    public static class Server
    {
        public const string Conquest = "Conquest";
        public const string Levistras = "Levistras";

        // The current world name (e.g. "Conquest", "Levistras"), or "" if not yet known.
        public static string Name => CoreManager.Current.CharacterFilter.Server ?? "";

        public static bool Is(string name) => Name == name;

        public static bool IsConquest => Is(Conquest);
        public static bool IsLevistras => Is(Levistras);
    }
}
