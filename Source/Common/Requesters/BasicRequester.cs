using System.Net.Http.Headers;

using GoodsTracker.DataCollector.Common.Requesters.Abstractions;

using HtmlAgilityPack;

namespace GoodsTracker.DataCollector.Common.Requesters;

internal sealed class BasicRequester : IRequester
{
    internal BasicRequester(HttpClient? client = null)
    {
        this.Client = client ?? new HttpClient();
    }

    private HttpClient Client { get; }

    public async Task<HtmlDocument> GetPageHtmlAsync(string url, Dictionary<string, string>? headers = null)
    {
        using var request = CreateRequest(url, headers);

        HttpResponseMessage response = await this.Client.SendAsync(request)
                                                 .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HtmlWebException($"couldn't get HTML page from {url}");
        }

        var result = new HtmlDocument();

        result.LoadHtml(
            await response.Content.ReadAsStringAsync()
                          .ConfigureAwait(false));

        return result;
    }

    public async Task<string> PostAsync(Uri uri, Dictionary<string, string>? headers = null, string data = "")
    {
        var result = "";
        var request = new HttpRequestMessage(HttpMethod.Post, uri);

        if (headers != null)
        {
            foreach (KeyValuePair<string, string> header in headers)
            {
                request.Headers.Add(header.Key, header.Value);
            }
        }

        request.Content = new StringContent(data);
        request.Content!.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json;charset=utf-8");

        using HttpResponseMessage response = await this.Client.SendAsync(request)
                                                       .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"couldn't perform POST to {uri.AbsoluteUri}");
        }

        using var reader = new StreamReader(
            await response.Content.ReadAsStreamAsync()
                          .ConfigureAwait(false));

        result = await reader.ReadToEndAsync()
                             .ConfigureAwait(false);

        return result;
    }

    public async Task<string> GetAsync(string url, Dictionary<string, string>? headers = null)
    {
        using HttpRequestMessage request = CreateRequest(url, headers);

        using HttpResponseMessage response = await this.Client.SendAsync(request)
                                                       .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"couldn't perform GET to {url}");
        }

        return await response.Content.ReadAsStringAsync()
                             .ConfigureAwait(false);
    }

    private static HttpRequestMessage CreateRequest(string url, Dictionary<string, string>? headers = null)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (headers == null)
        {
            return request;
        }

        foreach (KeyValuePair<string, string> header in headers)
        {
            request.Headers.Add(header.Key, header.Value);
        }

        return request;
    }
}
