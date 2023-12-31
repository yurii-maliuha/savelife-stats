﻿namespace SaveLife.Stats.Downloader.Models
{
    public class RunHistory
    {
        public long? LastTransactionId { get; set; }

        public DateTime DateFrom { get; set; }

        public DateTime DateTo { get; set; }

        public int Page { get; set; }
        public int PerPage { get; set; } = 100;

    }
}
