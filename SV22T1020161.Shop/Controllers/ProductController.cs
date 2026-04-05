using Microsoft.AspNetCore.Mvc;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Catalog;
using SV22T1020161.Models.Common;

namespace SV22T1020161.Shop.Controllers;

public class ProductController : Controller
{
    private const int PAGE_SIZE = 12;

    public async Task<IActionResult> Index(int page = 1, int categoryID = 0, string searchValue = "")
    {
        var input = new ProductSearchInput()
        {
            Page = page,
            PageSize = PAGE_SIZE,
            SearchValue = searchValue ?? "",
            CategoryID = categoryID,
            MinPrice = 0,
            MaxPrice = 0
        };
        ViewBag.Categories = await CatalogDataService.ListCategoriesAsync(new PaginationSearchInput { Page = 1, PageSize = 100 });
        return View(input);
    }

    /// <summary>
    /// Partial view tra ve danh sach san pham (Ajax)
    /// TC-C3: Kiem tra MaxPrice >= MinPrice.
    /// </summary>
    public async Task<IActionResult> Search(ProductSearchInput input)
    {
        // TC-C3: Giá kết thúc không được nhỏ hơn giá khởi đầu
        if (input.MaxPrice > 0 && input.MinPrice > 0 && input.MaxPrice < input.MinPrice)
        {
            input.MaxPrice = 0;
        }
        // Khong cho am
        if (input.MinPrice < 0) input.MinPrice = 0;
        if (input.MaxPrice < 0) input.MaxPrice = 0;

        input.PageSize = PAGE_SIZE;
        var data = await CatalogDataService.ListProductsAsync(input);
        return PartialView("_ProductGrid", data);
    }

    public async Task<IActionResult> Detail(int id)
    {
        var product = await CatalogDataService.GetProductAsync(id);
        if (product == null) return RedirectToAction("Index");

        ViewBag.Photos = await CatalogDataService.ListPhotosAsync(id);
        ViewBag.Attributes = await CatalogDataService.ListAttributesAsync(id);
        
        // Fetch Supplier and Category for better display
        if (product.SupplierID.HasValue && product.SupplierID.Value > 0)
            ViewBag.Supplier = await PartnerDataService.GetSupplierAsync(product.SupplierID.Value);
            
        if (product.CategoryID.HasValue && product.CategoryID.Value > 0)
            ViewBag.Category = await CatalogDataService.GetCategoryAsync(product.CategoryID.Value);

        return View(product);
    }
}
