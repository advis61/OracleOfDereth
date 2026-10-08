using System;
using OracleOfDereth;

internal static class ArenaBoardTests
{
    public static void Run()
    {
        ArenaBoard.Init();
        string header = "***** Top Ten 1v1 Players *****";
        string row = "  Rank #1 - Cloud of Mages\r\n  Rank Points: 1722\r\n  Total Matches: 72\r\n  Total Wins: 52\r\n  Total Draws: 0\r\n  Total Losses: 20";
        if (ArenaBoard.NoteChat(header + "\r\n\r\n" + row + "\r\n**********"))
            throw new Exception("A manually requested arena reply was marked for suppression.");
        var players = ArenaBoard.Sorted();
        if (players.Count != 1 || players[0].Name != "Cloud of Mages" ||
            string.Join(",", players[0].Metrics) != "1722,72,52,0,20")
            throw new Exception("Arena multi-line parsing lost a name or metric.");

        ArenaBoard.NoteChat(header);
        ArenaBoard.NoteChat("Rank #2 - Unstable Cornbread");
        ArenaBoard.NoteChat("Rank Points: 1,712");
        if (ArenaBoard.Sorted().Count != 0)
            throw new Exception("Arena displayed an incomplete row or retained an old reply.");
        ArenaBoard.NoteChat("Total Matches: 51");
        ArenaBoard.NoteChat("Total Wins: 36");
        ArenaBoard.NoteChat("Total Draws: 0");
        ArenaBoard.NoteChat("Total Losses: 15");
        foreach (string line in row.Split('\n')) ArenaBoard.NoteChat(line);
        ArenaBoard.NoteChat("**********");
        players = ArenaBoard.Sorted();
        if (players.Count != 2 || players[0].Rank != 1 || players[1].Name != "Unstable Cornbread" || players[1].Metrics[0] != "1,712")
            throw new Exception("Arena split replies were not sorted by server rank.");

        ArenaBoard.NoteChat(header + "\n**********");
        ArenaBoard.NoteChat(row);
        if (ArenaBoard.Sorted().Count != 0)
            throw new Exception("Arena accepted rows outside a reply or retained stale rows.");
        ArenaBoard.Init();
    }
}
