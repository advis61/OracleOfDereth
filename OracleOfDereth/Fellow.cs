using Decal.Adapter;
using Decal.Adapter.Wrappers;
using System;

namespace OracleOfDereth
{
    public class Fellow
    {
        public WorldObject Item;
        public int Id;
        public string Name = "";
        public string FellowshipName = "";
        public bool Identified = false;
        public DateTime LastRequestedAt = DateTime.MinValue;
        public DateTime LastIdentifiedAt = DateTime.MinValue;
        public DateTime LastRecruitedAt = DateTime.MinValue;

        public int LastRequestedAgo()
        {
            if (LastRequestedAt == DateTime.MinValue) return -1;
            return (int)(DateTime.Now - LastRequestedAt).TotalSeconds;
        }

        public int LastIdentifiedAgo()
        {
            if (LastIdentifiedAt == DateTime.MinValue) return -1;
            return (int)(DateTime.Now - LastIdentifiedAt).TotalSeconds;
        }

        public int LastRecruitedAgo()
        {
            if (LastRecruitedAt == DateTime.MinValue) return -1;
            return (int)(DateTime.Now - LastRecruitedAt).TotalSeconds;
        }

        public bool WasRecruited()
        {
            return LastRecruitedAt != DateTime.MinValue;
        }

        public bool FellowshipNameBlank()
        {
            return string.IsNullOrEmpty(FellowshipName);
        }

        public override string ToString()
        {
            return $"{Name} Fellowship: {FellowshipName}";
        }
    }
}
