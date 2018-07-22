using System;
using System.Globalization;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using AritySystems.Models;
using AritySystems.Data;
using System.Collections.Generic;
using AritySystems.Common;

namespace AritySystems.Controllers
{
    //[Authorize]
    public class OrderController : Controller
    {

        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// Order listing landing page
        /// </summary>
        /// <returns></returns>
        public ActionResult OrderList()
        {
            return View();
        }

        /// <summary>
        /// Get order details for customer
        /// </summary>
        /// <returns></returns>
        public JsonResult GetOrderList()
        {
            ArityEntities objDb = new ArityEntities();
            var orderLst = (from order in objDb.Orders.OrderByDescending(_=>_.CreatedDate).ToList()
                            select new
                            {
                                Id = order.Id,
                                Prefix = order.Prefix,
                                CreatedDate = order.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
                                Status = order.Status,
                                TotalItem = order.OrderLineItems.Sum(_ => _.Quantity),
                                Total = order.OrderLineItems.Sum(_ => (_.DollarSalesPrice * _.Quantity))
                            }).ToList();
            return Json(new { data = orderLst }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Order details
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult OrderDetail(int id)
        {
            ArityEntities objDb = new ArityEntities();
            return View(objDb.Orders.Where(_ => _.Id == id).FirstOrDefault());
        }
        /// <summary>
        /// Place order landing page
        /// </summary>
        /// <returns></returns>
        public ActionResult MakeOrder()
        {
            ArityEntities objDb = new ArityEntities();
            ViewBag.Products = new SelectList(objDb.Products.Where(_ => _.Parent_Id == 0).ToList(), "Id", "English_Name");
            return View();
            
        }

        public ActionResult OrderLineItems(int? OrderId)
        {
            using (var db = new ArityEntities())
            {
                ViewBag.OrderName = db.Orders.Where(x => x.Id == OrderId).Select(x => x.Prefix).FirstOrDefault();
                ViewBag.OrderDate = db.Orders.Where(x => x.Id == OrderId).Select(x => x.CreatedDate).FirstOrDefault();
                ViewBag.Status = db.Orders.Where(x => x.Id == OrderId).Select(x => x.Status).FirstOrDefault();
            }
            ViewBag.OrderId = OrderId;
            return View();
        }


        /// <summary>
        /// Order Line Item list and supplier order line item list with order details
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult OrderLineItemList(int OrderId)
        {
            try
            {
                using (var db = new ArityEntities())
                {
                   
                    var model = (from m in db.OrderLineItems
                                 join n in db.Orders on m.OrderId equals n.Id
                                 join o in db.Products on m.ProductId equals o.Id
                                 where m.OrderId == OrderId && m.Quantity > 0
                                 select new OrderLineItemViewModel {
                                     Id = m.Id,
                                     //OrderId = m.OrderId ?? 0,
                                     Order_Name = n.Prefix,
                                     //ProductId = m.ProductId ?? 0,
                                     Product_Name = o.English_Name + "(" + o.Chinese_Name + ")",
                                     Purchase_Price_dollar = m.DollarPurchasePrice,
                                     Sales_Price_dollar = m.DollarSalesPrice,
                                     Purchase_Price_rmb = m.RMDPurchasePrice,
                                     Sales_Price_rmb = m.RMBSalesPrice,
                                     quantity = m.Quantity,
                                     //CreatedDate = m.CreatedDate.ToString(),
                                     //ModifiedDate = m.ModifiedDate ?? DateTime.MinValue,
                                     Suppliers = (from a in db.Users join b in db.UserTypes on a.Id equals b.UserId
                                                  where b.Id == 1
                                                  select new SelectListItem{
                                                     Text = a.Id.ToString(), Value = a.FirstName+" "+a.LastName
                                                  }).ToList()
                                 }).ToList();
                    
                    return Json(new { data = model }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Supplier Order List items
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SuppliersOrderLineItemList(int OrderId)
        {
            try
            {
                using (var db = new ArityEntities())
                {
                    var model = (from a in db.Supplier_Assigned_OrderLineItem
                                 join b in db.OrderLineItem_Supplier_Mapping on a.OrderSupplierMapId equals b.Id
                                 join c in db.OrderLineItems on b.OrderLineItemId equals c.Id
                                 join d in db.Orders on c.OrderId equals d.Id
                                 where c.OrderId == OrderId && c.Quantity > 0
                                 select new SupplierOrderLineItemModel
                                 {
                                     Id = a.Id,
                                     //CreatedDate = a.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
                                     //ModifiedDate = a.ModifiedDate.ToString("MM/dd/yyyy h:m tt"),
                                     OrderSupplierMapId = a.OrderSupplierMapId,
                                     Order_Prefix = d.Prefix,
                                     Quantity = a.Quantity,
                                     Status = a.Status,
                                     SupplierId = a.SupplierId,
                                     SupplierName = b.User.UserName
                                 }).ToList();

                    return Json(new { data = model }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        /// <summary>
        /// Add supplier order line items
        /// </summary>
        /// <param name="SupplierOrderLineItemModel"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult AddSupplierOrderLineItems(List<SupplierOrderItemAdd> data)
        {
            Supplier_Assigned_OrderLineItem model = new Supplier_Assigned_OrderLineItem();
            try
            {
                if (data != null)
                {
                    foreach (var item in data)
                    {
                        using (var db = new ArityEntities())
                        {
                            var orderItemId = Convert.ToInt32(item.OrderLineItemId);
                            var quantity = Convert.ToDecimal(item.Quantity);
                            OrderLineItem ActualQuantity = db.OrderLineItems.Where(x => x.Id == orderItemId).FirstOrDefault();
                            if (ActualQuantity.Quantity > quantity)
                            {
                                ActualQuantity.Quantity = ActualQuantity.Quantity - quantity;
                                db.SaveChanges();
                            }
                            
                            model.OrderSupplierMapId = Convert.ToInt32(item.OrderLineItemId);
                            model.Quantity = quantity;
                            model.Status = 1;
                            model.SupplierId = Convert.ToInt32(item.SupplierId);
                            model.CreatedDate = DateTime.Now;
                            model.ModifiedDate = DateTime.Now;
                            db.Supplier_Assigned_OrderLineItem.Add(model);
                            db.SaveChanges();
                        }
                    }
                }
                return Json(new { data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }            
        }
        

        /// <summary>
        /// Add supplier carton details
        /// </summary>
        /// <param name="supplierCarton"></param>
        /// <returns></returns>
        public JsonResult AddSupplierCartoonDetails(SupplierCartoon supplierCartoon)
        {
            SupplierCartoon data = new SupplierCartoon();
            try
            {
                using (var db = new ArityEntities())
                {
                   
                    data.CartoonBM = supplierCartoon.CartoonBM;
                    data.CartoonNumber = supplierCartoon.CartoonNumber;
                    data.CartoonSize = supplierCartoon.CartoonSize;
                    data.CreatedDate = DateTime.UtcNow;
                    data.GrossWeight = supplierCartoon.GrossWeight;
                    data.ModifiedDate = DateTime.UtcNow;
                    data.NetWeight = supplierCartoon.NetWeight;
                    data.PcsPerCartoon = supplierCartoon.PcsPerCartoon;
                    data.Status = supplierCartoon.Status;
                    data.SupplierAssignedMapId = supplierCartoon.SupplierAssignedMapId;
                    data.TotalCartoons = supplierCartoon.TotalCartoons;
                    data.TotalGrossWeight = supplierCartoon.TotalGrossWeight;
                    data.TotalNetWeight = supplierCartoon.TotalNetWeight;
                    db.SupplierCartoons.Add(data);
                    db.SaveChanges();
                }
                return Json(new { data = data }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public List<SelectListItem> SupplierDD()
        {
            List<SelectListItem> list = new List<SelectListItem>();
            try
            {
                using (var db = new ArityEntities())
                {
                    var data = (from a in db.Users
                                join b in db.UserTypes on a.Id equals b.UserId
                                where b.Id == 4
                                select new SelectListItem
                                {
                                    Text = a.UserName,
                                    Value = a.Id.ToString()
                                });
                }
                return list;
            }
            catch (Exception ex)
            { throw; }
        }

        /// <summary>
        /// Get product for order
        /// </summary>
        /// <param name="id"></param>
        /// <param name="qty"></param>
        /// <returns></returns>
        public JsonResult AddProductToCart(int id, int qty)
        {
            ArityEntities objDb = new ArityEntities();
            var product = (from pro in objDb.Products.ToList()
                           where pro.Id == id
                           select new Product
                           {
                               Dollar_Price = pro.Dollar_Price
                           }).FirstOrDefault();
            var items = objDb.Products.Where(_ => _.Id == id).Union(objDb.Products.Where(_ => _.Parent_Id == id)).ToList();
            items.ForEach(_ => _.Quantity = qty);
            var productList = (from lst in items
                               select new Product
                               {
                                   English_Name = lst.English_Name,
                                   Chinese_Name = lst.Chinese_Name,
                                   Dollar_Price = lst.Dollar_Price,
                                   RMB_Price = lst.RMB_Price,
                                   Quantity = lst.Quantity,
                                   Id = lst.Id,
                                   Parent_Id = lst.Parent_Id
                               }).ToList();
            return Json(new { data = productList }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Place order method
        /// </summary>
        /// <param name="fc"></param>
        /// <returns></returns>
        [HttpPost]
        public ActionResult MakeOrder(FormCollection fc)
        {
            ArityEntities objDb = new ArityEntities();
            List<KeyValuePair<int, int>> lineItem = new List<KeyValuePair<int, int>>();
            ViewBag.Products = new SelectList(objDb.Products.Where(_ => _.Parent_Id == 0).ToList(), "Id", "English_Name");
            if (fc != null)
            {
                try
                {
                    for (int i = 1; i <= 999999; i++)
                    {
                        if (fc["productId_" + i] == null && fc["qty_" + i] == null)
                            break;
                        lineItem.Add(new KeyValuePair<int, int>(Convert.ToInt32(fc["productId_" + i]), Convert.ToInt32(fc["qty_" + i])));
                    }
                    if (lineItem.Any())
                    {
                        var order = objDb.Orders.Add(new Order() { CustomerId = 1, CreatedDate = DateTime.Now, Prefix = "user1", Status = 1});
                        objDb.SaveChanges();
                        foreach (var item in lineItem.Where(_ => _.Value > 0))
                        {
                            var prodcut = objDb.Products.Where(_ => _.Id == item.Key).FirstOrDefault();
                            objDb.OrderLineItems.Add(new OrderLineItem() { OrderId = order.Id, ProductId = prodcut.Id, DollarSalesPrice = prodcut.Dollar_Price, RMDPurchasePrice = prodcut.RMB_Price, Quantity = item.Value });
                        }
                        objDb.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error occured while place your order. Please try again.";
                    return View();
                }
            }
            TempData["Success"] = "Order Placed. Thank you";
            return View();
        }

        /// <summary>
        /// Get Product details by Id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public JsonResult GetProductDetail(int id)
        {
            ArityEntities objDb = new ArityEntities();
            var product = (from pro in objDb.Products.ToList()
                           where pro.Id == id
                           select new Product
                           {
                               Dollar_Price = pro.Dollar_Price
                           }).FirstOrDefault();
            return Json(product, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult AddSupplierCartonDetail(int orderId)
        {
            ArityEntities dbContext = new ArityEntities();
            ViewBag.OrderId = orderId;
            ViewBag.OrderName = dbContext.Orders.Where(x => x.Id == orderId).Select(x => x.Prefix).FirstOrDefault();
            ViewBag.OrderDate = dbContext.Orders.Where(x => x.Id == orderId).Select(x => x.CreatedDate).FirstOrDefault();
            var Status = dbContext.Orders.Where(x => x.Id == orderId).Select(x => x.Status).FirstOrDefault();
            ViewBag.Status = ((AritySystems.Common.EnumHelpers.OrderStatus)Status).ToString();
            var ordersLineItems = (from a in dbContext.OrderLineItems
                                   join b in dbContext.Products on a.ProductId equals b.Id
                                   where a.OrderId == orderId
                                   select new { a.Id,b.English_Name}).ToList();
            ViewBag.OrderLineItems = new SelectList(ordersLineItems, "Id", "English_Name");
            return View();
        }


        [HttpPost]
        public ActionResult AddSupplierCartonDetail(FormCollection fc)
        {
            ArityEntities objDb = new ArityEntities();
            List<KeyValuePair<int, int>> lineItem = new List<KeyValuePair<int, int>>();
            ViewBag.OrderId = Convert.ToInt32(fc["OrderId"]);
            if (fc != null)
            {

                SupplierCartoon model = new SupplierCartoon();
                try
                {
                    if (Convert.ToInt32(fc["SupplierOrderMapId"]) != 0)
                    {
                        model.SupplierAssignedMapId = Convert.ToInt32(fc["SupplierOrderMapId"]);
                        model.PcsPerCartoon = Convert.ToInt32(fc["PcsPerCartoon"]);
                        model.CartoonBM = Convert.ToInt32(fc["CartoonBM"]);
                        model.CartoonNumber = fc["CartoonNumber"].ToString();
                        model.CartoonSize = Convert.ToInt32(fc["CartoonSize"]);
                        model.CreatedDate = DateTime.Now;
                        model.NetWeight = Convert.ToInt32(fc["NetWeight"]);
                        model.Status = 1;
                        model.TotalCartoons = Convert.ToInt32(fc["TotalCartoons"]);
                        model.TotalGrossWeight = Convert.ToInt32(fc["TotalGrossWeight"]);
                        model.TotalNetWeight = Convert.ToInt32(fc["TotalNetWeight"]);
                        objDb.SupplierCartoons.Add(model);
                        objDb.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error occured while place your order. Please try again.";
                    return View();
                }
            }
            TempData["Success"] = "Supplier Carton Details added successfully. Thank you";
            return RedirectToAction("AddSupplierCartonDetail","Order",new { orderId = ViewBag.OrderId});
        }

        /// <summary>
        /// Supplier Order List items
        /// </summary>
        /// <param name="OrderId"></param>
        /// <returns></returns>
        [HttpGet]
        public ActionResult SupplierCartonList(int OrderId)
        {
            try
            {
                using (var db = new ArityEntities())
                {
                    var model = (from a in db.SupplierCartoons
                               join b in db.OrderLineItem_Supplier_Mapping on a.SupplierAssignedMapId equals b.OrderLineItemId
                               join c in db.OrderLineItems on b.OrderLineItemId equals c.Id
                               join d in db.Orders on c.OrderId equals d.Id
                                 where d.Id == OrderId
                                 select new SupplierCartonDetailModel
                                 {
                                      Id = a.Id,
                                      CartoonBM = a.CartoonBM,
                                      CartoonNumber = a.CartoonNumber,
                                      CartoonSize = a.CartoonSize,
                                      NetWeight = a.NetWeight,
                                      PcsPerCartoon = a.PcsPerCartoon,
                                      Product_Chinese_Name = c.Product.Chinese_Name,
                                      Product_English_Name = c.Product.English_Name,
                                      Status = 1,
                                      SupplierAssignedMapId = a.SupplierAssignedMapId,
                                      TotalCartoons = a.TotalCartoons,
                                      TotalGrossWeight =a.TotalGrossWeight,
                                      TotalNetWeight = a.TotalNetWeight
                                 }).ToList();

                    return Json(new { data = model }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
    }
}