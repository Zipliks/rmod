using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace siege_wpf.jsonClasses
{
    class root_obj
    {
        public string ver { get; set; }
        public string gameExeHash { get; set; }
        public ver_urls[] main_arr { get; set; }
    }
}
