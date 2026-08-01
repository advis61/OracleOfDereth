using System;

namespace OracleOfDereth
{
    // Records that the plugin issued a server command and is expecting its reply, so the chat
    // handler can tell our own output apart from the same command typed by hand. Only ours is
    // eligible to be kept out of the chat window (see Setting.SuppressPluginRefreshChat) — what
    // you type, you get to see.
    //
    // It has to be a time window rather than something exact: these replies carry nothing tying
    // them back to the command that asked for them, and none of them have a dependable last line
    // to close on. Long enough for a slow server to answer, short enough that typing the same
    // command a moment later still prints.
    //
    // TopBoard doesn't use this — a "/top" block announces its own length, so it tracks the reply
    // precisely and closes as soon as the last row lands.
    public class ChatRequest
    {
        private static readonly TimeSpan Window = TimeSpan.FromSeconds(5);

        private DateTime until = DateTime.MinValue;

        // The command just went out.
        public void Sent() { until = DateTime.UtcNow + Window; }

        // Called from each model's Init so a login can't inherit an open window.
        public void Clear() { until = DateTime.MinValue; }

        // True while a reply we asked for could still be arriving.
        public bool Awaiting => DateTime.UtcNow < until;
    }
}
