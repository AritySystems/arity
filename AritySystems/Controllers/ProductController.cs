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
using AritySystems.Common;
using System.Collections.Generic;

namespace AritySystems.Controllers
{
    [Authorize]
    public class ProductController : Controller
    {
        public ActionResult Create(int? Id)
        {
            try
            {
                Product product = new Product();
                ArityEntities dataContext = new ArityEntities();
                int[] parentIds = null;
                int[] supplierIds = null;
                product = dataContext.Products.Where(x => x.Id == Id).FirstOrDefault();
                if (product != null)
                {
                    parentIds = product.ParentIds != null ? Array.ConvertAll(product.ParentIds.Split(','), int.Parse) : null;
                    supplierIds = product.Suppliers != null ? Array.ConvertAll(product.Suppliers.Split(','), int.Parse) : null;
                }
                ViewBag.productList = product == null ? new MultiSelectList(dataContext.Products.Where(x => x.Parent_Id == 0).ToList(), "Id", "English_Name") : new MultiSelectList(dataContext.Products.Where(x => x.Parent_Id == 0).ToList(), "Id", "English_Name", parentIds);
                var suppliers = dataContext.Users.Where(x => x.UserType == 5).Select(s => new
                {
                    Id = s.Id,
                    SupplierName = s.FirstName + " " + s.LastName
                }).ToList();
                ViewBag.supplierList = product == null ? new MultiSelectList(suppliers, "Id", "SupplierName") : new MultiSelectList(suppliers, "Id", "SupplierName", supplierIds); //: new SelectList(dataContext.Products.Where(x => x.Parent_Id == 0).ToList(), "Id", "English_Name", product.Parent_Id);
                return View(product);
            }

            catch (Exception ex)
            {
                var exception = ex;
                return View();
            }
        }

        [HttpPost]
        public ActionResult Create(Product product)
        {
            string productParentIds = string.Empty;
            string suppliers = string.Empty;
            int parent = 0;
            if (product.ParentIdsArray != null)
            {
                productParentIds = string.Join(",", product.ParentIdsArray);
                parent = 1;
            }
            if (product.Suppliers != null)
            {
                suppliers = string.Join(",", product.Suppliers);
            }

            ArityEntities dataContext = new ArityEntities();
            if (product != null && product.Id > 0)
            {
                var existingProduct = dataContext.Products.Where(_ => _.Id == product.Id).FirstOrDefault();
                existingProduct.Chinese_Name = product.Chinese_Name;
                existingProduct.English_Name = product.English_Name;
                existingProduct.Quantity = product.Quantity;
                existingProduct.Unit = product.Unit;
                existingProduct.Dollar_Price = product.Dollar_Price;
                existingProduct.RMB_Price = product.RMB_Price;
                existingProduct.Parent_Id = parent;
                existingProduct.Description = product.Description;
                existingProduct.ModifiedDate = DateTime.Now;
                existingProduct.ParentIds = productParentIds;
            }
            else
            {
                product.Parent_Id = parent;
                product.ParentIds = productParentIds;
                product.CreatedDate = DateTime.Now;
                product.ModifiedDate = DateTime.Now;
                dataContext.Products.Add(product);
            }
            dataContext.SaveChanges();
            return RedirectToAction("list", "product");
        }

        public ActionResult List()
        {
            return View();
        }

        [HttpGet]
        public ActionResult ProductList()
        {
            ArityEntities dataContext = new ArityEntities();

            var productList = (from product in dataContext.Products.ToList().Where(_ => _.Parent_Id == 0 && Convert.ToBoolean(_.IsActive))
                               select new
                               {
                                   Id = product.Id,
                                   Chinese_Name = product.Chinese_Name,
                                   English_Name = product.English_Name,
                                   Quantity = product.Quantity,
                                   Dollar_Price = product.Dollar_Price,
                                   RMB_Price = product.RMB_Price,
                                   Unit = getEnumValue(Convert.ToInt32(product.Unit)),
                                   Description = product.Description,
                                   ModifiedDate = product.ModifiedDate.GetValueOrDefault().ToString("MM/dd/yyyy h:m tt"),
                                   ChildProducts = GetChildProducts(product.Id)
                               }).ToList();

            return Json(new { data = productList }, JsonRequestBehavior.AllowGet);
            ///return View(productList,JsonRequestBehavior.AllowGet);
        }

        public ActionResult Delete(int? id)
        {
            ArityEntities dataContext = new ArityEntities();
            id = id ?? 0;
            var product = dataContext.Products.Where(x => x.Id == id).FirstOrDefault();
            if (product != null)
            {
                product.IsActive = false;
                dataContext.SaveChanges();
            }

            return RedirectToAction("List");
        }

        public string getEnumValue(int value)
        {
            Common.EnumHelpers.Units unitValue = (Common.EnumHelpers.Units)value;
            return unitValue.ToString();
        }

        public string getEnumValue(string value)
        {
            int intValue = Convert.ToInt32(value);
            Common.EnumHelpers.Units unitValue = (Common.EnumHelpers.Units)intValue;
            return unitValue.ToString();
        }

        private List<ProductDetails> GetChildProducts(int parentId)
        {
            ArityEntities dataContext = new ArityEntities();
            List<ProductDetails> childProductList = new List<ProductDetails>();

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
                                           select new ProductDetails()
                                           {
                                               Id = childProduct.Id,
                                               Chinese_Name = childProduct.Chinese_Name,
                                               Description = childProduct.Description,
                                               Dollar_Price = childProduct.Dollar_Price,
                                               ModifiedDate = childProduct.ModifiedDate,
                                               English_Name = childProduct.English_Name,
                                               Quantity = childProduct.Quantity,
                                               RMB_Price = childProduct.RMB_Price,
                                               Unit = getEnumValue(childProduct.Unit),
                                           }).FirstOrDefault();



                if (childProductDetails != null)
                {
                    childProductList.Add(childProductDetails);
                }

            }

            return childProductList;
        }

    }
}