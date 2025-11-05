using BusinessCentralPlugin.Helper;
using powerGateServer.SDK;

namespace BusinessCentralPlugin
{
    //netsh http add urlacl url=http://+:8080/PGS user=\Everyone
    //netsh http add urlacl url=http://+:8080/coolOrange user=\Everyone
    [WebServiceData("PGS", "BusinessCentral")]
    public class WebService : powerGateServer.SDK.WebService
    {
        public WebService()
        {
            AddMethod(new Items());
            AddMethod(new BomHeaders());
            AddMethod(new BomRows());
            AddMethod(new Documents());

            Configuration.Initialize();
        }
    }
}