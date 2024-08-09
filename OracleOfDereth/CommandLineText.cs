using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OracleOfDereth
{
    public static class CommandLineText
    {
        public static bool Process(string text)
        {
            string command = text.ToLower().Trim();

            if (command == "/od" || command == "/ood")
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                CoreManager.Current.Actions.AddChatText($"Oracle of Dereth v{version}", 1);
                return true;
            }

            if (command == "/od exception")
            {
                CoreManager.Current.Actions.AddChatText($"Oracle of Dereth EXCEPTION", 1);
                throw new InvalidOperationException("An error occurred.");
            }

            return false;
        }
    }

    //class Command : IDisposable
    //{
    //    readonly PluginHost host;
    //    private bool disposed;

    //    public Command(PluginHost host)
    //    {
    //        this.host = host;
    //    }

    //    public void Dispose()
    //    {
    //        Dispose(true);
    //        GC.SuppressFinalize(this);
    //    }

    //    protected virtual void Dispose(bool disposing)
    //    {
    //        if (disposed || !disposing) { return; }

    //        CoreManager.Current.Actions.AddChatText($"Disposing Command Class", 1);

    //        // Indicate that the instance has been disposed.
    //        disposed = true;
    //    }
    //}
}
