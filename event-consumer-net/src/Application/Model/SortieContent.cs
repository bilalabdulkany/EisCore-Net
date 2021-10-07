using System;
using System.Collections.Generic;

namespace event_consumer_net.Application.Model
{
    public class SortieContent
    {
        public int MId { get; set; }
        public string CId { get; set; }
        public string Name { get; set; }
        public string EventTimestamp { get; set; }
        public List<CrEvents> CrEvents { get; set; }

       

        public override string ToString()
        {
            return MId+ " "+CId+" ::"+Name+ " ::" + EventTimestamp;
        }
    }
}