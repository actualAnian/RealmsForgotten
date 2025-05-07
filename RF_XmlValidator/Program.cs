// See https://aka.ms/new-console-template for more information
using System;
using System.IO;
using System.Xml;

class Program
{
    static void Main(string[] args)
    {
        string modulePath = @"C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\Modules\RealmsForgotten\ModuleData";

        if (!Directory.Exists(modulePath))
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"❌ ERROR: ModuleData folder not found at:\n{modulePath}");
            Console.ResetColor();
            Environment.Exit(1); // Stop build
        }

        Console.WriteLine($"🔍 Scanning XML files in: {modulePath}\n");

        bool hasError = false;
        foreach (string file in Directory.GetFiles(modulePath, "*.xml", SearchOption.AllDirectories))
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(file);
                Console.WriteLine($"✅ Valid: {Path.GetFileName(file)}");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Invalid XML: {Path.GetFileName(file)} — {ex.Message}");
                Console.ResetColor();
                hasError = true;
            }
        }

        Console.WriteLine();
        if (hasError)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("🛑 XML validation failed. Fix the above errors.");
            Console.ResetColor();
            Environment.Exit(1);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("✔️ All XML files are valid.");
            Console.ResetColor();
        }
    }
}
