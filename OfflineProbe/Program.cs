using System.Net.Http;

using var client = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(5)
};

try
{
    using HttpResponseMessage response =
        await client.GetAsync("https://www.microsoft.com");

    Console.WriteLine(
        $"INTERNET ACCESSIBLE: {(int)response.StatusCode}");
}
catch (Exception ex)
{
    Console.WriteLine(
        $"INTERNET BLOCKED: {ex.GetType().Name}");
}