using Decal.Adapter;
using Decal.Adapter.Wrappers;
using Decal.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace OracleOfDereth.Models
{
    // From MagTools DetectItemsIdentifiedByUser
    public class WorldObjectIdentifier : IDisposable
    {
        public event EventHandler<WorldObject> Identified;
        private CoreManager core;

        public WorldObjectIdentifier()
        {
            try
            {
                core = CoreManager.Current;
                core.WindowMessage += Current_WindowMessage;
                core.ItemSelected += Current_ItemSelected;
                core.WorldFilter.ChangeObject += WorldFilter_ChangeObject;
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        private bool disposed;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;
            var subscribedCore = core;
            core = null;
            Identified = null;
            itemsSelected.Clear();
            if (disposing && subscribedCore != null)
            {
                try { subscribedCore.WindowMessage -= Current_WindowMessage; }
                catch (Exception ex) { Util.Log(ex); }
                try { subscribedCore.ItemSelected -= Current_ItemSelected; }
                catch (Exception ex) { Util.Log(ex); }
                try { subscribedCore.WorldFilter.ChangeObject -= WorldFilter_ChangeObject; }
                catch (Exception ex) { Util.Log(ex); }
            }
        }

        DateTime lastLeftClick = DateTime.MinValue;

        const int WM_LBUTTONDOWN = 0x201;

        void Current_WindowMessage(object sender, WindowMessageEventArgs e)
        {
            if (disposed) return;
            try
            {
                if (e.Msg == WM_LBUTTONDOWN)
                    lastLeftClick = DateTime.UtcNow;
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        readonly Dictionary<int, DateTime> itemsSelected = new Dictionary<int, DateTime>();

        internal void ExpireSelections(DateTime now)
        {
            foreach (int id in itemsSelected.Where(pair => now - pair.Value > TimeSpan.FromSeconds(10))
                .Select(pair => pair.Key).ToArray()) itemsSelected.Remove(id);
        }

        void Current_ItemSelected(object sender, ItemSelectedEventArgs e)
        {
            if (disposed) return;
            try
            {
                ExpireSelections(DateTime.UtcNow);
                if (e.ItemGuid == 0)
                    return;

                if (itemsSelected.ContainsKey(e.ItemGuid))
                    itemsSelected[e.ItemGuid] = DateTime.UtcNow;
                else
                    itemsSelected.Add(e.ItemGuid, DateTime.UtcNow);

                if (DateTime.UtcNow - lastLeftClick < TimeSpan.FromSeconds(1))
                {
                    CoreManager.Current.Actions.RequestId(e.ItemGuid);
                    lastLeftClick = DateTime.MinValue;
                }
            }
            catch (Exception ex) { Util.Log(ex); }
        }

        void WorldFilter_ChangeObject(object sender, ChangeObjectEventArgs e)
        {
            if (disposed) return;
            try
            {
                if (e.Change != WorldChangeType.IdentReceived)
                    return;

                ExpireSelections(DateTime.UtcNow);

                if (!itemsSelected.ContainsKey(e.Changed.Id))
                    return;

                itemsSelected.Remove(e.Changed.Id);

                if (e.Changed.ObjectClass == ObjectClass.Corpse ||
                    e.Changed.ObjectClass == ObjectClass.Door ||
                    e.Changed.ObjectClass == ObjectClass.Foci ||
                    e.Changed.ObjectClass == ObjectClass.Housing ||
                    e.Changed.ObjectClass == ObjectClass.Lifestone ||
                    e.Changed.ObjectClass == ObjectClass.Npc ||
                    e.Changed.ObjectClass == ObjectClass.Player ||
                    e.Changed.ObjectClass == ObjectClass.Portal ||
                    e.Changed.ObjectClass == ObjectClass.Vendor)
                    return;

                if (Identified != null) { Identified(this, e.Changed); }
            }
            catch (Exception ex) { Util.Log(ex); }
        }
    }
}
