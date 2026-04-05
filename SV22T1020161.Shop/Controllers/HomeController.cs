using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SV22T1020161.Shop.Models;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Catalog;
using SV22T1020161.Models.Common;

namespace SV22T1020161.Shop.Controllers;

public class HomeController : Controller
{
    private const int PAGE_SIZE = 12;

    public async Task<IActionResult> Index(
        int page = 1,
        int categoryID = 0,
        string searchValue = "",
        decimal minPrice = 0,
        decimal maxPrice = 0)
    {
        // Load categories for sidebar filter
        var categories = await CatalogDataService.ListCategoriesAsync(
            new PaginationSearchInput { Page = 1, PageSize = 100 });

        // Load products with filters
        var input = new ProductSearchInput
        {
            Page = page,
            PageSize = PAGE_SIZE,
            SearchValue = searchValue ?? "",
            CategoryID = categoryID,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        };
        var products = await CatalogDataService.ListProductsAsync(input);

        // Pass to view
        ViewBag.Categories = categories;
        ViewBag.ProductResult = products;
        ViewBag.CurrentCategoryID = categoryID;
        ViewBag.CurrentSearchValue = searchValue;
        ViewBag.CurrentMinPrice = minPrice;
        ViewBag.CurrentMaxPrice = maxPrice;

        return View();
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

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
