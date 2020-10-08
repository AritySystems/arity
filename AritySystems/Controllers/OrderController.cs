using AritySystems.Data;
using AritySystems.Models;
using Syncfusion.XlsIO;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AritySystems.Controllers
{
    [AllowAnonymous]
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
            if (Request.Cookies["UserId"] == null)
                return Json(new { data = "" }, JsonRequestBehavior.AllowGet);
            int loggedInId = Convert.ToInt32(Request.Cookies["UserId"].Value);
            int type = Convert.ToInt32(Request.Cookies["UserType"].Value);

            var orderItems = objDb.Orders.AsQueryable();
            if (type == (int)AritySystems.Common.EnumHelpers.UserType.Customer || type == (int)AritySystems.Common.EnumHelpers.UserType.Supplier)
                orderItems = orderItems.Where(_ => _.CustomerId == loggedInId).AsQueryable();
            var orderLst = (from order in orderItems.ToList()
                            select new
                            {
                                Id = order.Id,
                                Prefix = order.Prefix,
                                InternalStatus = order.Internal_status,
                                CreatedDate = order.CreatedDate.ToString("dd/MM/yyyy"),
                                Status = order.Status,
                                TotalItem = order.OrderLineItems.Sum(_ => _.Quantity),
                                DollerSalesTotal = order.OrderLineItems.Sum(_ => (_.DollarSalesPrice * _.Quantity)),
                                DollerPurchaseTotal = order.OrderLineItems.Sum(_ => (_.DollarPurchasePrice * _.Quantity)),
                                RmbSalesTotal = order.OrderLineItems.Sum(_ => (_.RMBSalesPrice * _.Quantity)),
                                RmbPurchaseTotal = order.OrderLineItems.Sum(_ => (_.RMBPurchasePrice * _.Quantity)),
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
            var orders = objDb.Orders.Where(_ => _.Id == id).FirstOrDefault();
            ViewBag.Sales_Person_Id = orders != null && (orders.Sales_Person_Id ?? 0) > 0 ? new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Sales), "Id", "FullName", orders.Sales_Person_Id) : new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Sales), "Id", "FullName");
            ViewBag.ExporterList = orders != null && (orders.ExporterId ?? 0) > 0 ? new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Exporter), "Id", "CompanyName", orders.ExporterId) : new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Exporter), "Id", "CompanyName");
            return View(orders);
        }

        private List<UserModel> GetUerList(int Usertype)
        {
            ArityEntities objDb = new ArityEntities();
            return objDb.Users.Where(_ => (_.UserType ?? 0) == Usertype).Select(_ => new UserModel
            {
                Id = _.Id,
                FullName = _.FirstName + " " + _.LastName,
                CompanyName = _.CompanyName
            }).ToList();
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
                                 join o in db.Orders on m.OrderId equals o.Id
                                 join n in db.Products on m.ProductId equals n.Id
                                 where m.OrderId == OrderId && m.Quantity > 0
                                 select new
                                 {
                                     OrderLineItem = m,
                                     Orders = o,
                                     Products = n
                                 }
                                ).ToList();

                    var newModel = model.Select(_ => new OrderLineItemViewModel
                    {
                        Id = _.OrderLineItem.Id,
                        //OrderId = m.OrderId ?? 0,
                        Order_Name = _.Orders.Prefix,
                        ProductId = _.OrderLineItem.ProductId ?? 0,
                        Product_Name = _.Products.English_Name + "(" + _.Products.Chinese_Name + ")",
                        Purchase_Price_dollar = _.OrderLineItem.DollarPurchasePrice,
                        Sales_Price_dollar = _.OrderLineItem.DollarSalesPrice,
                        Purchase_Price_rmb = _.OrderLineItem.RMBPurchasePrice,
                        Sales_Price_rmb = _.OrderLineItem.RMBSalesPrice,
                        quantity = _.OrderLineItem.Quantity,
                        UpdatedQuantity = UpdatedQuantity(_.OrderLineItem.Id),
                        //CreatedDate = m.CreatedDate.ToString(),
                        //ModifiedDate = m.ModifiedDate ?? DateTime.MinValue,
                        Suppliers = GetProductSuppliers(_.OrderLineItem.ProductId ?? 0).ToList()
                    }).ToList();
                    return Json(new { data = newModel }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }

        public decimal UpdatedQuantity(int orderLineItemId)
        {
            ArityEntities dbContext = new ArityEntities();
            decimal newUpdated = 0;
            var updated = (from b in dbContext.Supplier_Assigned_OrderLineItem
                           where b.OrderLineItem == orderLineItemId
                           select b.Quantity).ToList().Sum();
            var actual = (from a in dbContext.OrderLineItems
                          where a.Id == orderLineItemId
                          select a.Quantity).FirstOrDefault();
            if (actual >= updated)
                newUpdated = actual - updated;
            else
                newUpdated = 0;
            return newUpdated;
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
                    var model = (from supplierOrder in db.Supplier_Assigned_OrderLineItem.ToList()
                                 join orderLineItem in db.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                                 join order in db.Orders on orderLineItem.OrderId equals order.Id
                                 where order.Id == OrderId && supplierOrder.Quantity > 0
                                 select new SupplierOrderLineItemModel
                                 {
                                     Order_Prefix = order.Prefix,
                                     Quantity = supplierOrder.Quantity,
                                     OrderId = order.Id,
                                     Status = order.Status,
                                     RMBSalesPrice = orderLineItem.RMBSalesPrice * supplierOrder.Quantity,
                                     SupplierId = supplierOrder.SupplierId,
                                     SupplierName = supplierOrder.User.UserName,
                                     ExpectedDeliveryDate = supplierOrder.ExpectedTimeDelivery != null ? supplierOrder.ExpectedTimeDelivery.Value.ToString("MM/dd/yyyy") : ""
                                 }).ToList();

                    var supplierOrders = model.GroupBy(x => new { supplier = x.SupplierId, order = x.OrderId }).Select(x => new
                    {
                        Order_Prefix = x.Select(y => y.Order_Prefix).FirstOrDefault(),
                        Quantity = x.Sum(y => y.Quantity),
                        OrderId = x.Select(y => y.OrderId).FirstOrDefault(),
                        Status = x.Select(y => y.Status).FirstOrDefault(),
                        SupplierId = x.Select(y => y.SupplierId).FirstOrDefault(),
                        SupplierName = x.Select(y => y.SupplierName).FirstOrDefault(),
                        TotalRMBSalesPrice = x.Sum(y => y.RMBSalesPrice),
                        ExpectedDeliveryDate = x.Select(y => y.ExpectedDeliveryDate).FirstOrDefault()
                    }).ToList();

                    return Json(new { data = supplierOrders }, JsonRequestBehavior.AllowGet);
                }
            }
            catch (Exception)
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
        public ActionResult AddSupplierOrderLineItems(List<SupplierOrderItemAdd> addData)
        {
            Supplier_Assigned_OrderLineItem model = new Supplier_Assigned_OrderLineItem();
            try
            {
                if (addData != null)
                {
                    foreach (var item in addData)
                    {
                        using (var db = new ArityEntities())
                        {
                            Decimal newQuantity = 0;
                            int id = 0;
                            var orderId = Convert.ToInt32(item.OrderId);
                            var orderLineItemId = Convert.ToInt32(item.OrderLineItemId);
                            var quantity = Convert.ToDecimal(item.NewQuantity);
                            var oldQuantity = Convert.ToDecimal(item.OldQuantity);
                            var supplierid = Convert.ToInt32(item.SupplierId);
                            if (oldQuantity >= quantity)
                            {
                                var updated = UpdatedQuantity(orderLineItemId);
                                newQuantity = updated - quantity;
                            }
                            if (newQuantity >= 0)
                            {
                                ViewBag.updatedQuantity = newQuantity;
                            }

                            //OrderLineItem_Supplier_Mapping dataModeldata = new OrderLineItem_Supplier_Mapping();
                            //dataModeldata = db.OrderLineItem_Supplier_Mapping.Where(x => x.OrderId == orderId && x.SupplierId == supplierid).FirstOrDefault();
                            //OrderLineItem_Supplier_Mapping dataModel = new OrderLineItem_Supplier_Mapping();
                            //if (dataModeldata == null)
                            //{
                            //    dataModel.CreatedDate = DateTime.Now;
                            //    dataModel.ModifiedDate = DateTime.Now;
                            //    dataModel.OrderId = Convert.ToInt32(item.OrderId);
                            //    dataModel.Quantity = db.OrderLineItems.Where(x => x.OrderId == orderId).Select(x => x.Quantity).Sum();
                            //    dataModel.SupplierId = Convert.ToInt32(item.SupplierId);
                            //    id = dataModel.Id;
                            //    db.OrderLineItem_Supplier_Mapping.Add(dataModel);
                            //}
                            //else
                            //{
                            //    dataModeldata.ModifiedDate = DateTime.Now;
                            //    id = dataModeldata.Id;
                            //    dataModeldata.Quantity = db.OrderLineItems.Where(x => x.OrderId == orderId).Select(x => x.Quantity).Sum();
                            //}
                            //db.SaveChanges();

                            //var id = dataModeldata.Id;
                            if (quantity > 0)
                            {
                                model.OrderSupplierMapId = null;
                                model.OrderLineItem = Convert.ToInt32(item.OrderLineItemId);
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
                }
                return Json(new { data = addData }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception)
            {
                throw;
            }
        }

        /// <summary>
        /// Add supplier carton details
        /// </summary>
        /// <param name="supplierCarton"></param>
        /// <returns></returns>
        public JsonResult AddSupplierCartonDetails(SupplierCartoon supplierCartoon)
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

        [HttpPost]
        public ActionResult AddSupplierCartonDetail(SupplierCartoonModel data)
        {
            ArityEntities objDb = new ArityEntities();
            List<KeyValuePair<int, int>> lineItem = new List<KeyValuePair<int, int>>();
            //ViewBag.OrderId = Convert.ToInt32(data.OrderId);
            // if (data != null)
            {

                SupplierCartoon model = new SupplierCartoon();
                try
                {
                    if (Convert.ToInt32(data.SupplierOrderMapId) != 0 && data.TotalCartoons > 0)
                    {
                        int maxOrderId = objDb.SupplierCartoons.Where(x => x.OrderId == data.OrderId).Select(x => x.CartoonMax).Max() ?? 0;

                        model.SupplierAssignedMapId = Convert.ToInt32(data.SupplierOrderMapId);
                        model.PcsPerCartoon = Convert.ToDecimal(data.PcsPerCartoon);
                        model.CreatedDate = DateTime.Now;
                        model.ModifiedDate = DateTime.Now;
                        model.GrossWeight = Convert.ToDecimal(data.GrossWeight);
                        model.NetWeight = Convert.ToDecimal(data.NetWeight);
                        model.Status = 1;
                        model.TotalCartoons = Convert.ToInt32(data.TotalCartoons);
                        model.CartoonBreadth = data.CartoonBreadth;
                        model.CartoonLength = data.CartoonLength;
                        model.CartoonHeight = data.CartoonHeight;
                        model.CartoonMax = maxOrderId + data.TotalCartoons;
                        model.CartoonPrefix = data.CartoonPrefix;
                        model.TotalGrossWeight = model.TotalCartoons * model.GrossWeight;
                        model.TotalNetWeight = model.TotalCartoons * model.NetWeight;
                        model.CartoonSize = model.CartoonLength * model.CartoonHeight * model.CartoonBreadth;
                        model.CartoonNumber = model.CartoonPrefix + (maxOrderId + 1) + "-" + model.CartoonPrefix + model.CartoonMax + "";
                        model.OrderId = data.OrderId;
                        objDb.SupplierCartoons.Add(model);
                        objDb.SaveChanges();

                        if (model.Id > 0)
                        {
                            string cartoonNumber = model.CartoonPrefix + "_" + model.OrderId + "_" + DateTime.Now.Date.Year + "_" + (maxOrderId + 1);
                            for (int i = 0; i < model.TotalCartoons; i++)
                            {

                                SupplierCartoonDetail cartoondetail = new SupplierCartoonDetail()
                                {
                                    SupplierCartoonId = model.Id,
                                    CartoonNumber = cartoonNumber,
                                    CartoonStatus = 1,
                                    CreatedDate = DateTime.Now,
                                    ModifiedDate = DateTime.Now
                                };
                                maxOrderId += 1;
                                objDb.SupplierCartoonDetails.Add(cartoondetail);

                            }
                            objDb.SaveChanges();
                        }

                    }

                }
                catch (Exception ex)
                {
                    TempData["Error"] = "Error occured while place your order. Please try again.";
                    return View();
                }
            }
            //TempData["Success"] = "Supplier Carton Details added successfully. Thank you";
            return RedirectToAction("AddSupplierCartonDetail", "Order", new { orderId = data.OrderId });
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
                           select pro).FirstOrDefault();
            var items = new List<Product>();
            items.Add(product);
            items.AddRange(GetChildProducts(id));
            items.ForEach(_ => _.MOQ = qty);
            items.Where(_ => _.ParentIds != null && !string.IsNullOrEmpty(_.ParentIds)).ToList().ForEach(_ => { _.MOQ = (_.BOM != null && _.BOM > 0 ? _.MOQ * (_.BOM ?? 0) : _.MOQ); });
            var productList = (from lst in items
                               select new Product
                               {
                                   English_Name = lst.English_Name,
                                   Chinese_Name = lst.Chinese_Name,
                                   Dollar_Price = lst.Dollar_Price,
                                   RMB_Price = lst.RMB_Price,
                                   Quantity = lst.Quantity,
                                   MOQ = lst.MOQ,
                                   Id = lst.Id,
                                   ParentIds = lst.ParentIds,
                                   BOM = lst.BOM,
                                   CBM = lst.CBM,
                                   Weight = lst.Weight
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
            if (Request.Cookies["UserId"] == null)
            {
                Response.Redirect("/user/login");
            }
            int loggedInId = Convert.ToInt32(Request.Cookies["UserId"].Value);
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
                        var order = objDb.Orders.Add(new Order() { CustomerId = userDetails.Id, CreatedDate = DateTime.Now, Prefix = userDetails.Prefix, Status = (int)AritySystems.Common.EnumHelpers.OrderStatus.Draft, Internal_status = (int)AritySystems.Common.EnumHelpers.OrderStatus.Draft, TermsandCondition = "Above prices are based on X - Guzhen and in USD ,Payment terms :  50 % Advance balance Before Delivery ,Delivery period : 15 days after received Advance ,Bank Details for remittance: 	" });
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
                                RMBPurchasePrice = prodcut.RMB_Price,
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
                            orderLineItem.RMBPurchasePrice = item.RMBPrice;
                        }
                        else
                        {
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
        public ActionResult GeneratePL(int id)
        {
            ArityEntities dbContext = new ArityEntities();
            var perfoma = (from order in dbContext.Orders
                           join user in dbContext.Users on order.CustomerId equals user.Id
                           join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                           join product in dbContext.Products on lineItem.ProductId equals product.Id
                           join exporterDetail in dbContext.Users on order.ExporterId equals exporterDetail.Id
                           where order.Id == id && exporterDetail.UserType == 6
                           select new PerformaInvoice()
                           {
                               ExporterName = exporterDetail.CompanyName,
                               ExporterAddress = exporterDetail.Address,
                               ExporterPhone = exporterDetail.PhoneNumber,
                               CustomerCompanyName = user.CompanyName,
                               CustomerAddress = user.Address,
                               CustomerGST = user.GSTIN,
                               OrderDate = order.CreatedDate,
                               IECCode = user.IECCode,
                               CustomerName = user.FirstName + " " + user.LastName,
                               CustomerPhone = user.PhoneNumber
                           }).FirstOrDefault();

            DateTime? lastCommercialDt = dbContext.CommercialInvoices.Where(x => x.OrderId == id).OrderByDescending(x => x.ModifiedDate).FirstOrDefault()?.ModifiedDate;
            lastCommercialDt = lastCommercialDt ?? DateTime.MinValue;
            var cartoonList = (from cartoon in dbContext.SupplierCartoons
                               join supplierAsigned in dbContext.Supplier_Assigned_OrderLineItem on cartoon.SupplierAssignedMapId equals supplierAsigned.Id
                               join lineItem in dbContext.OrderLineItems on supplierAsigned.OrderLineItem equals lineItem.Id
                               join product in dbContext.Products on lineItem.ProductId equals product.Id
                               where cartoon.OrderId == id && cartoon.Status == 2 && cartoon.ModifiedDate > lastCommercialDt
                               select new
                               {
                                   c = cartoon,
                                   p = product
                               }).AsEnumerable().Select(x => new CartoonDetails()
                               {
                                   Partiular = x.p.English_Name,
                                   Quantity_PCS = x.c.PcsPerCartoon * x.c.TotalCartoons,
                                   PCSPERCartoon = x.c.PcsPerCartoon,
                                   TotalCartoon = x.c.TotalCartoons,
                                   NetWeight = x.c.NetWeight,
                                   TotalNetWeight = x.c.TotalNetWeight,
                                   GrossWeight = x.c.GrossWeight,
                                   TotalGrossWeight = x.c.TotalGrossWeight,
                                   CartoonLength = x.c.CartoonLength,
                                   CartoonBreadth = x.c.CartoonBreadth,
                                   CartoonHeight = x.c.CartoonHeight,
                                   CartoonCBM = x.c.CartoonSize ?? 0,
                                   CartoonNumber = x.c.CartoonNumber
                               }).ToList();

            perfoma.CartoonList = cartoonList ?? new List<CartoonDetails>();
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                //Set the default application version as Excel 2016.
                excelEngine.Excel.DefaultVersion = ExcelVersion.Excel2016;

                //Create a workbook with a worksheet.
                IWorkbook workbook = excelEngine.Excel.Workbooks.Create(1);

                //Access first worksheet from the workbook instance.
                IWorksheet worksheet = workbook.Worksheets[0];

                //Insert sample text into cell “A1”.
                worksheet.Range["A1"].Text = perfoma.ExporterName;
                worksheet.Range["$A$1:$N$1"].Merge();

                IStyle headingStyle = workbook.Styles.Add("HeadingStyle");
                headingStyle.Font.Bold = true;
                headingStyle.Font.Size = 20;
                headingStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$1:$N$1"].CellStyle = headingStyle;

                worksheet.Range["A2"].Text = perfoma.ExporterAddress;
                worksheet.Range["$A$2:$N$2"].Merge();

                IStyle exporterAdress = workbook.Styles.Add("exporterAdress");
                exporterAdress.Font.Size = 15;
                exporterAdress.Font.Bold = true;
                exporterAdress.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$2:$N$2"].CellStyle = exporterAdress;

                worksheet.Range["A3"].Text = perfoma.ExporterPhone;
                worksheet.Range["$A$3:$N$3"].Merge();

                worksheet.Range["$A$3:$N$3"].CellStyle = exporterAdress;

                worksheet.Range["A4"].Text = string.Empty;
                worksheet.Range["$A$4:$N$4"].Merge();

                worksheet.Range["A5"].Text = "Packing List";
                worksheet.Range["$A$5:$N$5"].Merge();

                IStyle CustomTextStyle = workbook.Styles.Add("CustomTextStyle");
                CustomTextStyle.Font.Size = 25;
                CustomTextStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                CustomTextStyle.Font.Bold = true;
                worksheet.Range["$A$5:$F$5"].CellStyle = CustomTextStyle;

                worksheet.Range["A6"].Text = perfoma.CustomerCompanyName;
                worksheet.Range["$A$6:$B$6"].Merge();

                IStyle CustomTextCustomerStyle = workbook.Styles.Add("CustomTextCustomerStyle");
                CustomTextCustomerStyle.Font.Size = 15;
                CustomTextCustomerStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                worksheet.Range["$A$6:$B$6"].CellStyle = CustomTextCustomerStyle;

                worksheet.Range["J6"].Text = "Inv No.";
                worksheet.Range["$J$6:$K$6"].Merge();

                worksheet.Range["L6"].Text = perfoma.PINo;
                worksheet.Range["$L$6:$N$6"].Merge();

                worksheet.Range["A7"].Text = perfoma.CustomerAddress;
                worksheet.Range["$A$7:$B$7"].Merge();

                worksheet.Range["J7"].Text = "Date";
                worksheet.Range["$J$7:$K$7"].Merge();

                worksheet.Range["L7"].Text = perfoma.OrderDate.ToString("dd/MM/yyyy");
                worksheet.Range["$L$7:$N$7"].Merge();

                worksheet.Range["A8"].Text = string.Empty;
                worksheet.Range["$A$8:$B$8"].Merge();

                worksheet.Range["J8"].Text = "";
                worksheet.Range["$J$8:$K$8"].Merge();

                worksheet.Range["L8"].Text = perfoma.IECCode;
                worksheet.Range["$L$8:$N$8"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["J9"].Text = "Name";
                worksheet.Range["$J$9:$K$9"].Merge();

                worksheet.Range["L9"].Text = perfoma.CustomerName;
                worksheet.Range["$L$9:$N$9"].Merge();

                worksheet.Range["A10"].Text = string.Empty;
                worksheet.Range["$A$10:$B$10"].Merge();

                worksheet.Range["J10"].Text = "Contact No.";
                worksheet.Range["$J$10:$K$10"].Merge();

                worksheet.Range["L10"].Text = perfoma.CustomerPhone;
                worksheet.Range["$L$10:$N$10"].Merge();

                IStyle headingLineItemStyle = workbook.Styles.Add("headingLineItemStyle");
                headingLineItemStyle.Font.Size = 15;
                headingLineItemStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

                headingLineItemStyle.Font.Bold = true;

                worksheet.AutofitRow(11);

                worksheet.AutofitColumn(1);
                worksheet.AutofitColumn(2);
                worksheet.AutofitColumn(3);
                worksheet.AutofitColumn(4);
                worksheet.AutofitColumn(5);
                worksheet.AutofitColumn(6);
                worksheet.AutofitColumn(7);
                worksheet.AutofitColumn(8);
                worksheet.AutofitColumn(9);
                worksheet.AutofitColumn(10);
                worksheet.AutofitColumn(11);
                worksheet.AutofitColumn(12);
                worksheet.AutofitColumn(13);
                worksheet.AutofitColumn(14);

                worksheet.Range["A11"].Text = "Sr.No.";
                worksheet.Range["B11"].Text = "Perticulates";
                worksheet.Range["C11"].Text = "QTY/PCS";
                worksheet.Range["D11"].Text = "PCS/CTN";
                worksheet.Range["E11"].Text = "CTNS";
                worksheet.Range["F11"].Text = "N.W.(kg)";
                worksheet.Range["G11"].Text = "T.N.W";
                worksheet.Range["H11"].Text = "G.W.(kg)";
                worksheet.Range["I11"].Text = "T.G.W";
                worksheet.Range["J11"].Text = "CARTON SIZE（CM)";
                worksheet.Range["$J$11:$L$11"].Merge();
                worksheet.Range["M11"].Text = " CBM(M3)";
                worksheet.Range["N11"].Text = "CTN/NO.";

                worksheet.Range["A11"].CellStyle = headingLineItemStyle;
                worksheet.Range["B11"].CellStyle = headingLineItemStyle;
                worksheet.Range["C11"].CellStyle = headingLineItemStyle;
                worksheet.Range["D11"].CellStyle = headingLineItemStyle;
                worksheet.Range["E11"].CellStyle = headingLineItemStyle;
                worksheet.Range["F11"].CellStyle = headingLineItemStyle;
                worksheet.Range["G11"].CellStyle = headingLineItemStyle;
                worksheet.Range["H11"].CellStyle = headingLineItemStyle;
                worksheet.Range["I11"].CellStyle = headingLineItemStyle;
                worksheet.Range["J11"].CellStyle = headingLineItemStyle;
                worksheet.Range["M11"].CellStyle = headingLineItemStyle;
                worksheet.Range["N11"].CellStyle = headingLineItemStyle;


                int i = 1;
                int rownum = 12;
                foreach (var item in perfoma.CartoonList)
                {
                    worksheet.Range["A" + rownum + ""].Text = i.ToString();
                    worksheet.Range["B" + rownum + ""].Text = item.Partiular;
                    worksheet.Range["C" + rownum + ""].Text = string.Format("{0:0}", item.Quantity_PCS);
                    worksheet.Range["D" + rownum + ""].Text = string.Format("{0:0}", item.PCSPERCartoon);
                    worksheet.Range["E" + rownum + ""].Text = item.TotalCartoon.ToString();
                    worksheet.Range["F" + rownum + ""].Text = string.Format("{0:0.0000}", item.NetWeight);
                    worksheet.Range["G" + rownum + ""].Text = string.Format("{0:0.00}", item.TotalNetWeight);
                    worksheet.Range["H" + rownum + ""].Text = string.Format("{0:0.0000}", item.GrossWeight);

                    worksheet.Range["I" + rownum + ""].Text = string.Format("{0:0.00}", item.TotalGrossWeight);
                    worksheet.Range["J" + rownum + ""].Text = string.Format("{0:0.0000}", item.CartoonLength);
                    worksheet.Range["K" + rownum + ""].Text = string.Format("{0:0.0000}", item.CartoonBreadth);
                    worksheet.Range["L" + rownum + ""].Text = string.Format("{0:0.0000}", item.CartoonHeight);
                    worksheet.Range["M" + rownum + ""].Text = string.Format("{0:0.0000}", item.CartoonCBM);
                    worksheet.Range["N" + rownum + ""].Text = item.CartoonNumber;
                    i++;
                    rownum++;
                }

                decimal totalCartoons = 0;
                decimal totalGrossWeight = 0;
                decimal totalNetWeight = 0;
                decimal totalCBM = 0;

                foreach (var item in cartoonList)
                {
                    totalCartoons += item.TotalCartoon;
                    totalGrossWeight += item.TotalGrossWeight;
                    totalNetWeight += item.TotalNetWeight;
                    totalCBM += item.CartoonCBM;
                }

                rownum += 10;

                worksheet.Range["A" + rownum + ""].Text = "Total";

                worksheet.Range["E" + rownum + ""].Text = string.Format("${0:0.00}", totalCartoons);
                worksheet.Range["G" + rownum + ""].Text = string.Format("¥{0:0.00}", totalNetWeight);
                worksheet.Range["I" + rownum + ""].Text = string.Format("${0:0.00}", totalGrossWeight);
                worksheet.Range["M" + rownum + ""].Text = string.Format("¥{0:0.00}", totalCBM);

                //Save the workbook to disk in xlsx format.
                workbook.SaveAs(@"/Content/PackingList/" + "PL" + ".xlsx", HttpContext.ApplicationInstance.Response, ExcelDownloadType.Open);
            }
            return View();
        }

        public ActionResult GeneratePerfomaInvoice(int? id)
        {
            ArityEntities dbContext = new ArityEntities();
            int asIntegers = 0;
            List<string> PerfomaList = dbContext.PerfomaInvoices.Where(x => x.OrderId == id).Select(x => x.PerfomaInvoiceReferece).ToList();
            if (PerfomaList != null && PerfomaList.Count > 0)
            {
                List<string> maxPerfoma = PerfomaList.Select(x => x.Replace("PI", "")).ToList();
                asIntegers = maxPerfoma.Select(s => int.Parse(s)).ToArray().Max();
            }
            var perfoma = (from order in dbContext.Orders
                           join user in dbContext.Users on order.CustomerId equals user.Id
                           join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                           join product in dbContext.Products on lineItem.ProductId equals product.Id
                           join exporterDetail in dbContext.Users on order.ExporterId equals exporterDetail.Id
                           where order.Id == id && exporterDetail.UserType == 6
                           select new PerformaInvoice()
                           {
                               ExporterName = exporterDetail.CompanyName,
                               ExporterAddress = exporterDetail.Address,
                               ExporterPhone = exporterDetail.PhoneNumber,
                               CustomerCompanyName = user.CompanyName,
                               CustomerAddress = user.Address,
                               CustomerGST = user.GSTIN,
                               PINo = "PI" + (asIntegers + 1),
                               OrderDate = order.CreatedDate,
                               IECCode = user.IECCode,
                               CustomerName = user.FirstName + " " + user.LastName,
                               CustomerPhone = user.PhoneNumber
                           }).FirstOrDefault();

            var productList = (from order in dbContext.Orders
                               join user in dbContext.Users on order.CustomerId equals user.Id
                               join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                               join product in dbContext.Products on lineItem.ProductId equals product.Id
                               where order.Id == id
                               select new
                               {
                                   l = lineItem,
                                   p = product
                               }).AsEnumerable().Select(x => new PerfomaProductList()
                               {
                                   Partiular = x.p.English_Name,
                                   Quantity = x.l.Quantity,
                                   Unit = getEnumValue(Convert.ToInt32(x.p.Unit)),
                                   UnitPrice = x.l.DollarSalesPrice,
                                   TotalUSD = x.l.Quantity * x.l.DollarSalesPrice,
                                   ProductId = x.p.Id,
                                   RMBUnitPrice = x.l.RMBSalesPrice,
                                   TotalRMB = x.l.Quantity * x.l.RMBSalesPrice

                               }).ToList();

            perfoma.ProductList = productList ?? new List<PerfomaProductList>();


            //Create an instance of ExcelEngine.
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                //Set the default application version as Excel 2016.
                excelEngine.Excel.DefaultVersion = ExcelVersion.Excel2016;

                //Create a workbook with a worksheet.
                IWorkbook workbook = excelEngine.Excel.Workbooks.Create(1);

                //Access first worksheet from the workbook instance.
                IWorksheet worksheet = workbook.Worksheets[0];

                //Insert sample text into cell “A1”.
                worksheet.Range["A1"].Text = perfoma.ExporterName;
                worksheet.Range["$A$1:$F$1"].Merge();

                IStyle headingStyle = workbook.Styles.Add("HeadingStyle");
                headingStyle.Font.Bold = true;
                headingStyle.Font.Size = 20;
                headingStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$1:$F$1"].CellStyle = headingStyle;

                worksheet.Range["A2"].Text = perfoma.ExporterAddress;
                worksheet.Range["$A$2:$F$2"].Merge();

                IStyle exporterAdress = workbook.Styles.Add("exporterAdress");
                exporterAdress.Font.Size = 15;
                exporterAdress.Font.Bold = true;
                exporterAdress.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$2:$F$2"].CellStyle = exporterAdress;

                worksheet.Range["A3"].Text = perfoma.ExporterPhone;
                worksheet.Range["$A$3:$F$3"].Merge();

                worksheet.Range["$A$3:$F$3"].CellStyle = exporterAdress;

                worksheet.Range["A4"].Text = string.Empty;
                worksheet.Range["$A$4:$F$4"].Merge();

                worksheet.Range["A5"].Text = "PERFORMA INVOICE";
                worksheet.Range["$A$5:$F$5"].Merge();

                IStyle CustomTextStyle = workbook.Styles.Add("CustomTextStyle");
                CustomTextStyle.Font.Size = 25;
                CustomTextStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                CustomTextStyle.Font.Bold = true;
                worksheet.Range["$A$5:$F$5"].CellStyle = CustomTextStyle;

                worksheet.Range["A6"].Text = perfoma.CustomerCompanyName;
                worksheet.Range["$A$6:$B$6"].Merge();

                IStyle CustomTextCustomerStyle = workbook.Styles.Add("CustomTextCustomerStyle");
                CustomTextCustomerStyle.Font.Size = 15;
                CustomTextCustomerStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                worksheet.Range["$A$6:$B$6"].CellStyle = CustomTextCustomerStyle;

                worksheet.Range["C6"].Text = "Pi No.";

                worksheet.Range["D6"].Text = perfoma.PINo;
                worksheet.Range["$D$6:$F$6"].Merge();

                worksheet.Range["A7"].Text = perfoma.CustomerAddress;
                worksheet.Range["$A$7:$B$7"].Merge();

                worksheet.Range["C7"].Text = "Date";

                worksheet.Range["D7"].Text = perfoma.OrderDate.ToString("dd/MM/yyyy");
                worksheet.Range["$D$7:$F$7"].Merge();

                worksheet.Range["A8"].Text = perfoma.CustomerGST;
                worksheet.Range["$A$8:$B$8"].Merge();

                worksheet.Range["C8"].Text = "IEC code";

                worksheet.Range["D8"].Text = perfoma.IECCode;
                worksheet.Range["$D$8:$F$8"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["C9"].Text = "Name";

                worksheet.Range["D9"].Text = perfoma.CustomerName;
                worksheet.Range["$D$9:$F$9"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["C10"].Text = "Contact No.";

                worksheet.Range["D10"].Text = perfoma.CustomerPhone;
                worksheet.Range["$D$9:$F$9"].Merge();

                IStyle headingLineItemStyle = workbook.Styles.Add("headingLineItemStyle");
                headingLineItemStyle.Font.Size = 15;
                headingLineItemStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

                headingLineItemStyle.Font.Bold = true;

                worksheet.AutofitRow(11);

                worksheet.AutofitColumn(1);
                worksheet.AutofitColumn(2);
                worksheet.AutofitColumn(3);
                worksheet.AutofitColumn(4);
                worksheet.AutofitColumn(5);
                worksheet.AutofitColumn(6);
                worksheet.AutofitColumn(7);
                worksheet.AutofitColumn(8);

                worksheet.Range["A11"].Text = "Sr.No.";
                worksheet.Range["B11"].Text = "Perticulates";
                worksheet.Range["C11"].Text = "UP USD";
                worksheet.Range["D11"].Text = "Unit";
                worksheet.Range["E11"].Text = "Qty";
                worksheet.Range["F11"].Text = "Total USD";
                worksheet.Range["G11"].Text = "Total RMB";
                worksheet.Range["H11"].Text = "RMB";


                worksheet.Range["A11"].CellStyle = headingLineItemStyle;
                worksheet.Range["B11"].CellStyle = headingLineItemStyle;
                worksheet.Range["C11"].CellStyle = headingLineItemStyle;
                worksheet.Range["D11"].CellStyle = headingLineItemStyle;
                worksheet.Range["E11"].CellStyle = headingLineItemStyle;
                worksheet.Range["F11"].CellStyle = headingLineItemStyle;
                worksheet.Range["G11"].CellStyle = headingLineItemStyle;
                worksheet.Range["H11"].CellStyle = headingLineItemStyle;


                int i = 1;
                int rownum = 12;
                foreach (var item in perfoma.ProductList)
                {
                    worksheet.Range["A" + rownum + ""].Text = i.ToString();
                    worksheet.Range["B" + rownum + ""].Text = item.Partiular;
                    worksheet.Range["C" + rownum + ""].Text = string.Format("{0:0.0000}", item.UnitPrice);
                    worksheet.Range["D" + rownum + ""].Text = item.Unit;
                    worksheet.Range["E" + rownum + ""].Text = string.Format("{0:0}", item.Quantity);
                    worksheet.Range["F" + rownum + ""].Text = string.Format("{0:0.00}", item.UnitPrice * item.Quantity);
                    worksheet.Range["G" + rownum + ""].Text = string.Format("{0:0.00}", item.TotalRMB);
                    worksheet.Range["H" + rownum + ""].Text = string.Format("{0:0.0000}", item.RMBUnitPrice);
                    i++;
                    rownum++;
                }

                decimal totalUSD = 0;
                decimal totalRMB = 0;

                foreach (var item in productList)
                {
                    totalUSD += item.TotalUSD;
                    totalRMB += item.TotalRMB;
                }

                rownum += 10;

                worksheet.Range["A" + rownum + ""].Text = "Total";
                worksheet.Range["$A$" + rownum + ":$E$" + rownum].Merge();

                worksheet.Range["F" + rownum + ""].Text = string.Format("${0:0.00}", totalUSD);
                worksheet.Range["G" + rownum + ""].Text = string.Format("¥{0:0.00}", totalRMB);

                headingStyle.Font.Size = 17;
                worksheet.Range["F" + rownum + ""].CellStyle = headingStyle;

                headingStyle.Font.Size = 17;
                worksheet.Range["G" + rownum + ""].CellStyle = headingStyle;

                rownum += 2;

                worksheet.Range["A" + rownum + ""].Text = "Terms And Conditions:";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                rownum += 1;
                worksheet.Range["A" + rownum + ""].Text = "(1)  Based On , FOB, X work, CNF, CIF";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                rownum += 1;
                worksheet.Range["A" + rownum + ""].Text = "(2)  Payment terms - advance.. Balance.. LC TT ";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                rownum += 1;
                worksheet.Range["A" + rownum + ""].Text = "(3)  Delivery period- … days after receipt/confirmation of ...";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                rownum += 1;
                worksheet.Range["A" + rownum + ""].Text = "(4)  Bank Details for remittance:  TT and LC both always show according to selection of exporter";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                PerfomaInvoice perfomaInvoice = new PerfomaInvoice()
                {
                    OrderId = id,
                    PerfomaInvoiceReferece = perfoma.PINo,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                dbContext.PerfomaInvoices.Add(perfomaInvoice);
                dbContext.SaveChanges();
                int perfomaId = perfomaInvoice.Id;

                foreach (var item in productList)
                {
                    PerfomaInvoiceItem perfomaInvoiceItem = new PerfomaInvoiceItem()
                    {
                        PerfomaInvoiceId = perfomaId,
                        ProductId = item.ProductId,
                        Dollar_ProductPrice = item.UnitPrice,
                        RMB_ProductPrice = item.RMBUnitPrice,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now

                    };

                    dbContext.PerfomaInvoiceItems.Add(perfomaInvoiceItem);
                }

                dbContext.SaveChanges();

                //Save the workbook to disk in xlsx format.
                workbook.SaveAs(@"/Content/PerfomaInvoice/" + perfomaInvoice.OrderId + perfoma.PINo + ".xlsx", HttpContext.ApplicationInstance.Response, ExcelDownloadType.Open);
            }

            return View();
        }

        public List<SelectListItem> GetProductSuppliers(int productId)
        {
            ArityEntities dataContext = new ArityEntities();
            List<SelectListItem> suppliers = new List<SelectListItem>();
            var suppliersCsv = (from data in dataContext.Products
                                where data.Id == productId && data.Suppliers != null
                                select new
                                {
                                    Id = data.Id,
                                    suppliers = data.Suppliers
                                }).ToList();

            foreach (var data in suppliersCsv)
            {
                var ids = data.suppliers != null && !string.IsNullOrEmpty(data.suppliers) ? data.suppliers.Split(new[] { ',' })
                              .Select(x => int.Parse(x))
                              .ToArray() : null;
                suppliers = ids != null ? (from u in dataContext.Users.ToList().Where(x => ids.Contains(x.Id) && x.UserType == (int)Common.EnumHelpers.UserType.Supplier)
                                           select new SelectListItem
                                           {
                                               Text = u.Id.ToString(),
                                               Value = u.CompanyName
                                           }).ToList() : suppliers;

            }

            return suppliers;
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
            try
            {
                if (Request.Cookies["UserId"] == null)
                {
                    Response.Redirect("/user/login");
                }
                var loggedInId = Convert.ToInt32(Request.Cookies["UserId"].Value);
                var objDb = new ArityEntities();
                //var orders = (from supplierorder in objDb.OrderLineItem_Supplier_Mapping.ToList()
                //              join supplieritem in objDb.Supplier_Assigned_OrderLineItem.ToList() on supplierorder.Id equals supplieritem.OrderSupplierMapId
                //              where supplieritem.SupplierId == loggedInId && supplierorder.OrderId != null
                //              select new
                //              {
                //                  SupplierOrderId = supplierorder.OrderId,
                //                  Prefix = supplierorder.Order.Prefix,
                //                  OrderId = supplierorder.OrderId,
                //                  CreatedOn = supplieritem.CreatedDate.ToString("dd/MM/yyyy"),
                //                  Quantity = supplieritem.Quantity,
                //                  OrderQuantity = supplierorder.Quantity,
                //                  //DollerSalesTotal = 0,
                //                  RmbSalesTotal = supplieritem.OrderLineItem1.RMBSalesPrice,
                //                  Status = supplieritem.Status
                //              }).ToList();

                var orders = (from supplierOrder in objDb.Supplier_Assigned_OrderLineItem.ToList()
                              join orderLineItem in objDb.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                              join order in objDb.Orders on orderLineItem.OrderId equals order.Id
                              where supplierOrder.SupplierId == loggedInId && supplierOrder.OrderLineItem != null && supplierOrder.Quantity > 0
                              select new
                              {
                                  Prefix = order.Prefix,
                                  OrderId = order.Id,
                                  CreatedOn = supplierOrder.CreatedDate.ToString("dd/MM/yyyy"),
                                  Quantity = supplierOrder.Quantity,
                                  RmbSalesTotal = supplierOrder.OrderLineItem1.RMBSalesPrice,
                                  Status = supplierOrder.Status
                              }).ToList();

                var final = orders.GroupBy(x => x.OrderId).Select(x => new
                {
                    SupplierOrderId = x.Key,
                    Prefix = x.Select(p => p.Prefix).FirstOrDefault(),
                    CreatedOn = x.Select(p => p.CreatedOn).FirstOrDefault(),
                    Quantity = x.Sum(p => p.Quantity),
                    RmbSalesTotal = x.Sum(p => p.Quantity * p.RmbSalesTotal),
                    Status = x.Select(p => p.Status).FirstOrDefault()
                }).ToList();

                return Json(new { data = final }, JsonRequestBehavior.AllowGet);
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
        public ActionResult SupplierCartonList(int OrderId)
        {
            try
            {
                using (var db = new ArityEntities())
                {
                    int loggedInId = Convert.ToInt32(Request.Cookies["UserId"].Value);
                    var model = (from a in db.SupplierCartoons
                                 join supplier in db.Supplier_Assigned_OrderLineItem on a.SupplierAssignedMapId equals supplier.Id
                                 //join b in db.OrderLineItem_Supplier_Mapping on supplier.SupplierId equals b.SupplierId
                                 join c in db.OrderLineItems on supplier.OrderLineItem equals c.Id
                                 where c.OrderId == OrderId && supplier.SupplierId == loggedInId
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
                                     TotalGrossWeight = a.TotalGrossWeight,
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

        public ActionResult AddSupplierCartonDetail(int orderId)
        {

            int loggedInId = Convert.ToInt32(Request.Cookies["UserId"].Value);
            SupplierCartoonModel carton = new SupplierCartoonModel();
            ArityEntities dbContext = new ArityEntities();
            ViewBag.OrderId = orderId;
            ViewBag.OrderName = dbContext.Orders.Where(x => x.Id == orderId).Select(x => x.Prefix).FirstOrDefault();
            //ViewBag.OrderDate = (from b in dbContext.OrderLineItem_Supplier_Mapping
            //                     join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
            //                     where b.OrderId == orderId && b.OrderId != null
            //                     select c.CreatedDate).FirstOrDefault();
            ViewBag.OrderDate = (from supplierOrder in dbContext.Supplier_Assigned_OrderLineItem
                                 join orderLineItem in dbContext.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                                 join order in dbContext.Orders on orderLineItem.OrderId equals order.Id
                                 where order.Id == orderId
                                 select supplierOrder.CreatedDate
                                 ).ToList().Min();

            //var Status = (from b in dbContext.OrderLineItem_Supplier_Mapping
            //              join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
            //              where b.OrderId == orderId && b.OrderId != null
            //              select c.Status).FirstOrDefault();
            //ViewBag.Status = ((AritySystems.Common.EnumHelpers.OrderStatus)Status).ToString();
            //var ordersLineItems = (from b in dbContext.OrderLineItem_Supplier_Mapping
            //                       join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
            //                       where b.OrderId == orderId && b.OrderId != null
            //                       select new { c.Id, c.OrderLineItem1.Product. }).ToList();
            var ordersLineItems = (from supplierOrder in dbContext.Supplier_Assigned_OrderLineItem
                                   join orderLineItem in dbContext.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                                   join order in dbContext.Orders on orderLineItem.OrderId equals order.Id
                                   where order.Id == orderId && supplierOrder.SupplierId == loggedInId
                                   select new { supplierOrder.Id, supplierOrder.OrderLineItem1.Product.Chinese_Name }).ToList();
            ViewBag.OrderLineItems = new SelectList(ordersLineItems, "Id", "Chinese_Name");
            //carton.PcsPerCartoon = (from b in dbContext.OrderLineItem_Supplier_Mapping
            //                        join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
            //                        where b.OrderId == orderId && b.OrderId != null
            //                        select c.Quantity).FirstOrDefault();
            carton.PcsPerCartoon = 0;
            carton.CartoonPrefix = (from u in dbContext.Users
                                    join o in dbContext.Orders on u.Id equals o.CustomerId
                                    where o.Id == orderId
                                    select u.Prefix).FirstOrDefault();
            carton.OrderId = orderId;
            return View(carton);
        }

        public string getEnumValue(int value)
        {
            Common.EnumHelpers.Units unitValue = (Common.EnumHelpers.Units)value;
            return unitValue.ToString();
        }

        private List<Product> GetChildProducts(int parentId)
        {
            ArityEntities dataContext = new ArityEntities();
            List<Product> childProductList = new List<Product>();

            var parentsCsv = (from data in dataContext.Products
                              where data.ParentIds != null && data.ParentIds != string.Empty
                              select new
                              {
                                  Id = data.Id,
                                  parents = data.ParentIds
                              }).ToList();

            foreach (var data in parentsCsv)
            {
                var ids = data.parents.Split(new[] { ',' })
                              .Select(x => int.Parse(x))
                              .ToArray();

                var childProductDetails = (from childProduct in dataContext.Products.ToList().Where(x => ids.Contains(parentId) && x.Id == data.Id)
                                           select new Product()
                                           {
                                               Id = childProduct.Id,
                                               Chinese_Name = childProduct.Chinese_Name,
                                               Description = childProduct.Description,
                                               Dollar_Price = childProduct.Dollar_Price,
                                               ModifiedDate = childProduct.ModifiedDate,
                                               English_Name = childProduct.English_Name,
                                               Quantity = childProduct.Quantity,
                                               RMB_Price = childProduct.RMB_Price,
                                               Unit = getEnumValue(Convert.ToInt32(childProduct.Unit)),
                                               ParentIds = childProduct.ParentIds,
                                               BOM = GetBOMForParent(parentId, childProduct.Id)
                                           }).FirstOrDefault();



                if (childProductDetails != null)
                {
                    childProductList.Add(childProductDetails);
                }

            }

            return childProductList;
        }

        private decimal GetBOMForParent(int parentId, int childId)
        {
            ArityEntities dataContext = new ArityEntities();
            var data = dataContext.BOM_Mapper.Where(x => x.ProductId == parentId && x.ChildId == childId).Select(x => x.BOM).FirstOrDefault();

            data = data != null ? data : 0;

            return data.Value;
        }
        public JsonResult AssignSalesPersonToOrder(int id, int salesPerson)
        {
            var dbContext = new ArityEntities();
            var order = dbContext.Orders.Where(_ => _.Id == id).FirstOrDefault();
            if (order != null)
            {
                order.Sales_Person_Id = salesPerson > 0 ? salesPerson : (int?)null;
                dbContext.SaveChanges();
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public JsonResult AssignExporterToOrder(int id, int exporterId)
        {
            var dbContext = new ArityEntities();
            var order = dbContext.Orders.Where(_ => _.Id == id).FirstOrDefault();
            if (order != null)
            {
                order.ExporterId = exporterId > 0 ? exporterId : (int?)null;
                dbContext.SaveChanges();
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public ActionResult Account()
        {
            return View();
        }

        [HttpGet]
        public ActionResult AddPayments(int? id)
        {
            var dbContext = new ArityEntities();
            ViewBag.OrderID = id ?? 0;
            var orderCIPI = dbContext.PerfomaInvoices.Where(_ => _.OrderId == id).Select(_ => new OrderCIPIModel()
            {
                Id = _.Id,
                Name = _.PerfomaInvoiceReferece,
                Type = "PI"
            }).ToList();
            orderCIPI = orderCIPI.Union(dbContext.CommercialInvoices.Where(_ => _.OrderId == id).Select(_ => new OrderCIPIModel()
            {
                Id = _.Id,
                Name = _.CommercialInvoiceReferece,
                Type = "Com"
            }).ToList()).ToList();
            orderCIPI = orderCIPI.Union(
                (from user in dbContext.Users.ToList()
                 join supplier in dbContext.Supplier_Assigned_OrderLineItem on user.Id equals supplier.SupplierId
                 where supplier.OrderSupplierMapId == id
                 select new OrderCIPIModel()
                 {
                     Id = user.Id,
                     Name = user.CompanyName,
                     Type = "Supplier"
                 }).ToList()).ToList();
            ViewBag.OrderCI = orderCIPI;
            return View();
        }

        [HttpPost]
        public ActionResult AddPaymentForOrder(int id, int ciId, decimal dAmmount, decimal rAmmount, string type)
        {
            var dbContext = new ArityEntities();
            try
            {
                dbContext.Accounts.Add(new Data.Account()
                {
                    CommercialId = type != null && type.Equals("Com") ? ciId : (int?)null,
                    PerfomaId = type != null && type.Equals("PI") ? ciId : (int?)null,
                    SupplierId = type != null && type.Equals("Supplier") ? ciId : (int?)null,
                    CreatedDate = DateTime.Now,
                    OrderId = id,
                    Dollar_Price = dAmmount,
                    RMB_Price = rAmmount
                });
                dbContext.SaveChanges();
            }
            catch
            {
                throw;
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        public JsonResult OrderPaidAmmounts(int id)
        {
            var dbContext = new ArityEntities();

            var ammounts = (from _ in dbContext.Accounts.ToList()
                            join com in dbContext.PerfomaInvoices on _.PerfomaId equals com.Id
                            join order in dbContext.Orders on _.OrderId equals order.Id
                            join user in dbContext.Users on order.User.Id equals user.Id
                            where _.OrderId == id && _.PerfomaId != null
                            select new
                            {
                                CIId = _.Id,
                                ShippingMark = order.Prefix,
                                CI = com.PerfomaInvoiceReferece,
                                DollerPrice = _.Dollar_Price.HasValue ? Math.Round(Convert.ToDouble(_.Dollar_Price.Value), 2) : 0.00,
                                RMBPrice = _.RMB_Price.HasValue ? Math.Round(Convert.ToDouble(_.RMB_Price.Value), 2) : 0.00,
                                Date = _.CreatedDate.HasValue ? _.CreatedDate.Value.ToString("dd/MM/yyyy") : null
                            }).ToList();
            ammounts = ammounts.Union((from _ in dbContext.Accounts.ToList()
                                       join com in dbContext.CommercialInvoices on _.CommercialId equals com.Id
                                       join order in dbContext.Orders on _.OrderId equals order.Id
                                       join user in dbContext.Users on order.User.Id equals user.Id
                                       where _.OrderId == id && _.CommercialId != null
                                       select new
                                       {
                                           CIId = _.Id,
                                           ShippingMark = order.Prefix,
                                           CI = com.CommercialInvoiceReferece,
                                           DollerPrice = _.Dollar_Price.HasValue ? Math.Round(Convert.ToDouble(_.Dollar_Price.Value), 2) : 0.00,
                                           RMBPrice = _.RMB_Price.HasValue ? Math.Round(Convert.ToDouble(_.RMB_Price.Value), 2) : 0.00,
                                           Date = _.CreatedDate.HasValue ? _.CreatedDate.Value.ToString("dd/MM/yyyy") : null
                                       }).ToList()).ToList();
            return Json(new { data = ammounts }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult GetOrderOrderListForAmmount()
        {
            var dbContext = new ArityEntities();
            var ammounts = (from order in dbContext.Orders.ToList()
                            select new
                            {
                                Id = order.Id,
                                ShippingMark = order.Prefix,
                                CreatedDate = order.CreatedDate.ToString("dd/MM/yyyy"),
                                TotalDollerPrice = order.Accounts.Sum(_ => _.Dollar_Price),
                                TotalRMBPrice = order.Accounts.Sum(_ => _.RMB_Price)
                            }).ToList();
            return Json(new { data = ammounts }, JsonRequestBehavior.AllowGet);
        }

        public JsonResult RemovePayment(int id)
        {
            var dbContext = new ArityEntities();
            dbContext.Accounts.Remove(dbContext.Accounts.FirstOrDefault(_ => _.Id == id));
            dbContext.SaveChanges();
            return Json("", JsonRequestBehavior.AllowGet);
        }
        [HttpPost]
        public JsonResult TermsandCondition(int id, string Tearms)
        {
            var dbContext = new ArityEntities();
            var order = dbContext.Orders.FirstOrDefault(_ => _.Id == id);
            order.TermsandCondition = Tearms;
            dbContext.SaveChanges();
            return Json("", JsonRequestBehavior.AllowGet);
        }

        #region CommercialInvoie

        public ActionResult GenerateCommmercialInvoice(int? id)
        {
            ArityEntities dbContext = new ArityEntities();
            List<PerfomaProductList> productList = new List<PerfomaProductList>();
            int asIntegers = 0;
            List<string> commercialList = dbContext.CommercialInvoices.Where(x => x.OrderId == id).Select(x => x.CommercialInvoiceReferece).ToList();
            if (commercialList != null && commercialList.Count > 0)
            {
                List<string> maxPerfoma = commercialList.Select(x => x.Replace("CI", "")).ToList();
                asIntegers = maxPerfoma.Select(s => int.Parse(s)).ToArray().Max();
            }
            var commercial = (from order in dbContext.Orders
                              join user in dbContext.Users on order.CustomerId equals user.Id
                              join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                              join product in dbContext.Products on lineItem.ProductId equals product.Id
                              join exporterDetail in dbContext.Users on order.ExporterId equals exporterDetail.Id
                              where order.Id == id && exporterDetail.UserType == 6
                              select new PerformaInvoice()
                              {
                                  ExporterName = exporterDetail.CompanyName,
                                  ExporterAddress = exporterDetail.Address,
                                  ExporterPhone = exporterDetail.PhoneNumber,
                                  CustomerCompanyName = user.CompanyName,
                                  CustomerAddress = user.Address,
                                  CustomerGST = user.GSTIN,
                                  PINo = "CI" + (asIntegers + 1),
                                  OrderDate = order.CreatedDate,
                                  IECCode = user.IECCode,
                                  CustomerName = user.FirstName + " " + user.LastName,
                                  CustomerPhone = user.PhoneNumber
                              }).FirstOrDefault();

            if (commercial == null)
            {
                return Content("No Such Order Found");
            }

            DateTime? lastCommercialDt = dbContext.CommercialInvoices.Where(x => x.OrderId == id).OrderByDescending(x => x.ModifiedDate).FirstOrDefault()?.ModifiedDate;
            lastCommercialDt = lastCommercialDt ?? DateTime.MinValue;

            productList = (from cartoon in dbContext.SupplierCartoons
                           join supplierAsigned in dbContext.Supplier_Assigned_OrderLineItem on cartoon.SupplierAssignedMapId equals supplierAsigned.Id
                           join lineItem in dbContext.OrderLineItems on supplierAsigned.OrderLineItem equals lineItem.Id
                           join product in dbContext.Products on lineItem.ProductId equals product.Id
                           where cartoon.OrderId == id && cartoon.Status == 2 && cartoon.ModifiedDate > lastCommercialDt
                           select new
                           {
                               c = cartoon,
                               p = product,
                               oli = lineItem
                           }).AsEnumerable().Select(x => new PerfomaProductList()
                           {
                               Partiular = x.p.English_Name,
                               Quantity = x.c.PcsPerCartoon * x.c.TotalCartoons,
                               Unit = getEnumValue(Convert.ToInt32(x.p.Unit)),
                               UnitPrice = x.oli.DollarSalesPrice,
                               TotalUSD = (x.c.PcsPerCartoon * x.c.TotalCartoons) * x.oli.DollarSalesPrice,
                               ProductId = x.p.Id,
                               RMBUnitPrice = x.oli.RMBSalesPrice,
                               TotalRMB = (x.c.PcsPerCartoon * x.c.TotalCartoons) * x.oli.RMBSalesPrice
                           }).ToList();

            commercial.ProductList = productList ?? new List<PerfomaProductList>();


            //Create an instance of ExcelEngine.
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                //Set the default application version as Excel 2016.
                excelEngine.Excel.DefaultVersion = ExcelVersion.Excel2016;

                //Create a workbook with a worksheet.
                IWorkbook workbook = excelEngine.Excel.Workbooks.Create(1);

                //Access first worksheet from the workbook instance.
                IWorksheet worksheet = workbook.Worksheets[0];

                //Insert sample text into cell “A1”.
                worksheet.Range["A1"].Text = commercial.ExporterName;
                worksheet.Range["$A$1:$F$1"].Merge();

                IStyle headingStyle = workbook.Styles.Add("HeadingStyle");
                headingStyle.Font.Bold = true;
                headingStyle.Font.Size = 20;
                headingStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$1:$F$1"].CellStyle = headingStyle;

                worksheet.Range["A2"].Text = commercial.ExporterAddress;
                worksheet.Range["$A$2:$F$2"].Merge();

                IStyle exporterAdress = workbook.Styles.Add("exporterAdress");
                exporterAdress.Font.Size = 15;
                exporterAdress.Font.Bold = true;
                exporterAdress.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$2:$F$2"].CellStyle = exporterAdress;

                worksheet.Range["A3"].Text = commercial.ExporterPhone;
                worksheet.Range["$A$3:$F$3"].Merge();

                worksheet.Range["$A$3:$F$3"].CellStyle = exporterAdress;

                worksheet.Range["A4"].Text = string.Empty;
                worksheet.Range["$A$4:$F$4"].Merge();

                worksheet.Range["A5"].Text = "Commercial Invoice";
                worksheet.Range["$A$5:$F$5"].Merge();

                IStyle CustomTextStyle = workbook.Styles.Add("CustomTextStyle");
                CustomTextStyle.Font.Size = 25;
                CustomTextStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                CustomTextStyle.Font.Bold = true;
                worksheet.Range["$A$5:$F$5"].CellStyle = CustomTextStyle;

                worksheet.Range["A6"].Text = commercial.CustomerCompanyName;
                worksheet.Range["$A$6:$B$6"].Merge();

                IStyle CustomTextCustomerStyle = workbook.Styles.Add("CustomTextCustomerStyle");
                CustomTextCustomerStyle.Font.Size = 15;
                CustomTextCustomerStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                worksheet.Range["$A$6:$B$6"].CellStyle = CustomTextCustomerStyle;

                worksheet.Range["C6"].Text = "Inv No.";

                worksheet.Range["D6"].Text = commercial.PINo;
                worksheet.Range["$D$6:$F$6"].Merge();

                worksheet.Range["A7"].Text = commercial.CustomerAddress;
                worksheet.Range["$A$7:$B$7"].Merge();

                worksheet.Range["C7"].Text = "Date";

                worksheet.Range["D7"].Text = commercial.OrderDate.ToString();
                worksheet.Range["$D$7:$F$7"].Merge();

                worksheet.Range["A8"].Text = commercial.CustomerGST;
                worksheet.Range["$A$8:$B$8"].Merge();

                worksheet.Range["C8"].Text = "IEC code";

                worksheet.Range["D8"].Text = commercial.IECCode;
                worksheet.Range["$D$8:$F$8"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["C9"].Text = "Name";

                worksheet.Range["D9"].Text = commercial.CustomerName;
                worksheet.Range["$D$9:$F$9"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["C10"].Text = "Contact No.";

                worksheet.Range["D10"].Text = commercial.CustomerPhone;
                worksheet.Range["$D$9:$F$9"].Merge();

                IStyle headingLineItemStyle = workbook.Styles.Add("headingLineItemStyle");
                headingLineItemStyle.Font.Size = 15;
                headingLineItemStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;

                headingLineItemStyle.Font.Bold = true;

                worksheet.AutofitRow(11);

                worksheet.AutofitColumn(1);
                worksheet.AutofitColumn(2);
                worksheet.AutofitColumn(3);
                worksheet.AutofitColumn(4);
                worksheet.AutofitColumn(5);
                worksheet.AutofitColumn(6);
                worksheet.AutofitColumn(7);
                worksheet.AutofitColumn(8);

                worksheet.Range["A11"].Text = "Sr.No.";
                worksheet.Range["B11"].Text = "Perticulates";
                worksheet.Range["C11"].Text = "UP USD";
                worksheet.Range["D11"].Text = "Unit";
                worksheet.Range["E11"].Text = "Qty";
                worksheet.Range["F11"].Text = "Total USD";
                worksheet.Range["G11"].Text = "Total RMB";
                worksheet.Range["H11"].Text = "RMB";


                worksheet.Range["A11"].CellStyle = headingLineItemStyle;
                worksheet.Range["B11"].CellStyle = headingLineItemStyle;
                worksheet.Range["C11"].CellStyle = headingLineItemStyle;
                worksheet.Range["D11"].CellStyle = headingLineItemStyle;
                worksheet.Range["E11"].CellStyle = headingLineItemStyle;
                worksheet.Range["F11"].CellStyle = headingLineItemStyle;
                worksheet.Range["G11"].CellStyle = headingLineItemStyle;
                worksheet.Range["H11"].CellStyle = headingLineItemStyle;


                int i = 1;
                int rownum = 12;
                foreach (var item in commercial.ProductList)
                {
                    worksheet.Range["A" + rownum + ""].Text = i.ToString();
                    worksheet.Range["B" + rownum + ""].Text = item.Partiular;
                    worksheet.Range["C" + rownum + ""].Text = item.UnitPrice.ToString();
                    worksheet.Range["D" + rownum + ""].Text = item.Unit;
                    worksheet.Range["E" + rownum + ""].Text = item.Quantity.ToString();
                    worksheet.Range["F" + rownum + ""].Text = item.TotalUSD.ToString();
                    worksheet.Range["G" + rownum + ""].Text = item.TotalRMB.ToString();
                    worksheet.Range["H" + rownum + ""].Text = item.RMBUnitPrice.ToString();
                    i++;
                    rownum++;
                }

                decimal totalItems = 0;
                decimal totalRMB = 0;

                foreach (var item in productList)
                {
                    totalItems += item.TotalUSD;
                    totalRMB += item.TotalRMB;
                }

                rownum += 10;

                worksheet.Range["A" + rownum + ""].Text = "Total Rd Off";
                worksheet.Range["$A$" + rownum + ":$E$" + rownum].Merge();

                worksheet.Range["F" + rownum + ""].Text = "$" + totalItems.ToString();
                worksheet.Range["G" + rownum + ""].Text = "¥" + totalRMB.ToString();

                headingStyle.Font.Size = 17;
                worksheet.Range["F" + rownum + ""].CellStyle = headingStyle;

                headingStyle.Font.Size = 17;
                worksheet.Range["G" + rownum + ""].CellStyle = headingStyle;

                rownum += 2;

                worksheet.Range["A" + rownum + ""].Text = "For, Exporter co. Name";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                //Save the workbook to disk in xlsx format.
                workbook.SaveAs(@"/Content/CommercialInvoice/" + id + ".xlsx", HttpContext.ApplicationInstance.Response, ExcelDownloadType.Open);
            }

            CommercialInvoice commercialInvoice = new CommercialInvoice
            {
                OrderId = id,
                CommercialInvoiceReferece = commercial.PINo,
                CreatedDate = DateTime.Now,
                ModifiedDate = DateTime.Now
            };

            dbContext.CommercialInvoices.Add(commercialInvoice);
            dbContext.SaveChanges();

            return View();
        }

        #endregion

        #region PackingList

        public ActionResult GeneratePackingList(int? id)
        {
            ArityEntities dbContext = new ArityEntities();

            List<PerfomaProductList> productList = new List<PerfomaProductList>();
            int asIntegers = 0;
            List<string> commercialList = dbContext.CommercialInvoices.Where(x => x.OrderId == id).Select(x => x.CommercialInvoiceReferece).ToList();
            if (commercialList != null && commercialList.Count > 0)
            {
                List<string> maxPerfoma = commercialList.Select(x => x.Replace("CI", "")).ToList();
                asIntegers = maxPerfoma.Select(s => int.Parse(s)).ToArray().Max();
            }
            var commercial = (from order in dbContext.Orders
                              join user in dbContext.Users on order.CustomerId equals user.Id
                              join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                              join product in dbContext.Products on lineItem.ProductId equals product.Id
                              join exporterDetail in dbContext.Users on order.ExporterId equals exporterDetail.Id
                              where order.Id == id && exporterDetail.UserType == 6
                              select new PerformaInvoice()
                              {
                                  ExporterName = exporterDetail.CompanyName,
                                  ExporterAddress = exporterDetail.Address,
                                  ExporterPhone = exporterDetail.PhoneNumber,
                                  CustomerCompanyName = user.CompanyName,
                                  CustomerAddress = user.Address,
                                  CustomerGST = user.GSTIN,
                                  PINo = "CI" + (asIntegers + 1),
                                  OrderDate = order.CreatedDate,
                                  IECCode = user.IECCode,
                                  CustomerName = user.FirstName + " " + user.LastName,
                                  CustomerPhone = user.PhoneNumber
                              }).FirstOrDefault();

            DateTime? lastCommercialDt = dbContext.CommercialInvoices.Where(x => x.OrderId == id).OrderByDescending(x => x.ModifiedDate).FirstOrDefault().ModifiedDate;

            productList = (from cartoon in dbContext.SupplierCartoons
                           join supplierAsigned in dbContext.Supplier_Assigned_OrderLineItem on cartoon.SupplierAssignedMapId equals supplierAsigned.Id
                           join lineItem in dbContext.OrderLineItems on supplierAsigned.OrderLineItem equals lineItem.Id
                           join product in dbContext.Products on lineItem.ProductId equals product.Id
                           join perfoma in dbContext.PerfomaInvoiceItems on product.Id equals perfoma.ProductId
                           where cartoon.OrderId == id && cartoon.Status == 2 && cartoon.ModifiedDate > lastCommercialDt
                           select new
                           {
                               c = cartoon,
                               p = product,
                               oli = lineItem
                           }).AsEnumerable().Select(x => new PerfomaProductList()
                           {
                               Partiular = x.p.English_Name,
                               Quantity = x.c.PcsPerCartoon * x.c.TotalCartoons,
                               Unit = getEnumValue(Convert.ToInt32(x.p.Unit)),
                               UnitPrice = x.oli.DollarSalesPrice,
                               TotalUSD = (x.c.PcsPerCartoon * x.c.TotalCartoons) * x.oli.DollarSalesPrice,
                               ProductId = x.p.Id,
                               RMBUnitPrice = x.oli.RMBSalesPrice,
                               TotalRMB = (x.c.PcsPerCartoon * x.c.TotalCartoons) * x.oli.RMBSalesPrice
                           }).ToList();


            commercial.ProductList = productList;


            //Create an instance of ExcelEngine.
            using (ExcelEngine excelEngine = new ExcelEngine())
            {
                //Set the default application version as Excel 2016.
                excelEngine.Excel.DefaultVersion = ExcelVersion.Excel2016;

                //Create a workbook with a worksheet.
                IWorkbook workbook = excelEngine.Excel.Workbooks.Create(1);

                //Access first worksheet from the workbook instance.
                IWorksheet worksheet = workbook.Worksheets[0];

                //Insert sample text into cell “A1”.
                worksheet.Range["A1"].Text = commercial.ExporterName;
                worksheet.Range["$A$1:$N$1"].Merge();

                IStyle headingStyle = workbook.Styles.Add("HeadingStyle");
                headingStyle.Font.Bold = true;
                headingStyle.Font.Size = 20;
                headingStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$1:$N$1"].CellStyle = headingStyle;

                worksheet.Range["A2"].Text = commercial.ExporterAddress;
                worksheet.Range["$A$2:$N$2"].Merge();

                IStyle exporterAdress = workbook.Styles.Add("exporterAdress");
                exporterAdress.Font.Size = 15;
                exporterAdress.Font.Bold = true;
                exporterAdress.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                worksheet.Range["$A$2:$N$2"].CellStyle = exporterAdress;

                worksheet.Range["A3"].Text = commercial.ExporterPhone;
                worksheet.Range["$A$3:$N$3"].Merge();
                worksheet.Range["$A$3:$N$3"].CellStyle = exporterAdress;

                worksheet.Range["A4"].Text = string.Empty;
                worksheet.Range["$A$4:$N$4"].Merge();

                worksheet.Range["A5"].Text = "Packing List";
                worksheet.Range["$A$5:$N$5"].Merge();

                IStyle CustomTextStyle = workbook.Styles.Add("CustomTextStyle");
                CustomTextStyle.Font.Size = 25;
                CustomTextStyle.HorizontalAlignment = ExcelHAlign.HAlignCenter;
                CustomTextStyle.Font.Bold = true;
                worksheet.Range["$A$5:$N$5"].CellStyle = CustomTextStyle;

                worksheet.Range["A6"].Text = commercial.CustomerCompanyName;
                worksheet.Range["$A$6:$B$6"].Merge();

                IStyle CustomTextCustomerStyle = workbook.Styles.Add("CustomTextCustomerStyle");
                CustomTextCustomerStyle.Font.Size = 15;
                CustomTextCustomerStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                worksheet.Range["$A$6:$B$6"].CellStyle = CustomTextCustomerStyle;

                worksheet.Range["J6"].Text = "Inv No.";
                worksheet.Range["$J$6:$K$6"].Merge();

                worksheet.Range["L6"].Text = commercial.PINo;
                worksheet.Range["$L$6:$N$6"].Merge();

                worksheet.Range["A7"].Text = commercial.CustomerAddress;
                worksheet.Range["$A$7:$B$7"].Merge();

                worksheet.Range["J7"].Text = "Date";
                worksheet.Range["$J$7:$K$7"].Merge();

                worksheet.Range["L7"].Text = commercial.OrderDate.ToString();
                worksheet.Range["$L$7:$N$7"].Merge();

                worksheet.Range["A8"].Text = string.Empty;
                worksheet.Range["$A$8:$B$8"].Merge();

                worksheet.Range["J8"].Text = string.Empty;
                worksheet.Range["$J$8:$K$8"].Merge();

                worksheet.Range["L8"].Text = commercial.IECCode;
                worksheet.Range["$L$8:$N$8"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["J9"].Text = "Name";
                worksheet.Range["$J$9:$K$9"].Merge();

                worksheet.Range["L9"].Text = commercial.CustomerName;
                worksheet.Range["$L$9:$N$9"].Merge();

                worksheet.Range["A9"].Text = string.Empty;
                worksheet.Range["$A$9:$B$9"].Merge();

                worksheet.Range["J10"].Text = "Contact No.";
                worksheet.Range["$J$10:$K$10"].Merge();

                worksheet.Range["L10"].Text = commercial.CustomerPhone;
                worksheet.Range["$L$10:$N$10"].Merge();

                IStyle headingLineItemStyle = workbook.Styles.Add("headingLineItemStyle");
                headingLineItemStyle.Font.Size = 15;
                headingLineItemStyle.HorizontalAlignment = ExcelHAlign.HAlignLeft;
                headingLineItemStyle.Font.Bold = true;

                worksheet.AutofitRow(11);

                worksheet.AutofitColumn(1);
                worksheet.AutofitColumn(2);
                worksheet.AutofitColumn(3);
                worksheet.AutofitColumn(4);
                worksheet.AutofitColumn(5);
                worksheet.AutofitColumn(6);
                worksheet.AutofitColumn(7);
                worksheet.AutofitColumn(8);
                worksheet.AutofitColumn(9);
                worksheet.AutofitColumn(10);
                worksheet.AutofitColumn(11);
                worksheet.AutofitColumn(12);
                worksheet.AutofitColumn(13);
                worksheet.AutofitColumn(14);

                worksheet.Range["A11"].Text = "Sr.No.";
                worksheet.Range["B11"].Text = "Perticulates";
                worksheet.Range["C11"].Text = "QTY/PCS";
                worksheet.Range["D11"].Text = "PCS/CTN";
                worksheet.Range["E11"].Text = "CTNS";
                worksheet.Range["F11"].Text = "N.W.(kg)";
                worksheet.Range["G11"].Text = "T.N.W";
                worksheet.Range["H11"].Text = "G.W.(kg)";
                worksheet.Range["I11"].Text = "T.G.W";
                worksheet.Range["J11"].Text = "CARTON SIZE（CM)";
                worksheet.Range["$J$11:$L$11"].Merge();
                worksheet.Range["M11"].Text = " CBM(M3)";
                worksheet.Range["N11"].Text = "CTN/NO.";



                worksheet.Range["A11"].CellStyle = headingLineItemStyle;
                worksheet.Range["B11"].CellStyle = headingLineItemStyle;
                worksheet.Range["C11"].CellStyle = headingLineItemStyle;
                worksheet.Range["D11"].CellStyle = headingLineItemStyle;
                worksheet.Range["E11"].CellStyle = headingLineItemStyle;
                worksheet.Range["F11"].CellStyle = headingLineItemStyle;
                worksheet.Range["G11"].CellStyle = headingLineItemStyle;
                worksheet.Range["H11"].CellStyle = headingLineItemStyle;
                worksheet.Range["I11"].CellStyle = headingLineItemStyle;
                worksheet.Range["J11"].CellStyle = headingLineItemStyle;
                worksheet.Range["M11"].CellStyle = headingLineItemStyle;
                worksheet.Range["N11"].CellStyle = headingLineItemStyle;

                int i = 1;
                int rownum = 12;
                foreach (var item in commercial.ProductList)
                {
                    worksheet.Range["A" + rownum + ""].Text = i.ToString();
                    worksheet.Range["B" + rownum + ""].Text = item.Partiular;
                    worksheet.Range["C" + rownum + ""].Text = item.UnitPrice.ToString();
                    worksheet.Range["D" + rownum + ""].Text = item.Unit.ToString();
                    worksheet.Range["E" + rownum + ""].Text = item.Quantity.ToString();
                    worksheet.Range["F" + rownum + ""].Text = (item.UnitPrice * item.Quantity).ToString();
                    worksheet.Range["G" + rownum + ""].Text = item.TotalRMB.ToString();
                    worksheet.Range["H" + rownum + ""].Text = item.RMBUnitPrice.ToString();
                    i++;
                    rownum++;
                }

                decimal totalItems = 0;
                decimal totalRMB = 0;

                foreach (var item in productList)
                {
                    totalItems += item.TotalUSD;
                    totalRMB += item.TotalRMB;
                }

                rownum += 10;

                worksheet.Range["A" + rownum + ""].Text = "Total Rd Off";
                worksheet.Range["$A$" + rownum + ":$E$" + rownum].Merge();

                worksheet.Range["F" + rownum + ""].Text = "$" + totalItems.ToString();
                worksheet.Range["G" + rownum + ""].Text = "¥" + totalRMB.ToString();

                headingStyle.Font.Size = 17;
                worksheet.Range["F" + rownum + ""].CellStyle = headingStyle;

                headingStyle.Font.Size = 17;
                worksheet.Range["G" + rownum + ""].CellStyle = headingStyle;

                rownum += 2;

                worksheet.Range["A" + rownum + ""].Text = "For, Exporter co. Name";
                worksheet.Range["$A$" + rownum + ":$F$" + rownum].Merge();

                PerfomaInvoice perfomaInvoice = new PerfomaInvoice()
                {
                    OrderId = id,
                    PerfomaInvoiceReferece = commercial.PINo,
                    CreatedDate = DateTime.Now,
                    ModifiedDate = DateTime.Now
                };

                dbContext.PerfomaInvoices.Add(perfomaInvoice);
                dbContext.SaveChanges();
                int perfomaId = perfomaInvoice.Id;

                foreach (var item in productList)
                {
                    PerfomaInvoiceItem perfomaInvoiceItem = new PerfomaInvoiceItem()
                    {
                        PerfomaInvoiceId = perfomaId,
                        ProductId = item.ProductId,
                        Dollar_ProductPrice = item.UnitPrice,
                        RMB_ProductPrice = item.RMBUnitPrice,
                        CreatedDate = DateTime.Now,
                        ModifiedDate = DateTime.Now

                    };

                    dbContext.PerfomaInvoiceItems.Add(perfomaInvoiceItem);
                }

                dbContext.SaveChanges();

                //Save the workbook to disk in xlsx format.
                workbook.SaveAs(@"/Content/PerfomaInvoice/" + perfomaInvoice.OrderId + commercial.PINo + ".xlsx", HttpContext.ApplicationInstance.Response, ExcelDownloadType.Open);
            }

            return View();
        }

        #endregion

        /// <summary>
        /// Load items details for supplier order by id
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonResult SupplierOrderLineItems(int id)
        {
            var dbContext = new ArityEntities();
            int loggedInId = Convert.ToInt32(Request.Cookies["UserId"].Value);
            var orders = (from supplierOrder in dbContext.Supplier_Assigned_OrderLineItem.ToList()
                          join orderLineItem in dbContext.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                          join order in dbContext.Orders on orderLineItem.OrderId equals order.Id
                          where supplierOrder.SupplierId == loggedInId && supplierOrder.OrderLineItem != null && order.Id == id
                          select new
                          {
                              SupplierOrderId = order.Id,
                              OrderId = order.Id,
                              Quantity = supplierOrder.Quantity,
                              OrderQuantity = supplierOrder.Quantity,
                              ItemId = supplierOrder.OrderLineItem,
                              ProductName = GetProductName(dbContext, supplierOrder.OrderLineItem ?? 0),
                              Status = supplierOrder.Status
                          }).ToList();
            return Json(new { data = orders }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Landing page for dispatch order
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult OrderDispatch(int? id)
        {
            var db = new ArityEntities();
            var order = db.Orders.Where(_ => _.Id == (id ?? 0)).FirstOrDefault();
            return View(order);
        }

        public JsonResult DispatchOrderItems(int? id)
        {
            var dbContext = new ArityEntities();
            var orders = (from supplierOrder in dbContext.Supplier_Assigned_OrderLineItem.ToList()
                          join orderLineItem in dbContext.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                          join order in dbContext.Orders on orderLineItem.OrderId equals order.Id
                          join cartoon in dbContext.SupplierCartoons on supplierOrder.Id equals cartoon.SupplierAssignedMapId
                          where order.Id == id
                          select new
                          {
                              OrderId = order.Id,
                              CartoonId = cartoon.Id,
                              TotalCartoons = cartoon.TotalCartoons,
                              ExpectedDate = supplierOrder.ExpectedTimeDelivery,
                              CartoonNumber = cartoon.CartoonNumber,
                              Quantity = supplierOrder.Quantity,
                              OrderQuantity = supplierOrder.Quantity,
                              ItemId = supplierOrder.OrderLineItem,
                              ProductName = GetProductName(dbContext, supplierOrder.OrderLineItem ?? 0),
                              SupplierName = GetSupplierName(dbContext, supplierOrder.SupplierId ?? 0),
                              Status = cartoon.Status
                          }).ToList();
            return Json(new { data = orders }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Set cartoon status as Dispatch
        /// </summary>
        /// <param name="addData"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> MarkCartoonDispatch(List<int> addData)
        {
            try
            {
                var dbContext = new ArityEntities();
                foreach (var cartoonId in addData)
                {
                    var cartoon = await dbContext.SupplierCartoons.Where(_ => _.Id == cartoonId).FirstOrDefaultAsync();
                    if (cartoon != null)
                        cartoon.Status = (int)AritySystems.Common.EnumHelpers.CartoonStatus.Dispatch;
                }
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Set or Update Expected delivery date of order of each supplier
        /// </summary>
        /// <param name="postData"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<JsonResult> SetExpectedDeliveryDate(List<ExpectedDeliveryDateModel> postData)
        {
            try
            {
                var dbContext = new ArityEntities();
                foreach (var date in postData)
                {
                    if (date.ExpectedDeliveryDate != null)
                    {
                        var orders = (from supplierOrder in dbContext.Supplier_Assigned_OrderLineItem.ToList()
                                      join orderLineItem in dbContext.OrderLineItems on supplierOrder.OrderLineItem equals orderLineItem.Id
                                      join order in dbContext.Orders on orderLineItem.OrderId equals order.Id
                                      where order.Id == date.OrderId && supplierOrder.SupplierId == date.SupplierId
                                      select supplierOrder).ToList();
                        orders.ForEach(_ => { _.ExpectedTimeDelivery = date.ExpectedDeliveryDate; });
                    }
                }
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw;
            }
            return Json(true, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Get Supplier company name
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="supplierId"></param>
        /// <returns></returns>
        private string GetSupplierName(ArityEntities dbContext, int supplierId)
        {
            return dbContext.Users.Where(_ => _.Id == supplierId).Select(_ => _.CompanyName).FirstOrDefault();
        }

        /// <summary>
        /// Supportive method to pass dbcontext and item id it will return product name
        /// </summary>
        /// <param name="dbContext"></param>
        /// <param name="productId"></param>
        /// <returns></returns>
        private string GetProductName(ArityEntities dbContext, int itemId)
        {
            return (from product in dbContext.Products
                    join orderLineItem in dbContext.OrderLineItems on product.Id equals orderLineItem.ProductId
                    where orderLineItem.Id == itemId
                    select product.Chinese_Name
                    ).FirstOrDefault();
        }

    }
}