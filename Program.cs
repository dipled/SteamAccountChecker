class Program
{
    public static async Task Main(string[] args)
    {
        QueryHandler queryHandler = new QueryHandler();
        await queryHandler.Run();
    }
}
