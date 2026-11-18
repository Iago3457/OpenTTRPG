using System;
using System.IO;

class Test
{
    static void Main()
    {
        string current = Directory.GetCurrentDirectory();
        Console.WriteLine("Current dir: " + current);
        string[] candidates = new string[] {
            Path.GetFullPath(Path.Combine(current, "../../saves/characters.json")),
            Path.GetFullPath(Path.Combine(current, "characters.json")),
            "/home/ritoj/Documentos/Códigos/Gitclones/OpenTTRPG/saves/characters.json"
        };
        foreach (var c in candidates)
        {
            Console.WriteLine($"Candidate {c}: Exists = {File.Exists(c)}");
        }
    }
}
