using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Core;

class RequestsListenerHttp
{
    private readonly Executor _executor;
    private readonly Parser _parser;

    public RequestsListenerHttp(Executor executor,  Parser parser)
    {
        _executor = executor;
        _parser = parser;
    }

    public void Listen()
    {
        var httpListener = new HttpListener();
        httpListener.Prefixes.Add(BotSettings.GetSettings().MessagesEndpoint);
        httpListener.Start();

        while (httpListener.IsListening)
        {
            var context = httpListener.GetContext();
            HandleRequest(context);
        }
        
        Console.WriteLine("stop listening");
    }

    private void HandleRequest(HttpListenerContext context)
    {
        Action handler = () =>
        {
            var request = context.Request;
            var response = context.Response;

            if (request.HttpMethod != "POST")
            {
                WriteOkResponse(response.OutputStream);
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var data = reader.ReadToEnd();
            var jObject = JObject.Parse(data);
            
            var commands = new List<Command>();
            _parser.ParseMessageTo(jObject, commands);
            foreach (var command in commands)
                _executor.Execute(command);
            
            WriteOkResponse(response.OutputStream);
        };

        Task.Run(handler);
    }

    private void WriteOkResponse(Stream stream)
    {
        using var writer = new StreamWriter(stream);
        writer.Write("ok");
    }
}