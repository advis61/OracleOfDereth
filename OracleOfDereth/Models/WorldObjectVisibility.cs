using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;
using System.Collections.Generic;

namespace OracleOfDereth
{
    // Deletes the client object, including its rendered model; this is not a list filter.
    // Server-side pets are unaffected. Do not delete inside CreateObject or while enumerating
    // WorldFilter: native deletion can synchronously release objects and invalidate wrappers.
    public static class WorldObjectVisibility
    {
        private static bool inPortal;
        private static bool failed;

        public static void Init()
        {
            inPortal = false;
            failed = false;
        }

        public static void PortalModeChanged(string mode)
        {
            if (mode == "EnterPortal") inPortal = true;
            else if (mode == "ExitPortal") inPortal = false;
        }

        public static void Command(string command)
        {
            bool pets = command == "/od deletepets" || command.StartsWith("/od deletepets ");
            string prefix = pets ? "/od deletepets" : "/od deletesummons";

            Setting setting = pets ? Setting.DeleteOtherPets : Setting.DeleteOtherSummons;

            if (command == prefix + " on" || command == prefix + " off")
            {
                setting.Value = command == prefix + " on" ? "Yes" : "No";
                ResetFailure();
            }
            else if (command != prefix)
            {
                Util.Chat($"Usage: {prefix} [on|off]", Util.ColorPink);
                return;
            }

            Util.Chat($"{setting.Name}: {setting.Value}.{(failed ? " Paused after an error." : "")}", Util.ColorPink);

            if (command == prefix + " off")
                Util.Chat($"Deletion stopped for {(pets ? "pets" : "summons")}. Already deleted creatures return only when the game reloads them.", Util.ColorPink);
        }

        public static void ResetFailure() => failed = false;

        public static void Tick()
        {
            bool summons = Setting.DeleteOtherSummons?.IsYes == true;
            bool pets = Setting.DeleteOtherPets?.IsYes == true;

            if (!summons && !pets)
            {
                failed = false;
                return;
            }
            if (inPortal || failed || IntPtr.Size != 4) return;

            var core = CoreManager.Current;
            if (core == null || core.CharacterFilter.LoginStatus < 1) return;

            int playerId = core.CharacterFilter.Id;
            if (playerId == 0 || !core.Actions.IsValidObject(playerId) || core.Actions.Underlying.GetPhysicsObjectPtr(playerId) == 0) return;

            try
            {
                if (summons) DeleteOwnedCreatures(ObjectClass.Monster, playerId);
                if (pets) DeleteOwnedCreatures(ObjectClass.Npc, playerId);
            }
            catch (Exception ex)
            {
                failed = true;
                Util.Log(ex);
                Util.Chat("Pet and summon deletion stopped after an error. Toggle either setting off and on to retry.", Util.ColorPink);
            }
        }

        private static void DeleteOwnedCreatures(ObjectClass category, int playerId)
        {
            var core = CoreManager.Current;
            var ids = new List<int>();

            using (var creatures = core.WorldFilter.GetByObjectClass(category))
                foreach (WorldObject creature in creatures) ids.Add(creature.Id);

            foreach (int id in ids)
            {
                // Resolve again after previous deletions; never retain a native pointer.
                if (core.WorldFilter[id]?.ObjectClass != category || !core.Actions.IsValidObject(id) || core.Actions.Underlying.GetPhysicsObjectPtr(id) == 0) continue;
                DeleteWeenie(id, playerId);
            }

        }

        internal static bool ShouldDeletePet(uint ownerId, uint playerId) => playerId != 0 && ownerId != 0 && ownerId != playerId;

        private static unsafe void DeleteWeenie(int id, int playerId)
        {
            // Retail x86 client bindings verified against UtilityBelt.Service 3.0.11:
            // CObjectMaint.s_pcInstance, GetWeenieObject(uint), DeleteObject(uint).
            // UtilityBelt's Nametags uses ACCWeenieObject.pwd._pet_owner to recognize pets.
            // pwd starts at 0x98, and _pet_owner is at 0xA8 within PublicWeenieDesc.
            // Keep these client-version-specific details here, as with AcClient.cs.
            void* objects = *(void**)0x842ADC;
            if (objects == null) return;

            var getWeenie = (delegate* unmanaged[Thiscall]<void*, uint, byte*>)0x5088E0;
            byte* weenie = getWeenie(objects, unchecked((uint)id));
            if (weenie == null) return;

            uint ownerId = *(uint*)(weenie + 0x98 + 0xA8);
            if (!ShouldDeletePet(ownerId, unchecked((uint)playerId))) return;

            var deleteObject = (delegate* unmanaged[Thiscall]<void*, uint, int>)0x508FA0;
            deleteObject(objects, unchecked((uint)id));
        }
    }
}
