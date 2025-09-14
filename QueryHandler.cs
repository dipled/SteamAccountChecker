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

    public async Task<JsonDocument?> RequestLevel(string steamId64)
    {
        using HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string url = "https://api.steampowered.com/IPlayerService/GetSteamLevel/v1/";
        string requestUrl = $"{url}?key={settings.ApiKey}&steamid={steamId64}";

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
            if (data.RootElement.GetProperty("response").TryGetProperty("player_level", out _))
            {
                return data;
            }
            return null;
        }
        catch (TaskCanceledException) // Timed out
        {
            Console.WriteLine("Connection timed out. Waiting and retrying");
            await Task.Delay(5000);
            return await RequestLevel(steamId64);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return await RequestLevel(steamId64);
        }
    }

    public async Task<JsonDocument?> RequestGames(string steamId64)
    {
        using HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string url = "https://api.steampowered.com/IPlayerService/GetOwnedGames/v0001/";
        string requestUrl = $"{url}?key={settings.ApiKey}&steamid={steamId64}";

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
            if (data.RootElement.GetProperty("response").TryGetProperty("games", out _))
            {
                return data;
            }
            return null;
        }
        catch (TaskCanceledException) // Timed out
        {
            Console.WriteLine("Connection timed out. Waiting and retrying");
            await Task.Delay(5000);
            return await RequestGames(steamId64);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return await RequestGames(steamId64);
        }
    }

    public async Task<JsonDocument?> RequestBadges(string steamId64)
    {
        using HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string url = "https://api.steampowered.com/IPlayerService/GetBadges/v1/";
        string requestUrl = $"{url}?key={settings.ApiKey}&steamid={steamId64}";

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
            if (data.RootElement.GetProperty("response").TryGetProperty("badges", out _))
            {
                return data;
            }
            return null;
        }
        catch (TaskCanceledException) // Timed out
        {
            Console.WriteLine("Connection timed out. Waiting and retrying");
            await Task.Delay(5000);
            return await RequestBadges(steamId64);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error occurred: {ex.Message}");
            return await RequestBadges(steamId64);
        }
    }

    public async Task<bool> QueryUnverified(string steamId, string steamId64, string playerName, JsonDocument dataSummary)
    {
        if (!dataSummary.RootElement.GetProperty("response").GetProperty("players")[0].TryGetProperty("profilestate", out _))
        {
            using StreamWriter outputFile = new StreamWriter("out/unverified_accounts.txt", true);
            await outputFile.WriteAsync($"UNVERIFIED ACCOUNT FOUND:\n{steamId} | {playerName} | https://steamcommunity.com/profiles/{steamId64}\n\n");
            Console.WriteLine($"Unverified account found: {steamId} | https://steamcommunity.com/profiles/{steamId64}");
            return true;
        }
        return false;
    }

    public async Task<bool> QueryOldGames(string steamId, string steamId64, string playerName, JsonDocument dataGames, JsonDocument dataLevel, JsonDocument dataBadge)
    {

        if (dataLevel == null || dataBadge == null || dataGames == null)
        {
            return false;
        }

        if (dataLevel.RootElement.GetProperty("response").GetProperty("player_level").GetInt32() > 9)
        {
            return false;
        }

        var gameIdList = dataGames.RootElement
        .GetProperty("response")
        .GetProperty("games")
        .EnumerateArray()
        .Select(game => game.GetProperty("appid").GetInt32())
        .ToList();

        var badgeOwnedGames = dataBadge.RootElement
        .GetProperty("response")
        .GetProperty("badges")
        .EnumerateArray()
        .Where(badge => badge.GetProperty("badgeid").GetInt32() == 13)
        .ToList();

        if (badgeOwnedGames.Count == 0)
        {
            return false;
        }

        var targetGameIds = new[] { 240, 70, 10 };

        if (targetGameIds.Any(gameIdList.Contains) &&
            badgeOwnedGames[0].GetProperty("level").GetInt32() <= 5)
        {

            using StreamWriter outputFile = new StreamWriter("out/old_games_accounts.txt", true);
            await outputFile.WriteAsync($"OLD GAMES ACCOUNT FOUND:\n{steamId} | {playerName} | https://steamcommunity.com/profiles/{steamId64}\n\n");
            Console.WriteLine($"Account owns CSS, HL or CS 1.6: {steamId} | https://steamcommunity.com/profiles/{steamId64}");
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
        JsonDocument? dataLevel = null;
        JsonDocument? dataGames = null;
        JsonDocument? dataBadge = null;

        if (dataSummary == null)
        {
            Console.WriteLine($"Invalid Steam Account: {steamId}");
            return;
        }
        string playerName = dataSummary.RootElement.GetProperty("response").GetProperty("players")[0].GetProperty("personaname").ToString();

        // Unverified accounts query
        if (settings.UnverifiedAccounts)
        {
            if (await QueryUnverified(steamId, steamId64, playerName, dataSummary))
            {
                return;
            }
        }

        // If we are going to query old games, lvl0 or csgo we might as well hit the level ep already
        if (settings.Lvl0Accounts || settings.OldGames || settings.CsgoAccounts)
        {
            dataLevel = await RequestLevel(steamId64);
            if (dataLevel == null)
            {
                return;
            }
        }

        // If we are going to query old games or csgo we request the data for games
        if (settings.OldGames || settings.CsgoAccounts)
        {
            dataGames = await RequestGames(steamId64);
            if (dataGames == null)
            {
                return;
            }
        }

        // Old Games Query
        if (settings.OldGames)
        {
            dataBadge = await RequestBadges(steamId64);
            if (dataGames != null && dataLevel != null && dataBadge != null)
            {
                if (await QueryOldGames(steamId, steamId64, playerName, dataGames, dataLevel, dataBadge))
                {
                    return;
                }
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