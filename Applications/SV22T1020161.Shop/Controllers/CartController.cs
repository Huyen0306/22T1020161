using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1020161.BusinessLayers;
using SV22T1020161.Models.Sales;
using SV22T1020161.Models.Common;
using SV22T1020161.Models.Catalog;
using SV22T1020161.Shop.Models;
using System.Security.Claims;

namespace SV22T1020161.Shop.Controllers
{
    public class CartController : Controller
    {
        private List<CartItem> GetCart() => CartSessionHelper.GetCart(HttpContext);
        private void SaveCart(List<CartItem> cart) => CartSessionHelper.SaveCart(HttpContext, cart);
        private int GetCartItemCount() => CartSessionHelper.GetCartCount(HttpContext);

        private List<CartItem> GetSelectedItems()
        {
            var cart = GetCart();
            if (Request.Cookies.TryGetValue("selectedCartItems", out string? cookieVal) && !string.IsNullOrWhiteSpace(cookieVal))
            {
                var ids = new HashSet<int>();
                foreach (var idStr in cookieVal.Split(','))
                {
                    if (int.TryParse(idStr.Trim(), out int parsed)) ids.Add(parsed);
                }
                if (ids.Count > 0)
                {
                    return cart.Where(i => ids.Contains(i.ProductID)).ToList();
                }
            }
            return cart;
        }

        public IActionResult Index()
        {
            var cart = GetCart();
            return View(cart);
        }

        /// <summary>
        /// TC-B3: Chưa đăng nhập -> trả về JSON redirect về Login.
        /// TC-B1 (phần Add): Kiểm tra IsSelling, số lượng >= 1.
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Add(int id, int quantity = 1)
        {
            // TC-B3: Chưa đăng nhập -> yêu cầu đăng nhập
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                    message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng."
                });
            }

            // TC-B1: Số lượng phải >= 1
            if (quantity < 1) quantity = 1;

            var cart = GetCart();
            var existing = cart.FirstOrDefault(m => m.ProductID == id);

            if (existing != null)
            {
                existing.Quantity += quantity;
            }
            else
            {
                var product = await CatalogDataService.GetProductAsync(id);
                // TC-B1: Sản phẩm phải tồn tại và đang bán
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }
                if (product.IsSelling != true)
                {
                    return Json(new { success = false, message = "Sản phẩm hiện không còn được bán." });
                }

                cart.Add(new CartItem
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Photo = product.Photo ?? "",
                    Price = product.Price,
                    Unit = product.Unit,
                    Quantity = quantity
                });
            }

            SaveCart(cart);
            var itemCount = GetCartItemCount();
            return Json(new { success = true, itemCount, message = $"Đã thêm \"{cart.Last().ProductName}\" vào giỏ hàng." });
        }

        /// <summary>
        /// TC-B3: Chưa đăng nhập -> trả về JSON redirect về Login.
        /// TC-B1 (phần Update): Số lượng nguyên dương.
        /// </summary>
        [HttpPost]
        public IActionResult Update(int id, int quantity)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                    message = "Vui lòng đăng nhập."
                });
            }

            // TC-B1: Số lượng nguyên dương
            if (quantity < 1) quantity = 1;

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);
            if (item != null)
            {
                item.Quantity = quantity;
                SaveCart(cart);
                return Json(new
                {
                    success = true,
                    subtotal = item.TotalPrice.ToString("N0"),
                    total = cart.Sum(c => c.TotalPrice).ToString("N0")
                });
            }
            return Json(new { success = false });
        }

        /// <summary>
        /// TC-B3: Chưa đăng nhập -> trả về JSON redirect về Login.
        /// </summary>
        [HttpPost]
        public IActionResult Remove(int id)
        {
            if (User?.Identity?.IsAuthenticated != true)
            {
                return Json(new
                {
                    success = false,
                    requireLogin = true,
                    redirectUrl = Url.Action("Login", "Account", new { returnUrl = "/Cart" }),
                    message = "Vui lòng đăng nhập."
                });
            }

            var cart = GetCart();
            var item = cart.FirstOrDefault(m => m.ProductID == id);
            if (item != null)
            {
                cart.Remove(item);
                SaveCart(cart);
                return Json(new
                {
                    success = true,
                    total = cart.Sum(c => c.TotalPrice).ToString("N0"),
                    itemCount = cart.Count
                });
            }
            return Json(new { success = false });
        }

        [HttpPost]
        public IActionResult Clear()
        {
            CartSessionHelper.ClearCart(HttpContext);
            return RedirectToAction("Index");
        }

        /// <summary>
        /// TC-B3: Checkout yêu cầu đăng nhập (dùng [Authorize] attribute).
        /// TC-B2: Giỏ trống -> redirect về Index.
        /// </summary>
        [Authorize]
        public async Task<IActionResult> Checkout()
        {
            var cart = GetSelectedItems();
            if (cart.Count == 0)
            {
                TempData["ErrorMessage"] = "Giỏ hàng trống. Vui lòng thêm sản phẩm trước khi thanh toán.";
                return RedirectToAction("Index");
            }

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId))
                return RedirectToAction("Login", "Account");

            var customer = await PartnerDataService.GetCustomerAsync(userId);
            ViewBag.Cart = cart;
            ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 });

            return View(customer);
        }

        /// <summary>
        /// TC-B3: Checkout khi chưa đăng nhập -> redirect Login -> quay lại.
        /// TC-B1: Lưu Orders + OrderDetails thành công.
        /// TC-B3: Phải chọn Shipper + địa chỉ nhận.
        /// </summary>
        [Authorize]
        [HttpPost]
        public async Task<IActionResult> Confirm(
            string recipientName,
            string recipientPhone,
            string deliveryAddress,
            string deliveryProvince,
            int? shipperID,
            string note = "")
        {
            var cart = GetSelectedItems();
            if (cart.Count == 0) return RedirectToAction("Index");

            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdStr, out int userId)) return RedirectToAction("Login", "Account");

            // TC-B3: Validation bắt buộc
            if (string.IsNullOrWhiteSpace(recipientName))
                ModelState.AddModelError("recipientName", "Vui lòng nhập tên người nhận.");
            if (string.IsNullOrWhiteSpace(recipientPhone))
                ModelState.AddModelError("recipientPhone", "Vui lòng nhập số điện thoại người nhận.");
            if (string.IsNullOrWhiteSpace(deliveryAddress))
                ModelState.AddModelError("deliveryAddress", "Vui lòng nhập địa chỉ giao hàng.");
            if (string.IsNullOrWhiteSpace(deliveryProvince))
                ModelState.AddModelError("deliveryProvince", "Vui lòng chọn tỉnh/thành phố giao hàng.");
            if (!shipperID.HasValue || shipperID.Value <= 0)
                ModelState.AddModelError("shipperID", "Vui lòng chọn đơn vị vận chuyển.");

            if (!ModelState.IsValid)
            {
                // Load lai du lieu cho form
                var customer = await PartnerDataService.GetCustomerAsync(userId);
                ViewBag.Cart = cart;
                ViewBag.Shippers = await PartnerDataService.ListShippersAsync(new PaginationSearchInput { PageSize = 100 });
                ViewData["ValidationErrors"] = ModelState.ToDictionary(
                    kvp => kvp.Key,
                    kvp => kvp.Value?.Errors.Select(e => e.ErrorMessage).ToArray() ?? Array.Empty<string>()
                );
                return View("Checkout", customer);
            }

            // Build delivery info
            var fullDeliveryAddress = $"{recipientName} — {recipientPhone} — {deliveryAddress}";

            // Create Order
            var order = new Order
            {
                CustomerID = userId,
                DeliveryAddress = fullDeliveryAddress,
                DeliveryProvince = deliveryProvince,
                ShipperID = shipperID,
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New
            };

            int orderID = await SalesDataService.AddOrderAsync(order);

            // Add OrderDetails
            foreach (var item in cart)
            {
                await SalesDataService.AddDetailAsync(new OrderDetail
                {
                    OrderID = orderID,
                    ProductID = item.ProductID,
                    Quantity = item.Quantity,
                    SalePrice = item.Price
                });
            }

            // Remove only purchased items
            var fullCart = GetCart();
            foreach (var item in cart)
            {
                var c = fullCart.FirstOrDefault(x => x.ProductID == item.ProductID);
                if (c != null) fullCart.Remove(c);
            }
            SaveCart(fullCart);
            // Clear the cookie so it doesn't affect future selections
            Response.Cookies.Delete("selectedCartItems");

            TempData["SuccessMessage"] = $"Đặt hàng thành công! Mã đơn hàng #{orderID}. Đơn hàng đang được chờ duyệt.";
            return RedirectToAction("Status", "Order", new { id = orderID });
        }

        /// <summary>
        /// Tra ve so luong san pham trong gio (cho client cap nhat badge).
        /// </summary>
        public IActionResult GetCartCount()
        {
            if (User?.Identity?.IsAuthenticated != true)
                return Json(new { count = 0 });
            return Json(new { count = GetCartItemCount() });
        }
    }
}
