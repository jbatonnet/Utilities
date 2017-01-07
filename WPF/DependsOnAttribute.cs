using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System.Windows
{
    public class DependsOnAttribute : Attribute
    {
        public string[] Properties { get; private set; }

        public DependsOnAttribute(params string[] properties)
        {
            Properties = properties;
        }
    }
}