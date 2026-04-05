using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Sales;
using System.Security.Claims;
using SV22T1020161.Shop.Models;
using SV22T1020161.Models.Catalog;

namespace SV22T1020161.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        private const int PAGE_SIZE = 10;

        private List<CartItem> GetCart() => CartSessionHelper.GetCart(HttpContext);
        private void SaveCart(List<CartItem> cart) => CartSessionHelper.SaveCart(HttpContext, cart);

        public async Task<IActionResult> History(int status = 0, int page = 1, string searchValue = "")
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var input = new OrderSearchInput
            {
                Page = page,
                PageSize = PAGE_SIZE,
                Status = (OrderStatusEnum)status,
                SearchValue = searchValue,
                CustomerID = userId
            };

            var data = await SalesDataService.ListOrdersAsync(input);
            ViewBag.Status = status;
            ViewBag.SearchValue = searchValue;

            // Fetch details for each order to show products in cards
            var detailsDict = new Dictionary<int, List<OrderDetailViewInfo>>();
            foreach (var order in data.DataItems)
            {
                var details = await SalesDataService.ListDetailsAsync(order.OrderID);
                detailsDict[order.OrderID] = details;
            }
            ViewBag.OrderDetails = detailsDict;

            return View(data);
        }

        public async Task<IActionResult> Status(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null || order.CustomerID != userId)
            {
                return RedirectToAction("History");
            }

            ViewBag.Details = await SalesDataService.ListDetailsAsync(id);
            return View(order);
        }

        /// <summary>
        /// Customer cancels their own new order
        /// </summary>
        public async Task<IActionResult> Cancel(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null || order.CustomerID != userId)
            {
                return RedirectToAction("History");
            }

            // Only allow cancellation of NEW or ACCEPTED orders (customer can still cancel before shipping)
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                TempData["ErrorMessage"] = "Đơn hàng đã được giao cho đơn vị vận chuyển, không thể hủy.";
                return RedirectToAction("Status", new { id });
            }

            bool ok = await SalesDataService.CancelOrderAsync(id);
            if (ok)
            {
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng. Vui lòng thử lại.";
            }

            return RedirectToAction("History");
        }

        /// <summary>
        /// Re-order: add items from a previous order to current cart
        /// </summary>
        public async Task<IActionResult> Reorder(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null || order.CustomerID != userId)
            {
                return RedirectToAction("History");
            }

            var details = await SalesDataService.ListDetailsAsync(id);
            if (details.Count == 0)
            {
                TempData["ErrorMessage"] = "Đơn hàng này không có sản phẩm nào.";
                return RedirectToAction("History");
            }

            var cart = GetCart();
            int addedCount = 0;

            foreach (var d in details)
            {
                // Check if product still exists and is selling
                var product = await CatalogDataService.GetProductAsync(d.ProductID);
                if (product != null && product.IsSelling == true)
                {
                    var existing = cart.FirstOrDefault(c => c.ProductID == d.ProductID);
                    if (existing != null)
                        existing.Quantity += d.Quantity;
                    else
                        cart.Add(new CartItem
                        {
                            ProductID = product.ProductID,
                            ProductName = product.ProductName,
                            Photo = product.Photo ?? "",
                            Price = product.Price,
                            Unit = product.Unit,
                            Quantity = d.Quantity
                        });
                    addedCount++;
                }
            }

            SaveCart(cart);

            if (addedCount > 0)
            {
                TempData["SuccessMessage"] = $"Đã thêm {addedCount} sản phẩm vào giỏ hàng từ đơn #{id}.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không có sản phẩm nào có thể thêm vào giỏ (sản phẩm có thể đã ngừng bán).";
            }

            return RedirectToAction("Index", "Cart");
        }
    }
}
