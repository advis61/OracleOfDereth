using Decal.Adapter.Wrappers;
using System;
using System.Globalization;
using System.Linq;

namespace OracleOfDereth
{
    internal static class TreasureMap
    {
        private const int NorthSouthKey = 9014;
        private const int EastWestKey = 9015;

        public static void Identified(WorldObject item)
        {
            if (Setting.ShowTreasureMapCoordinates?.IsNo == true) return;
            if (item == null || !item.HasIdData || !IsMap(item.Name, item.ObjectClass)) return;
            // Missing properties must not turn into a bogus 0.0N 0.0E destination.
            if (!item.DoubleKeys.Contains(NorthSouthKey) || !item.DoubleKeys.Contains(EastWestKey)) return;
            string message = Format(item.Name, item.ObjectClass,
                item.Values((DoubleValueKey)NorthSouthKey), item.Values((DoubleValueKey)EastWestKey));
            // Use the same self-tell and modifier-key routing as quest hints.
            if (message != null) Util.Think(message);
        }

        private static bool IsMap(string name, ObjectClass objectClass) =>
            objectClass == ObjectClass.Gem && name != null &&
            name.IndexOf("Treasure Map", StringComparison.OrdinalIgnoreCase) >= 0;

        internal static string Format(string name, ObjectClass objectClass, double? northSouth, double? eastWest)
        {
            if (!IsMap(name, objectClass) || !Valid(northSouth) || !Valid(eastWest)) return null;
            double ns = northSouth.Value;
            double ew = eastWest.Value;
            return name + ": " + Math.Abs(ns).ToString("0.0", CultureInfo.InvariantCulture) + (ns < 0 ? "S" : "N")
                + " " + Math.Abs(ew).ToString("0.0", CultureInfo.InvariantCulture) + (ew < 0 ? "W" : "E");
        }

        private static bool Valid(double? value) =>
            value.HasValue && !double.IsNaN(value.Value) && !double.IsInfinity(value.Value);
    }
}
