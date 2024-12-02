using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RealmsForgotten.AiMade.Knighthood
{
    public static class Logger
    {
        public static void Trace(string message)
        {
            // Log trace messages (for debugging purposes)
            System.Diagnostics.Debug.WriteLine($"TRACE: {message}");
        }

        public static void Error(string message)
        {
            // Log error messages
            System.Diagnostics.Debug.WriteLine($"ERROR: {message}");
        }
    }
}
