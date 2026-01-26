using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [Route(template: "magaza")]
    public class ShopController : Controller
    {
        // Ana mağaza sayfası: /magaza
        [Route("")]
        public IActionResult Index()
        {
            return View();
        }

        // Tüm filtre kombinasyonları için catch-all route
        // Örnekler:
        // /magaza/kategori/motor
        // /magaza/kategori/motor/motor-yaglari
        // /magaza/marka/fiat/model/egea
        // /magaza/kategori/motor/uretici/opar
        // /magaza/marka/fiat/model/egea/versiyon/1.4/uretici/opar
        [Route("{*path}")]
        public IActionResult Filter(string path)
        {
            if (string.IsNullOrEmpty(path))
                return View("Index");

            var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            
            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == "kategori" && i + 1 < segments.Length)
                {
                    ViewBag.MainCategoryName = segments[i + 1];
                    i++;
                    // Alt kategori kontrolü (sonraki segment keyword değilse alt kategoridir)
                    if (i + 1 < segments.Length && 
                        segments[i + 1] != "marka" && 
                        segments[i + 1] != "uretici" &&
                        segments[i + 1] != "model" &&
                        segments[i + 1] != "versiyon")
                    {
                        ViewBag.SubCategoryName = segments[i + 1];
                        i++;
                    }
                }
                else if (segments[i] == "marka" && i + 1 < segments.Length)
                {
                    ViewBag.BrandName = segments[i + 1];
                    i++;
                }
                else if (segments[i] == "model" && i + 1 < segments.Length)
                {
                    ViewBag.ModelName = segments[i + 1];
                    i++;
                }
                else if (segments[i] == "versiyon" && i + 1 < segments.Length)
                {
                    ViewBag.VersionName = segments[i + 1];
                    i++;
                }
                else if (segments[i] == "uretici" && i + 1 < segments.Length)
                {
                    ViewBag.ManufacturerName = segments[i + 1];
                    i++;
                }
            }
            
            return View("Index");
        }
    }
}