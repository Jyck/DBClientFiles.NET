﻿using DBClientFiles.NET.Attributes;

namespace DBClientFiles.NET.Data.WDBC
{
    [DBFileName(Name = "Achievement.WDBC", Extension = FileExtension.DBC)]
    public sealed class AchievementEntry
    {
        [Index]
        public int ID { get; set; }
        public int Faction { get; set; }
        public int MapID { get; set; }
        public uint Supercedes { get; set; }
        [Cardinality(SizeConst = 16)]
        public string[] Title { get; set; }
        public uint TitleFlags { get; set; }
        [Cardinality(SizeConst = 16)]
        public string[] Description { get; set; }
        public uint Description_flags { get; set; }
        public uint Category { get; set; }
        public uint Points { get; set; }
        public uint UIOrder { get; set; }
        public uint Flags { get; set; }
        public uint IconID { get; set; }
        [Cardinality(SizeConst = 16)]
        public string[] Rewards { get; set; }
        public uint RewardFlags { get; set; }
        public uint MinimumCriteria { get; set; }
        public uint SharesCriteria { get; set; }
    }
}
