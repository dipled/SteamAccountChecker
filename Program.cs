using System.Text.Json;

class Program
{
    public static async Task Main(string[] args)
    {
        if (!File.Exists("Settings.json"))
        {
            Settings newSettings = new Settings { StartId = 0, EndId = 0, UnverifiedAccounts = true, Lvl0Accounts = true, CsgoAccounts = true, OldGames = true, NumThreads = 4 };
            string jsonString = JsonSerializer.Serialize(newSettings, new JsonSerializerOptions { WriteIndented = true });
            Console.WriteLine("CREATING JSON FILE.\nEDIT FILE TO YOUR LIKING.");
            File.WriteAllText("Settings.json", jsonString);
            Thread.Sleep(5000);
            Environment.Exit(0);
        }
        using FileStream fileStream = File.OpenRead("Settings.json");
        Settings? settings = JsonSerializer.Deserialize<Settings>(fileStream);
        if (settings == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON");
        }

        Console.WriteLine("Enter your API key.");
        string? ApiKey = Console.ReadLine();
        if (ApiKey == null)
        {
            throw new InvalidOperationException("Failed to read API key");
        }
        Directory.CreateDirectory("out/");
        QueryHandler queryHandler = new QueryHandler (settings, ApiKey);
        await queryHandler.Run();
    }
}
