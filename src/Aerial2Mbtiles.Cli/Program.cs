using Aerial2Mbtiles.Cli;
using Aerial2Mbtiles.Cli.DependencyInjection;
using Aerial2Mbtiles.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;
using System.Text;

// Initialize SQLitePCL before any SQLite call (required by Microsoft.Data.Sqlite.Core).
Batteries_V2.Init();

// Code page 437 is required by ZipHelper for reading KMZ entry names written by Google Earth.
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var services = new ServiceCollection();
services.AddAerial2MbtilesCore();
services.AddAerial2MbtilesCli();

var provider = services.BuildServiceProvider();
var app = provider.GetRequiredService<CliApp>();

return app.Run(args);