using System.Web.Mvc;
using MongoDB.Bson;

namespace RealEstate.Controllers
{
    public class HomeController : Controller
    {
        private static readonly RealEstateContextNewApis Context = new RealEstateContextNewApis();

        public ActionResult Index()
        {
            //Context.Database.GetStats();
            //return Json(Context.Database.Server.BuildInfo, JsonRequestBehavior.AllowGet);

            var buildInfoCommand = new BsonDocument("buildinfo", 1);
            var buildInfo = Context.Database.RunCommand<BsonDocument>(buildInfoCommand);
            return Content(buildInfo.ToJson(), "application/json");
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }
    }
}