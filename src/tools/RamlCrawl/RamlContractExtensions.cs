using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using AMF.Common;
using AMF.Tools.Core.WebApiGenerator;

namespace RamlCrawl
{
    public static class RamlContractExtensions
    {
        public static WebApiGeneratorModel ToWebApiGeneratorModel(this RamlContract ramlContract)
        {
            var ramlInfo = RamlInfoService.GetRamlInfo(ramlContract.RamlFile).Result;
            var model = new WebApiGeneratorService(
                    ramlInfo.RamlDocument, 
                    ramlContract.ControllersNamespace,
                    ramlContract.ModelsNamespace)
                .BuildModel();

            return model;
        }

        private static async Task DownloadFile(string url, string destinationPath)
        {
            using var webClient = new WebClient();
            var downloadedString = await webClient.DownloadStringTaskAsync(url);
            var contents = string.Join(
                Environment.NewLine,
                downloadedString.TrimEnd('\r', '\n').Split(new[] {Environment.NewLine, "\n"}, StringSplitOptions.None)) + Environment.NewLine;
            File.WriteAllText(destinationPath, contents);
        }

        public static Task DownloadRamlFileFromSource(this RamlContract ramlContract) =>
            ramlContract.Parameters.TryGetValue("source", out var url) && url.StartsWith("http")
                ? DownloadFile(url, ramlContract.RamlFile)
                : Task.CompletedTask;
    }
}