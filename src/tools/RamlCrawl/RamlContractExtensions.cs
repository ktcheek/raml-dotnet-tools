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
    }
}