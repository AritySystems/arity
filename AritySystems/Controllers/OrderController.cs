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
            var orders = objDb.Orders.Where(_ => _.Id == id).FirstOrDefault();
            ViewBag.Sales_Person_Id = orders != null && (orders.Sales_Person_Id ?? 0) > 0 ? new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Sales), "Id", "FullName", orders.Sales_Person_Id) : new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Sales), "Id", "FullName");
            ViewBag.ExporterList = orders != null && (orders.ExporterId ?? 0) > 0 ? new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Exporter), "Id", "FullName", orders.ExporterId) : new SelectList(GetUerList((int)Common.EnumHelpers.UserType.Exporter), "Id", "FullName");
            return View(orders);
        }

        private List<UserModel> GetUerList(int Usertype)
        {
            ArityEntities objDb = new ArityEntities();
            return objDb.Users.Where(_ => (_.UserType ?? 0) == Usertype).Select(_ => new UserModel
            {
                Id = _.Id,
                FullName = _.FirstName + " " + _.LastName
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
                    var model = (from b in db.OrderLineItem_Supplier_Mapping
                                 join c in db.Orders on b.OrderId equals c.Id
                                // join d in db.Orders on c.OrderId equals d.Id
                                 where b.OrderId == OrderId && b.Quantity > 0
                                 select new SupplierOrderLineItemModel
                                 {
                                     //Id = b.Id,
                                     //CreatedDate = a.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
                                     //ModifiedDate = a.ModifiedDate.ToString("MM/dd/yyyy h:m tt"),
                                     //OrderSupplierMapId = a.OrderSupplierMapId,
                                     Order_Prefix = c.Prefix,
                                     Quantity = b.Quantity,
                                     Status = c.Status,
                                     SupplierId = b.SupplierId,
                                     SupplierName = b.User.UserName
                                 }).Distinct().ToList();

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
                            int id = 0;
                            var orderId = Convert.ToInt32(item.OrderId);
                            var orderLineItemId = Convert.ToInt32(item.OrderLineItemId);
                            var quantity = Convert.ToDecimal(item.NewQuantity);
                            var oldQuantity = Convert.ToDecimal(item.OldQuantity);
                            var supplierid = Convert.ToInt32(item.SupplierId);
                            //OrderLineItem ActualQuantity = db.OrderLineItems.Where(x => x.Id == orderItemId).FirstOrDefault();
                            if (oldQuantity >= quantity)
                            {
                                var updated = UpdatedQuantity(orderLineItemId);
                                newQuantity = updated - quantity;
                            }
                            if (newQuantity >= 0)
                            {
                                ViewBag.updatedQuantity = newQuantity;
                            }

                            OrderLineItem_Supplier_Mapping dataModeldata = new OrderLineItem_Supplier_Mapping();
                            dataModeldata = db.OrderLineItem_Supplier_Mapping.Where(x => x.OrderId == orderId && x.SupplierId == supplierid).FirstOrDefault();
                            if (dataModeldata == null)
                            {
                                OrderLineItem_Supplier_Mapping dataModel = new OrderLineItem_Supplier_Mapping();
                                dataModel.CreatedDate = DateTime.Now;
                                dataModel.ModifiedDate = DateTime.Now;
                                dataModel.OrderId = Convert.ToInt32(item.OrderId);
                                dataModel.Quantity = db.OrderLineItems.Where(x => x.OrderId == orderId).Select(x => x.Quantity).Sum();
                                dataModel.SupplierId = Convert.ToInt32(item.SupplierId);
                                id = dataModel.Id;
                                db.OrderLineItem_Supplier_Mapping.Add(dataModel);
                            }
                            else {
                                dataModeldata.ModifiedDate = DateTime.Now;
                                id = dataModeldata.Id;
                                dataModeldata.Quantity = db.OrderLineItems.Where(x => x.OrderId == orderId).Select(x => x.Quantity).Sum(); 
                            }
                            db.SaveChanges();

                            //var id = dataModeldata.Id;
                            model.OrderSupplierMapId = id;
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

       
        //public SupplierHelper SupplierAssigneddataforSupplierOrders(int orderlineitemid, int loggeduser)
        //{
        //    var objDb = new ArityEntities();
        //    var data = (from supplierorder in objDb.OrderLineItem_Supplier_Mapping.ToList()
        //                join supplieritem in objDb.Supplier_Assigned_OrderLineItem.ToList() on supplierorder.Id equals supplieritem.OrderSupplierMapId
        //                where supplierorder.OrderLineItemId == orderlineitemid && supplieritem.SupplierId == loggeduser
        //                select new SupplierHelper
        //                {
        //                    CreatedOn = supplieritem.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
        //                    Quantity = supplieritem.Quantity,
        //                    Status = supplieritem.Status
        //                }).FirstOrDefault();
        //    return data;
        //}
        //public class SupplierHelper
        //{
        //    public decimal Quantity { get; set; }
        //    public string CreatedOn { get; set; }
        //    public int Status { get; set; }
        //}
        

        public ActionResult GeneratePerfomaInvoice(int? id)
        {
            ArityEntities dbContext = new ArityEntities();
            List<PerfomaProductList> productList = new List<PerfomaProductList>();
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

            productList = (from order in dbContext.Orders
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

            perfoma.ProductList = productList;


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

                worksheet.Range["D7"].Text = perfoma.OrderDate.ToString();
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

                worksheet.Range["A" + rownum + ""].Text = "Total";
                worksheet.Range["$A$" + rownum + ":$E$" + rownum].Merge();

                worksheet.Range["F" + rownum + ""].Text = "$" + totalItems.ToString();
                worksheet.Range["G" + rownum + ""].Text = "¥" + totalRMB.ToString();

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
                    PerfomaInvoiceReferece = perfoma.PINo
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
                        RMB_ProductPrice = item.RMBUnitPrice

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
                var ids = data.suppliers.Split(new[] { ',' })
                              .Select(x => int.Parse(x))
                              .ToArray();

                suppliers = (from u in dataContext.Users.ToList().Where(x => ids.Contains(x.Id) && x.UserType == (int)Common.EnumHelpers.UserType.Supplier)
                             select new SelectListItem
                             {
                                 Text = u.Id.ToString(),
                                 Value = u.CompanyName
                             }).ToList();

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

        ///// <summary>
        ///// Get supplier order listing
        ///// </summary>
        ///// <returns></returns>
        public JsonResult GetSupplierOrderList()
        {
            var loggedInId = (int)Session["UserId"];
            var objDb = new ArityEntities();
            var orders = (from supplierorder in objDb.OrderLineItem_Supplier_Mapping.ToList()
                          join supplieritem in objDb.Supplier_Assigned_OrderLineItem.ToList() on supplierorder.Id equals supplieritem.OrderSupplierMapId
                          where supplieritem.SupplierId == loggedInId && supplierorder.OrderId != null
                          select new
                          {
                              SupplierOrderId = supplierorder.OrderId,
                              Prefix = supplierorder.Order.Prefix,
                              OrderId = supplierorder.OrderId,
                              CreatedOn = supplieritem.CreatedDate.ToString("MM/dd/yyyy h:m tt"),
                              Quantity = supplieritem.Quantity,
                              OrderQuantity = supplierorder.Quantity,
                              //DollerSalesTotal = 0,
                              RmbSalesTotal = supplieritem.OrderLineItem1.RMBSalesPrice,
                              Status = supplieritem.Status
                          }).ToList();
            var final = orders.GroupBy(x => x.OrderId).Select(x => new {
                SupplierOrderId = x.Key,
                Prefix = x.Select(p=>p.Prefix).FirstOrDefault(),
                CreatedOn = x.Select(p=>p.CreatedOn).FirstOrDefault(),
                Quantity = x.Select(p=>p.OrderQuantity).FirstOrDefault(),
                RmbSalesTotal = x.Sum(p => p.Quantity * p.RmbSalesTotal),
                Status = x.Select(p=>p.Status).FirstOrDefault()
            }).ToList();

            return Json(new { data = final }, JsonRequestBehavior.AllowGet);
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
                                 join b in db.OrderLineItem_Supplier_Mapping on a.SupplierAssignedMapId equals b.OrderId
                                 join c in db.OrderLineItems on b.OrderId equals c.OrderId
                                 join d in db.Orders on b.OrderId equals d.Id
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
            SupplierCartoon carton = new SupplierCartoon();
            ArityEntities dbContext = new ArityEntities();
            ViewBag.OrderId = orderId;
            ViewBag.OrderName = dbContext.Orders.Where(x => x.Id == orderId).Select(x => x.Prefix).FirstOrDefault();
            ViewBag.OrderDate = (from b in dbContext.OrderLineItem_Supplier_Mapping
                                 join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
                                 where b.OrderId == orderId && b.OrderId != null
                                 select c.CreatedDate).FirstOrDefault();

            var Status = (from b in dbContext.OrderLineItem_Supplier_Mapping
                          join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
                          where b.OrderId == orderId && b.OrderId != null
                          select c.Status).FirstOrDefault();
            ViewBag.Status = ((AritySystems.Common.EnumHelpers.OrderStatus)Status).ToString();
            var ordersLineItems = (from b in dbContext.OrderLineItem_Supplier_Mapping
                                   join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
                                   where b.OrderId == orderId && b.OrderId != null
                                   select new { c.Id, c.OrderLineItem1.Product.English_Name }).ToList();
            ViewBag.OrderLineItems = new SelectList(ordersLineItems, "Id", "English_Name");
            carton.PcsPerCartoon = (from b in dbContext.OrderLineItem_Supplier_Mapping
                                    join c in dbContext.Supplier_Assigned_OrderLineItem on b.Id equals c.OrderSupplierMapId
                                    where b.OrderId == orderId && b.OrderId != null
                                    select c.Quantity).FirstOrDefault();
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
                                               ParentIds = childProduct.ParentIds
                                           }).FirstOrDefault();



                if (childProductDetails != null)
                {
                    childProductList.Add(childProductDetails);
                }

            }

            return childProductList;
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
            ViewBag.OrderCI = new SelectList(dbContext.CommercialInvoices.Where(_ => _.OrderId == id).ToList(), "Id", "CommercialInvoiceReferece");
            return View();
        }

        [HttpPost]
        public ActionResult AddPaymentForOrder(int id, int ciId, decimal dAmmount, decimal rAmmount)
        {
            var dbContext = new ArityEntities();
            try
            {
                dbContext.Accounts.Add(new Data.Account() { CommercialId = ciId, CreatedDate = DateTime.Now, OrderId = id, Dollar_Price = dAmmount, RMB_Price = rAmmount });
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
                            join com in dbContext.CommercialInvoices on _.CommercialId equals com.Id
                            join order in dbContext.Orders on _.OrderId equals order.Id
                            join user in dbContext.Users on order.User.Id equals user.Id
                            where _.OrderId == id
                            select new
                            {
                                CIId = _.Id,
                                ShippingMark = order.Prefix,
                                CI = com.CommercialInvoiceReferece,
                                DollerPrice = _.Dollar_Price.HasValue ? Math.Round(Convert.ToDouble(_.Dollar_Price.Value),2) : 0.00,
                                RMBPrice = _.RMB_Price.HasValue ? Math.Round(Convert.ToDouble(_.RMB_Price.Value),2): 0.00,
                                Date = _.CreatedDate.HasValue ? _.CreatedDate.Value.ToString("MM/dd/yyyy h:mm") : null
                            }).ToList();
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
                                CreatedDate = order.CreatedDate.ToString("MM/dd/yyyy h:mm"),
                                TotalDollerPrice = order.Accounts.Sum(_=>_.Dollar_Price),
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
    }
}