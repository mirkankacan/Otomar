using Microsoft.AspNetCore.Mvc;

namespace Otomar.WebApp.Controllers
{
    [Route(template: "magaza")]
    public class ShopController : Controller
    {
        // Ana mağaza sayfası: /magaza
        public IActionResult Index()
        {
            return View();
        }

        // Kategori filtrelemesi: /magaza/kategori/yag-filtresi
        [Route("kategori/{mainCategoryName}")]
        public IActionResult Category(string mainCategoryName)
        {
            ViewBag.MainCategoryName = mainCategoryName;
            return View("Index");
        }

        // Alt kategori filtrelemesi: /magaza/kategori/yag-filtresi/motor-yaglari
        [Route("kategori/{mainCategoryName}/{subCategoryName}")]
        public IActionResult SubCategory(string mainCategoryName, string subCategoryName)
        {
            ViewBag.MainCategoryName = mainCategoryName;
            ViewBag.SubCategoryName = subCategoryName;
            return View("Index");
        }

        // Marka filtrelemesi: /magaza/marka/fiat
        [Route("marka/{brandName}")]
        public IActionResult Brand(string brandName)
        {
            ViewBag.BrandName = brandName;
            return View("Index");
        }

        // Model filtrelemesi: /magaza/marka/fiat/model/egea
        [Route("marka/{brandName}/model/{modelName}")]
        public IActionResult Model(string brandName, string modelName)
        {
            ViewBag.BrandName = brandName;
            ViewBag.ModelName = modelName;
            return View("Index");
        }

        // Versiyon filtrelemesi: /magaza/marka/fiat/model/egea/versiyon/egea-cross-2021
        [Route("marka/{brandName}/model/{modelName}/versiyon/{versionName}")]
        public IActionResult Version(string brandName, string modelName, string versionName)
        {
            ViewBag.BrandName = brandName;
            ViewBag.ModelName = modelName;
            ViewBag.VersionName = versionName;
            return View("Index");
        }

        // Üretici marka filtrelemesi: /magaza/uretici/opar
        [Route("uretici/{manufacturerName}")]
        public IActionResult Manufacturer(string manufacturerName)
        {
            ViewBag.ManufacturerName = manufacturerName;
            return View("Index");
        }
    }
}