using System.Collections;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
string directoryName = string.Empty;
if (Environment.GetCommandLineArgs().Length > 1)
{
    string commandLineOption = Environment.GetCommandLineArgs()[1];
    string commandLineArg = Environment.GetCommandLineArgs()[2];
    if (commandLineOption == "--directory" && !string.IsNullOrEmpty(commandLineArg))
    {
        directoryName = commandLineArg;
    }
}
var listener = new TcpListener(IPAddress.Parse("127.0.0.1"), 4221);
listener.Start();
while (true)
{
    var client = listener.AcceptTcpClient();
    Task.Run(async () => await ProcessClientRequest(client));
}
RequestMessage RequestParser(string message)
{
    RequestMessage requestMessage = new();
    var requestContentSplitByNewLine = message.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
    var request = requestContentSplitByNewLine.FirstOrDefault();
    var userAgent = requestContentSplitByNewLine.Take(3).LastOrDefault();
    if (request == null) return requestMessage;
    requestMessage.UserAgent = userAgent?.Split(":")?.LastOrDefault()?.Trim()!;
    var requestSlitBySpace = request?.Split(' ');
    Console.Write("test content length " + requestContentSplitByNewLine.Length);
    if (requestSlitBySpace != null && requestSlitBySpace.Length == 3)
    {
        requestMessage.Method = requestSlitBySpace[0];
        requestMessage.Url = requestSlitBySpace[1];
    }
    if (requestContentSplitByNewLine.Length == 5)
    {
        requestMessage.Content = requestContentSplitByNewLine[4];
        Console.Write("test content " + requestMessage.Content);
    }
    return requestMessage;
}
async Task ProcessClientRequest(TcpClient client)
{
    var buffer = new byte[10240];
    var stream = client.GetStream();
    var length = stream.Read(buffer, 0, buffer.Length);
    var incomingMessage = Encoding.UTF8.GetString(buffer, 0, length);
    var response = "HTTP/1.1 200 OK\r\n";
    var requestMessage = RequestParser(incomingMessage);
    var contentLength = "Content-Length: {0}\r\n\r\n";
    var contentType = "Content-Type: text/plain\r\n";
    if (requestMessage.Url.StartsWith("/user-agent"))
    {
        response += contentType;
        response += string.Format(contentLength, requestMessage.UserAgent.Length);
        response += requestMessage.UserAgent;
    }
    else if (requestMessage.Url.StartsWith("/echo"))
    {
        var requestUrlSplitByBackSlash = requestMessage.Url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (requestUrlSplitByBackSlash.Length == 2)
        {
            response += contentType;
            response += string.Format(contentLength, requestUrlSplitByBackSlash[1].Length);
            response += requestUrlSplitByBackSlash[1];
        }
    }
    else if (requestMessage.Url.StartsWith("/files") && requestMessage.Method == "POST")
    {
        var fileResponse = string.Empty;
        var requestUrlSplitByBackSlash = requestMessage.Url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (requestUrlSplitByBackSlash.Length == 2)
        {
            string fileName = requestUrlSplitByBackSlash[1];
            string filePath = Path.Combine(directoryName, fileName);
            await File.WriteAllTextAsync(filePath, requestMessage.Content);
            response = string.Empty;
            response = "HTTP/1.1 201 Created\r\n\r\n";
        }
      
    }
    else if (requestMessage.Url.StartsWith("/files") && requestMessage.Method == "GET")
    {
        var fileResponse = string.Empty;
        var requestUrlSplitByBackSlash = requestMessage.Url.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (requestUrlSplitByBackSlash.Length == 2)
        {
            string fileName = requestUrlSplitByBackSlash[1];
            string filePath = Path.Combine(directoryName, fileName);
            var octetContent = "Content-Type: application/octet-stream\r\n";
            if (File.Exists(filePath))
            {
                var fileLength = new System.IO.FileInfo(filePath).Length;
                response += octetContent;
                response += string.Format(contentLength, fileLength);
                string result = System.Text.Encoding.UTF8.GetString(File.ReadAllBytes(filePath));
                response += result;
            }
            else
            {
                response = string.Empty;
                response = "HTTP/1.1 404 Not Found\r\n\r\n";
            }
        }
    }
    else if (requestMessage.Url != "/")
    {
        response = "HTTP/1.1 404 Not Found\r\n\r\n";
    }
    else
    {
        response += "\r\n";
    }
    await stream.WriteAsync(Encoding.UTF8.GetBytes(response));
    Console.WriteLine("request message: {0}", incomingMessage);
    stream.Close();
    client.Close();
}
public class RequestMessage
{
    public string Method
    {
        get;
        set;
    } = string.Empty;
    public string Url
    {
        get;
        set;
    } = string.Empty;
    public string UserAgent
    {
        get;
        set;
    } = string.Empty;
    public string Content
    {
        get;
        set;
    } = string.Empty;
}