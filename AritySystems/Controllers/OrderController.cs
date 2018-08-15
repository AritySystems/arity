using System;
using System.Linq;
using System.Web.Mvc;
using AritySystems.Models;
using AritySystems.Data;
using System.Collections.Generic;
using Syncfusion.XlsIO;


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
                                 where m.OrderId == OrderId && m.Quantity > 0
                                 select new OrderLineItemViewModel {
                                     Id = m.Id,
                                     //OrderId = m.OrderId ?? 0,
                                     Order_Name = n.Prefix,
                                     //ProductId = m.ProductId ?? 0,
                                     Product_Name = o.English_Name + "(" + o.Chinese_Name + ")",
                                     Purchase_Price_dollar = m.DollarPurchasePrice,
                                     Sales_Price_dollar = m.DollarSalesPrice,
                                     Purchase_Price_rmb = m.RMBPurchasePrice,
                                     Sales_Price_rmb = m.RMBSalesPrice,
                                     quantity = m.Quantity,
                                     //CreatedDate = m.CreatedDate.ToString(),
                                     //ModifiedDate = m.ModifiedDate ?? DateTime.MinValue,
                                     Suppliers = (from a in db.Users
                                                  join b in db.UserTypes on a.Id equals b.UserId
                                                  where b.Id == 1
                                                  select new SelectListItem
                                                  {
                                                      Text = a.Id.ToString(),
                                                      Value = a.FirstName + " " + a.LastName
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
                            var orderItemId = Convert.ToInt32(item.OrderLineItemId);
                            var quantity = Convert.ToDecimal(item.NewQuantity);
                            var oldQuantity = Convert.ToDecimal(item.OldQuantity);
                            //OrderLineItem ActualQuantity = db.OrderLineItems.Where(x => x.Id == orderItemId).FirstOrDefault();
                            if (oldQuantity > quantity)
                            {
                                newQuantity = oldQuantity - quantity;
                                //db.SaveChanges();
                            }
                            if (oldQuantity > 0)
                            {
                                OrderLineItem remainData = db.OrderLineItems.Where(x => x.Id == orderItemId).FirstOrDefault();
                                remainData.Quantity = newQuantity;
                                db.SaveChanges();
                            }


                            OrderLineItem_Supplier_Mapping dataModel = new OrderLineItem_Supplier_Mapping();
                            dataModel.CreatedDate = DateTime.Now;
                            dataModel.ModifiedDate = DateTime.Now;
                            dataModel.OrderLineItemId = Convert.ToInt32(item.OrderLineItemId);
                            dataModel.Quantity = newQuantity;
                            dataModel.SupplierId = Convert.ToInt32(item.SupplierId);
                            db.OrderLineItem_Supplier_Mapping.Add(dataModel);
                            db.SaveChanges();
                            //var count = db.OrderLineItem_Supplier_Mapping.Count();
                            var id = dataModel.Id;
                            model.OrderSupplierMapId = id;
                            model.Quantity = newQuantity;
                            model.Status = 1;
                            model.SupplierId = Convert.ToInt32(item.SupplierId);
                            model.CreatedDate = DateTime.Now;
                            model.ModifiedDate = DateTime.Now;
                            db.Supplier_Assigned_OrderLineItem.Add(model);
                            db.SaveChanges();
                        }
                    }
                }
                return Json(new { data = addData }, JsonRequestBehavior.AllowGet);
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
            items.ForEach(_ => _.MOQ = qty);
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
            return RedirectToAction("AddSupplierCartonDetail", "Order", new { orderId = ViewBag.OrderId });
        }

        public ActionResult GeneratePerfomaInvoice(int? id)
        {
            ArityEntities dbContext = new ArityEntities();
            List<PerfomaProductList> productList = new List<PerfomaProductList>();
            id = 8;

            var perfoma = (from order in dbContext.Orders
                           join user in dbContext.Users on order.CustomerId equals user.Id
                           join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                           join product in dbContext.Products on lineItem.ProductId equals product.Id
                           where order.Id == id
                           select new PerformaInvoice()
                           {
                               ExporterName = "Exporter Co. Name",
                               ExporterAddress = "Exporter add	",
                               ExporterPhone = "Exporter phone Number",
                               CustomerCompanyName = user.CompanyName,
                               CustomerAddress = user.Address,
                               CustomerGST = user.GSTIN,
                               PINo = "17100601",
                               OrderDate = order.CreatedDate,
                               IECCode = user.IECCode,
                               CustomerName = user.FirstName + " " + user.LastName,
                               CustomerPhone = user.PhoneNumber
                           }).FirstOrDefault();

            productList = (from order in dbContext.Orders
                           join user in dbContext.Users on order.CustomerId equals user.Id
                           join lineItem in dbContext.OrderLineItems on order.Id equals lineItem.OrderId
                           join product in dbContext.Products on lineItem.ProductId equals product.Id
                           select new PerfomaProductList()
                           {
                               Partiular = product.English_Name,
                               Quantity = lineItem.Quantity,
                               Unit = "NOs",
                               UnitPrice = lineItem.DollarSalesPrice,
                               TotalUSD = lineItem.Quantity * lineItem.DollarSalesPrice
                           }).ToList();

            perfoma.ProductList = productList;

            //PerformaInvoice perfoma = new PerformaInvoice()
            //{
            //    ExporterName = "Exporter Co. Name",
            //    ExporterAddress = "Exporter add	",
            //    ExporterPhone = "Exporter phone Number",
            //    CustomerCompanyName = "Customer Company Name",
            //    CustomerAddress = "Cusomer Address",
            //    CustomerGST = "GST123",
            //    PINo = "17100601",
            //    OrderDate = DateTime.Now,
            //    IECCode = "Customer IEC Number",
            //    CustomerName = "Customer contact name",
            //    CustomerPhone = "Customer contact No.",
            //    ProductList = productList
            //};

            //productList.Add(new PerfomaProductList()
            //{
            //    SRNO = 1,
            //    Partiular = "Product Description",
            //    UnitPrice = 12,
            //    Unit = "NOs",
            //    Quantity = 10,
            //    TotalUSD = 120,
            //});

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

                worksheet.Range["A2"].Text = perfoma.ExporterAddress;
                worksheet.Range["$A$2:$F$2"].Merge();

                worksheet.Range["A3"].Text = perfoma.ExporterPhone;
                worksheet.Range["$A$3:$F$3"].Merge();

                worksheet.Range["A4"].Text = string.Empty;
                worksheet.Range["$A$4:$F$4"].Merge();

                worksheet.Range["A5"].Text = "PERFORMA INVOICE";
                worksheet.Range["$A$5:$F$5"].Merge();

                worksheet.Range["A6"].Text = "Customer Co. name";
                worksheet.Range["$A$6:$B$6"].Merge();

                worksheet.Range["C6"].Text = "Pi No.";

                worksheet.Range["D6"].Text = perfoma.PINo;
                worksheet.Range["$D$6:$F$6"].Merge();

                worksheet.Range["A7"].Text = "Customer Add";
                worksheet.Range["$A$7:$B$7"].Merge();

                worksheet.Range["C7"].Text = "Date";

                worksheet.Range["D7"].Text = perfoma.OrderDate.ToString();
                worksheet.Range["$D$7:$F$7"].Merge();

                worksheet.Range["A8"].Text = "GST Number";
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

                worksheet.Range["C9"].Text = "Contact No.";

                worksheet.Range["D9"].Text = perfoma.CustomerCompanyName;
                worksheet.Range["$D$9:$F$9"].Merge();

                worksheet.Range["A11"].Text = "Sr.No.";
                worksheet.Range["B11"].Text = "Perticulates";
                worksheet.Range["C11"].Text = "UP USD";
                worksheet.Range["D11"].Text = "Unit";
                worksheet.Range["E11"].Text = "Qty";
                worksheet.Range["F11"].Text = "Total USD";

                int i = 1;
                int rownum = 12;
                foreach (var item in perfoma.ProductList)
                {
                    worksheet.Range["A" + rownum + ""].Text = i.ToString();
                    worksheet.Range["B" + rownum + ""].Text = item.Partiular;
                    worksheet.Range["C" + rownum + ""].Text = item.UnitPrice.ToString();
                    worksheet.Range["D" + rownum + ""].Text = item.Unit.ToString();
                    worksheet.Range["E" + rownum + ""].Text = item.Quantity.ToString();
                    worksheet.Range["F" + rownum + ""].Text = (item.UnitPrice * item.Quantity).ToString();
                    i++;
                    rownum++;
                }

                //Save the workbook to disk in xlsx format.
                workbook.SaveAs(@"C:\excel\Sampldfsddsddse.xlsx", HttpContext.ApplicationInstance.Response, ExcelDownloadType.Open);
            }

            return View();
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