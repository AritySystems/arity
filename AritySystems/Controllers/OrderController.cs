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
    [Authorize]
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

        public ActionResult Orders()
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
            int loggedInId = (int)Session["UserId"];
            var orderItems = objDb.Orders.OrderByDescending(_ => _.CreatedDate).AsQueryable();
            if (Session["UserType"] != null && ((int)Session["UserType"] == (int)AritySystems.Common.EnumHelpers.UserType.Customer || (int)Session["UserType"] == (int)AritySystems.Common.EnumHelpers.UserType.Supplier))
                orderItems = orderItems.Where(_ => _.CustomerId == loggedInId).AsQueryable();
            var orderLst = (from order in orderItems.ToList()
                            select new
                            {
                                Id = order.Id,
                                Prefix = order.Prefix,
                                InternalStatus = order.Internal_status,
                                CreatedDate = order.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
                                Status = order.Status,
                                TotalItem = order.OrderLineItems.Sum(_ => _.Quantity),
                                DollerSalesTotal = order.OrderLineItems.Sum(_ => (_.DollarSalesPrice * _.Quantity)),
                                DollerPurchaseTotal = order.OrderLineItems.Sum(_ => (_.DollarPurchasePrice * _.Quantity)),
                                RmbSalesTotal = order.OrderLineItems.Sum(_ => (_.RMBSalesPrice * _.Quantity)),
                                RmbPurchaseTotal = order.OrderLineItems.Sum(_ => (_.RMDPurchasePrice * _.Quantity)),
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

        public ActionResult OrderLineItems(int? id)
        {
            var db = new ArityEntities();
            var order = db.Orders.Where(_ => _.Id == (id ?? 0)).FirstOrDefault();
            return View(order);
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
                                 where m.OrderId == OrderId
                                 select new OrderLineItemViewModel
                                 {
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
                                     CreatedDate = m.CreatedDate.ToString(),
                                     //ModifiedDate = m.ModifiedDate ?? DateTime.MinValue,
                                     Suppliers = (from a in db.Users
                                                  join b in db.UserTypes on a.Id equals b.UserId
                                                  where b.Id == 1
                                                  select new SelectListItem
                                                  {
                                                      Text = a.Id.ToString(),
                                                      Value = a.UserName
                                                  }).ToList()
                                 }).ToList();

                    //model.SupplierOrderLineItemList = (from a in db.Supplier_Assigned_OrderLineItem
                    //                                   join b in db.OrderLineItem_Supplier_Mapping on a.OrderSupplierMapId equals b.Id
                    //                                   join c in db.OrderLineItems on b.OrderLineItemId equals c.Id
                    //                                   join d in db.Orders on c.OrderId equals d.Id
                    //                                   where d.Id == OrderId
                    //                                   select new SupplierOrderLineItemModel
                    //                                   {
                    //                                       Id = a.Id,
                    //                                       CreatedDate = a.CreatedDate,
                    //                                       ModifiedDate = a.ModifiedDate,
                    //                                       OrderSupplierMapId = a.OrderSupplierMapId,
                    //                                       Order_Prefix = d.Prefix,
                    //                                       Quantity = a.Quantity,
                    //                                       Status = a.Status,
                    //                                       SupplierId = a.SupplierId,
                    //                                       SupplierName = b.User.UserName
                    //                                   }).ToList();

                    return Json(new { data = model }, JsonRequestBehavior.AllowGet);
                }
                //return View(model);
            }
            catch (Exception ex)
            {
                throw;
            }
        }

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
                                 where d.Id == OrderId
                                 select new SupplierOrderLineItemModel
                                 {
                                     Id = a.Id,
                                     CreatedDate = a.CreatedDate.ToString(),
                                     ModifiedDate = a.ModifiedDate.ToString(),
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
        public JsonResult AddSupplierOrderLineItems(SupplierOrderLineItemModel model)
        {
            Supplier_Assigned_OrderLineItem data = new Supplier_Assigned_OrderLineItem();
            try
            {
                if (model != null)
                {
                    using (var db = new ArityEntities())
                    {
                        data.OrderSupplierMapId = model.OrderSupplierMapId;
                        data.Quantity = model.Quantity;
                        data.Status = 1;
                        data.SupplierId = model.SupplierId;
                        data.CreatedDate = DateTime.UtcNow;
                        data.ModifiedDate = DateTime.UtcNow;
                        db.Supplier_Assigned_OrderLineItem.Add(data);
                        db.SaveChanges();
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
            int loggedInId = (int)Session["UserId"];
            var userDetails = objDb.Users.Where(_ => _.Id == loggedInId).FirstOrDefault();
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
                        var order = objDb.Orders.Add(new Order() { CustomerId = userDetails.Id, CreatedDate = DateTime.Now, Prefix = userDetails.Prefix, Status = (int)AritySystems.Common.EnumHelpers.OrderStatus.Draft, Internal_status = (int)AritySystems.Common.EnumHelpers.OrderStatus.Draft });
                        foreach (var item in lineItem.Where(_ => _.Value > 0))
                        {
                            var prodcut = objDb.Products.Where(_ => _.Id == item.Key).FirstOrDefault();
                            objDb.OrderLineItems.Add(new OrderLineItem()
                            {
                                OrderId = order.Id,
                                ProductId = prodcut.Id,
                                DollarSalesPrice = prodcut.Dollar_Price,
                                DollarPurchasePrice = prodcut.Dollar_Price,
                                RMBSalesPrice = prodcut.RMB_Price,
                                RMDPurchasePrice = prodcut.RMB_Price,
                                Quantity = item.Value,
                                CreatedDate = DateTime.Now
                            });
                        }
                        objDb.SaveChanges();
                    }
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error occured while placing your order. Please try again.";
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

        /// <summary>
        /// Update Purchase price
        /// </summary>
        /// <param name="purchaseMdel"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult PurchasePriceUpdate(List<PriceUpdateModel> purchaseMdel, string type)
        {
            try
            {
                var objDb = new ArityEntities();
                if (purchaseMdel != null && purchaseMdel.Any())
                {
                    foreach (var item in purchaseMdel)
                    {
                        var orderLineItem = objDb.OrderLineItems.Where(_ => _.Id == item.ItemId).FirstOrDefault();
                        if (type != null && !string.IsNullOrEmpty(type) && type.ToLower().Equals("purchase"))
                        {
                            orderLineItem.DollarPurchasePrice = item.DollerPrice;
                            orderLineItem.RMDPurchasePrice = item.RMBPrice;
                        }
                        else
                        {
                            orderLineItem.DollarSalesPrice = item.DollerPrice;
                            orderLineItem.RMBSalesPrice = item.RMBPrice;
                        }
                    }
                    objDb.SaveChanges();
                }
                return Json("", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Common method for updateing internal status
        /// </summary>
        /// <param name="id">order id</param>
        /// <param name="status">internal status id</param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult OrderInternamStatuseChange(int id, int status)
        {
            try
            {
                ArityEntities objDb = new ArityEntities();
                var order = objDb.Orders.Where(_ => _.Id == id).FirstOrDefault();
                order.Internal_status = status;
                objDb.SaveChanges();
                return Json("", JsonRequestBehavior.AllowGet);
            }
            catch
            {
                return Json(null, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Landing page for supplier order
        /// </summary>
        /// <returns></returns>
        public ActionResult SuppliersOrder()
        {
            return View();
        }

        /// <summary>
        /// Get supplier order listing
        /// </summary>
        /// <returns></returns>
        public JsonResult GetSupplierOrderList()
        {
            var loggedInId = (int)Session["UserId"];
            var objDb = new ArityEntities();
            var orders = (from order in objDb.Orders.ToList()
                          join orderlineitem in objDb.OrderLineItems.ToList() on order.Id equals orderlineitem.OrderId
                          join supplierorder in objDb.OrderLineItem_Supplier_Mapping.ToList() on orderlineitem.Id equals supplierorder.OrderLineItemId
                          join supplieritem in objDb.Supplier_Assigned_OrderLineItem.ToList() on supplierorder.Id equals supplieritem.OrderSupplierMapId
                          select new
                          {
                              SupplierOrderId = supplierorder.Id,
                              Prefix = "",
                              OrderId = order.Id,
                              CreatedOn = supplierorder.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
                              Quantity = supplierorder.Quantity,
                              DollerSalesTotal = 0,
                              RmbSalesTotal = 0,
                              Status = 1
                          }).ToList();
            return Json(new { data = orders }, JsonRequestBehavior.AllowGet);
        }
    }
}