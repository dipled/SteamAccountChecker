class Settings
{
    public required int StartId { get; set; }
    public required int EndId { get; set; }
    public required bool UnverifiedAccounts { get; set; }
    public required bool Lvl0Accounts { get; set; }
    public required bool OldGames { get; set; }
    public required bool CsgoAccounts { get; set; }
    public required int NumThreads { get; set; }

    public override string ToString()
    {
        return $"StartId: {StartId}, EndId: {EndId}, NumThreads: {NumThreads}";
    }
}