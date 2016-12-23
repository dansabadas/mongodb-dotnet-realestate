using System.Web.Mvc;

namespace RealEstate.Controllers
{
    public class HomeController : Controller
    {
        private readonly RealEstateContext Context = new RealEstateContext();

        public ActionResult Index()
        {
            Context.Database.GetStats();
            return Json(Context.Database.Server.BuildInfo, JsonRequestBehavior.AllowGet);
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