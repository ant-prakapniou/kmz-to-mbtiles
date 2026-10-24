using System.Text;
using Kmz2Mbtiles.Cli;
using Kmz2Mbtiles.Cli.DependencyInjection;
using Kmz2Mbtiles.Core.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SQLitePCL;

Batteries_V2.Init();
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
Console.OutputEncoding = Encoding.UTF8;

var services = new ServiceCollection();
services.AddKmz2MbtilesCore();
services.AddKmz2MbtilesCli();

var provider = services.BuildServiceProvider();
var app = provider.GetRequiredService<CliApp>();

return app.Run(args);