using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

[assembly: InternalsVisibleTo("RFCustomHorses")]
namespace RealmsForgotten
{
    internal static class Globals
    {
        public static Assembly realmsForgottenAssembly = Assembly.GetExecutingAssembly();
    }
}
