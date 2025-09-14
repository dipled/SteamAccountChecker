using System.Text.Json;
class QueryHandler
{
    private Settings _settings;
    private string _ApiKey;

    public QueryHandler()
    {
        using FileStream fileStream = File.OpenRead("Settings.json");
        Settings? settings = JsonSerializer.Deserialize<Settings>(fileStream);
        if (settings == null)
        {
            throw new InvalidOperationException("Failed to deserialize JSON");
        }
        _settings = settings;

        Console.WriteLine("Enter your API key.");
        string? ApiKey = Console.ReadLine();
        if (ApiKey == null)
        {
            throw new InvalidOperationException("Failed to read API key");
        }
        _ApiKey = ApiKey;
        Directory.CreateDirectory("out/");
    }

    public async Task<JsonDocument?> RequestSummary(string steamId64)
    {
        using HttpClient httpClient = new HttpClient();
        httpClient.Timeout = TimeSpan.FromSeconds(30);

        string url = "https://api.steampowered.com/ISteamUser/GetPlayerSummaries/v0002/";
        string requestUrl = $"{url}?key={_ApiKey}&steamids={steamId64}";

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
                return null; //Non-existent account.
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
        string requestUrl = $"{url}?key={_ApiKey}&steamid={steamId64}";

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
        string requestUrl = $"{url}?key={_ApiKey}&steamid={steamId64}";

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
        string requestUrl = $"{url}?key={_ApiKey}&steamid={steamId64}";

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
        int playerLevel = dataLevel.RootElement.GetProperty("response").GetProperty("player_level").GetInt32();

        if (playerLevel > 9)
        {
            return false;
        }

        var badgeOwnedGames = dataBadge.RootElement
        .GetProperty("response")
        .GetProperty("badges")
        .EnumerateArray()
        .Where(badge => badge.GetProperty("badgeid").GetInt32() == 13)
        .ToList();

        if (badgeOwnedGames.Count == 0 || badgeOwnedGames[0].GetProperty("level").GetInt32() > 5)
        {
            return false;
        }

        var gameIdList = dataGames.RootElement
        .GetProperty("response")
        .GetProperty("games")
        .EnumerateArray()
        .Select(game => game.GetProperty("appid").GetInt32())
        .ToList();

        var targetGameIds = new[] { 240, 70, 10 };

        if (targetGameIds.Any(gameIdList.Contains))
        {

            using StreamWriter outputFile = new StreamWriter("out/old_games_accounts.txt", true);
            await outputFile.WriteAsync($"OLD GAMES ACCOUNT FOUND:\n{steamId} | {playerName} | LEVEL: {playerLevel} | https://steamcommunity.com/profiles/{steamId64}\n\n");
            Console.WriteLine($"Account owns CSS, HL or CS 1.6: {steamId} | https://steamcommunity.com/profiles/{steamId64}");
            return true;
        }
        return false;
    }

    public async Task<bool> QueryLevel0(string steamId, string steamId64, string playerName, JsonDocument dataLevel)
    {

        int playerLevel = dataLevel.RootElement.GetProperty("response").GetProperty("player_level").GetInt32();

        if (playerLevel == 0)
        {

            using StreamWriter outputFile = new StreamWriter("out/level_0_accounts.txt", true);
            await outputFile.WriteAsync($"LEVEL 0 ACCOUNT FOUND:\n{steamId} | {playerName} | https://steamcommunity.com/profiles/{steamId64}\n\n");
            Console.WriteLine($"Level 0 account found: {steamId} | https://steamcommunity.com/profiles/{steamId64}");
            return true;
        }
        return false;
    }

    public async Task<bool> QueryCsgo(string steamId, string steamId64, string playerName, JsonDocument dataSummary, JsonDocument dataGames, JsonDocument dataLevel, JsonDocument dataBadge)
    {

        JsonElement country;
        if (dataSummary.RootElement.GetProperty("response").GetProperty("players")[0].TryGetProperty("loccountrycode", out country))
        {
            if (!(country.GetString() == "BR"))
            {
                return false;
            }
        }

        JsonElement realName;
        if (dataSummary.RootElement.GetProperty("response").GetProperty("players")[0].TryGetProperty("realname", out realName))
        {
            string? name = realName.GetString();
            if (name == null)
            {
                return false;
            }

            string[] names = name.Split(" ");
            if (names.Length < 2)
            {
                return false;
            }
        }
        else
        {
            return false;
        }

        int playerLevel = dataLevel.RootElement.GetProperty("response").GetProperty("player_level").GetInt32();
        if (playerLevel > 9)
        {
            return false;
        }

        var badgeOwnedGames = dataBadge.RootElement
        .GetProperty("response")
        .GetProperty("badges")
        .EnumerateArray()
        .Where(badge => badge.GetProperty("badgeid").GetInt32() == 13)
        .ToList();

        if (badgeOwnedGames.Count == 0 || badgeOwnedGames[0].GetProperty("level").GetInt32() > 4)
        {
            return false;
        }

        var gameIdList = dataGames.RootElement
        .GetProperty("response")
        .GetProperty("games")
        .EnumerateArray()
        .Select(game => game.GetProperty("appid").GetInt32())
        .ToList();

        if (gameIdList.Contains(730))
        {
            using StreamWriter outputFile = new StreamWriter("out/csgo_accounts.txt", true);
            await outputFile.WriteAsync($"CSGO ACCOUNT FOUND:\n{steamId} | {playerName} | REAL NAME: {realName.GetString()} | https://steamcommunity.com/profiles/{steamId64}\n\n");
            Console.WriteLine($"CSGO account found: {steamId} | https://steamcommunity.com/profiles/{steamId64}");
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
        if (_settings.UnverifiedAccounts)
        {
            if (await QueryUnverified(steamId, steamId64, playerName, dataSummary))
            {
                return;
            }
        }

        // If we are going to query old games, lvl0 or csgo we might as well hit the level ep already
        if (_settings.Lvl0Accounts || _settings.OldGames || _settings.CsgoAccounts)
        {
            dataLevel = await RequestLevel(steamId64);
            if (dataLevel == null)
            {
                return;
            }
        }

        // Level 0 Query
        if (_settings.Lvl0Accounts)
        {
            if (dataLevel != null)
            {
                if (await QueryLevel0(steamId, steamId64, playerName, dataLevel))
                {
                    return;
                }
            }
        }

        // If we are going to query old games or csgo we request the data for games
        if (_settings.OldGames || _settings.CsgoAccounts)
        {
            dataGames = await RequestGames(steamId64);
            dataBadge = await RequestBadges(steamId64);
            if (dataGames == null)
            {
                return;
            }
            if (dataBadge == null)
            {
                return;
            }
        }

        if (_settings.CsgoAccounts)
        {
            if (dataGames != null && dataLevel != null && dataBadge != null)
            {
                if (await QueryCsgo(steamId, steamId64, playerName, dataSummary, dataGames, dataLevel, dataBadge))
                {
                    return;
                }
            }
            
        }

        // Old Games Query
        if (_settings.OldGames)
        {
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
        if (!(_settings.UnverifiedAccounts || _settings.Lvl0Accounts || _settings.OldGames || _settings.CsgoAccounts) || _settings.StartId >= _settings.EndId)
        {
            Console.WriteLine("No accounts to search, check the Settings.json file and try again.");
            return;
        }
        if (_ApiKey == "")
        {
            Console.WriteLine("No Steam API key in Settings.json.");
            return;
        }
        for (int i = _settings.StartId; i <= _settings.EndId; i++)
            {
                for (int j = 0; j <= 1; j++)
                {
                    Console.WriteLine($"\nChecking STEAM_0:{j}_{i}");
                    await Query(j, i);
                    Thread.Sleep(50);
                }
            }

    }
}