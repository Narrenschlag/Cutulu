namespace Cutulu.Network;

using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Linq;
using System;

using Cutulu.Core;

public static class HttpCargo
{
    public static readonly Notification<Import> ReceivedCargo = new();

    private static readonly HttpClient SharedClient = new();

    public static async Task<Import> SeekCargo(this Export cargo)
    {
        if (cargo.IsNull())
        {
            ReceivedCargo.Invoke(Import.Invalid);
            return Import.Invalid;
        }

        // Fix url
        var uri = new Uri(cargo.HttpUrl);

        // Get
        if (cargo.Containers.IsEmpty())
        {
            using HttpResponseMessage response = await SharedClient.GetAsync(uri);

            try
            {
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var buffer = await response.Content.ReadAsByteArrayAsync();

                return new Import(Godot.Error.Ok, body, buffer);
            }

            catch { }
        }

        // Post
        else
        {
            using HttpResponseMessage response = await SharedClient.PostAsync(uri, cargo.Type switch
            {
                CONTAINER_TYPE.JSON => new StringContent(
                    "{" + string.Join(", ", cargo.Containers.Select(x => $"\"{x.Key}\": \"{x.Value}\"")) + "}",
                    System.Text.Encoding.UTF8,
                    "application/json"
                ),

                _ => new FormUrlEncodedContent(cargo.Containers),
            });

            try
            {
                response.EnsureSuccessStatusCode();

                var body = await response.Content.ReadAsStringAsync();
                var buffer = await response.Content.ReadAsByteArrayAsync();

                return new Import(Godot.Error.Ok, body, buffer);
            }

            catch { }
        }

        ReceivedCargo.Invoke(Import.Invalid);
        return Import.Invalid;
    }

    public class Export(string httpUrl, Dictionary<string, string> containers = null, CONTAINER_TYPE type = CONTAINER_TYPE.FORM)
    {
        public Dictionary<string, string> Containers { get; set; } = containers;
        public CONTAINER_TYPE Type { get; set; } = type;
        public string HttpUrl { get; set; } = httpUrl;
    }

    public enum CONTAINER_TYPE : byte
    {
        FORM,
        JSON,
    }

    public class Import(Godot.Error error, string body = default, byte[] buffer = null)
    {
        public Godot.Error Error { get; set; } = error;

        public string Body { get; set; } = body;
        public byte[] Buffer { get; set; } = buffer;

        public bool IsSuccess() => Error == Godot.Error.Ok;

        public static readonly Import Invalid = new(Godot.Error.Timeout);
    }
}