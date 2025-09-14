using System.Text.Json;
class QueryHandler
{
    public Settings settings;

    public QueryHandler()
    {
        using FileStream fileStream = File.OpenRead("Settings.json");
        Settings? settings_ = JsonSerializer.Deserialize<Settings>(fileStream);
        if (settings_ == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON");
        }
        settings = settings_;
    }

    public async Task<JsonDocument?> RequestSummary(string steamId64)
    {
        using HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/";
        string requestUrl = $"{url}?key={settings.ApiKey}&steamids={steamId64}";

        try
        {
            HttpResponseMessage response = await httpClient.GetAsync(requestUrl);

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests) // error 429
            {
                Console.WriteLine("STEAM API LIMIT REACHED, PLEASE SWITCH THE KEY");
                return null;
            }

            response.EnsureSuccessStatusCode();
            string jsonContent = await response.Content.ReadAsStringAsync();
            JsonDocument data = JsonDocument.Parse(jsonContent);

            if (data.RootElement.GetProperty("response").GetProperty("players").GetArrayLength() == 0)
            {
                Console.WriteLine($"Non-existent account: {steamId64}");
                return null;
            }

            return data;
        }
        catch (TaskCanceledException) // Timed out
        {
            Console.WriteLine("Connection timed out. Waiting and retrying");
            await Task.Delay(5000);
            return await RequestSummary(steamId64);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return await RequestSummary(steamId64);
        }
    }

    public async Task<bool> QueryUnverified(string steamId, string steamId64, string playerName, JsonDocument dataSummary)
    {
        JsonElement uselessValue;
        if (!dataSummary.RootElement.GetProperty("response").GetProperty("players")[0].TryGetProperty("profilestate", out uselessValue))
        {
            using StreamWriter outputFile = new StreamWriter("out/unverified_accounts.txt", true);
            await outputFile.WriteAsync($"UNVERIFIED ACCOUNT FOUND:\n{steamId} | {playerName} | https://steamcommunity.com/profiles/{steamId64}\n\n");
            Console.WriteLine($"Unverified account found: {steamId} | https://steamcommunity.com/profiles/{steamId64}");
            return true;
        }
        return false;
    }

    public async Task Query(int server, int steamDigit)
    {
        string steamId = $"STEAM_0:{server}:{steamDigit}";
        string steamId64 = Utils.ConvertIdToId64(steamId);

        // Query player summary regardless of everything, because we need the name anyways
        JsonDocument? dataSummary = await RequestSummary(steamId64);
        if (dataSummary == null)
        {
            Console.WriteLine($"Invalid Steam Account: {steamId}");
            return;
        }
        string playerName = dataSummary.RootElement.GetProperty("response").GetProperty("players")[0].GetProperty("personaname").ToString();

        if (settings.UnverifiedAccounts)
        {
            if (await QueryUnverified(steamId, steamId64, playerName, dataSummary))
            {
                return;
            }

        }
    }

    public async Task Run()
    {
        if (!(settings.UnverifiedAccounts || settings.Lvl0Accounts || settings.OldGames || settings.CsgoAccounts) || settings.StartId >= settings.EndId)
        {
            Console.WriteLine("No accounts to search, check the Settings.json file and try again.");
            return;
        }
        if (settings.ApiKey == "")
        {
            Console.WriteLine("No Steam API key in Settings.json.");
            return;
        }
        for (int i = settings.StartId; i <= settings.EndId; i++)
            {
                for (int j = 0; j <= 1; j++)
                {
                    Console.WriteLine($"Checking STEAM_0:{j}_{i}");
                    await Query(j, i);
                    Thread.Sleep(50);
                }
            }

    }
}