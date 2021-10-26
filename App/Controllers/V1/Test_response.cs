using GOSLibraries.URI;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Puchase_and_payables.Controllers.V1
{


    [ApiExplorerSettings(IgnoreApi = true)]
    public class Test_response : Controller
    {
        private readonly IBaseURIs _uRIs;
        public Test_response(IBaseURIs uRIs, IConfiguration configuration)
        {
            Configuration = configuration;
            _uRIs = uRIs;
        }

        private readonly IConfiguration Configuration;
        [HttpGet("test_response/purchase_payables/get/baseurls")]
        public BaseURIs return_app_urls()
        {
            return new BaseURIs
            {
                FlutterWave = _uRIs.FlutterWave,
                LiveGateway = _uRIs.LiveGateway,
                LocalGateway = _uRIs.LocalGateway,
                MainClient = _uRIs.MainClient,
                Other = _uRIs.Other,
                PayStack = _uRIs.PayStack,
                SelfClient = _uRIs.SelfClient,
            };
        }


        [HttpGet("test_response/purchase_payables/get/connectionstring")]
        public string return_app_connectionstring()
        {
            var connection = Configuration.GetConnectionString("DefaultConnection");
            return connection;
        }
    }
}
