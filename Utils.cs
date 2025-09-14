class Utils
{
    public static string ConvertIdToId64(string steamId)
    {
        steamId.Replace("STEAM_", "");
        string[] split = steamId.Split(":");
        if (split.Length != 3)
        {
            throw new ArgumentException("Invalid SteamId format");
        }
        return (76561197960265728 + int.Parse(split[2]) * 2 + int.Parse(split[1])).ToString();
    }
}
