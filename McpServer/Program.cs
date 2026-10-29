using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Rag;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// TODO(rag): move the knowledge folder to configuration and index in the background instead of blocking startup.
var knowledgeFolder = Path.Combine(AppContext.BaseDirectory, "knowledge");
var index = new RagIndex();
if (Directory.Exists(knowledgeFolder))
    index.IngestFolder(knowledgeFolder);
builder.Services.AddSingleton(index);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<RandomNumberTools>()
    .WithTools<RagTools>();

await builder.Build().RunAsync();
