using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;

namespace az203.tables.Data
{
    public class Todo : TableEntity
    {
        public string Content { get; set; }
        public bool Completed { get; set; }
        public string Due { get; set; }
    }
}
