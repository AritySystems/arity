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
                    parentIds = !string.IsNullOrWhiteSpace(product.ParentIds) ? Array.ConvertAll(product.ParentIds.Split(','), int.Parse) : null;
                    supplierIds = !string.IsNullOrWhiteSpace(product.Suppliers) ? Array.ConvertAll(product.Suppliers.Split(','), int.Parse) : null;
                }
                ViewBag.productList = product == null ? new MultiSelectList(dataContext.Products.Where(x => x.Parent_Id == 0 && x.IsActive == true).ToList(), "Id", "English_Name") : new MultiSelectList(dataContext.Products.Where(x => x.Parent_Id == 0).ToList(), "Id", "English_Name", parentIds);
                var suppliers = dataContext.Users.Where(x => x.UserType == 5 && x.IsActive == true && x.CompanyName != "").Select(s => new
                {
                    Id = s.Id,
                    SupplierName = s.CompanyName
                }).ToList();
                ViewBag.supplierList = product == null || supplierIds == null || supplierIds.Count() == 0 ? new MultiSelectList(suppliers, "Id", "SupplierName") : new MultiSelectList(suppliers, "Id", "SupplierName", supplierIds);

                ProductDetails productModel = ConvertModeltoDTO(product);

                return View(productModel);
            }

            catch (Exception ex)
            {
                var exception = ex;
                return View();
            }
        }

        [HttpPost]
        public ActionResult Create(ProductDetails product)
        {
            string productParentIds = string.Empty;
            string suppliers = string.Empty;
            int parent = 0;
            if (product.ParentIdsArray != null)
            {
                productParentIds = string.Join(",", product.ParentIdsArray);
                parent = 1;
            }
            if (product.SuppliersArray != null)
            {
                suppliers = string.Join(",", product.SuppliersArray);
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
                existingProduct.Suppliers = suppliers;
                existingProduct.IsActive = true;
            }
            else
            {
                product.Parent_Id = parent;
                product.ParentIds = productParentIds;
                product.ModifiedDate = DateTime.Now;
                product.CreatedDate = DateTime.Now;
                product.IsActive = true;
                dataContext.Products.Add(ConvertDTOtoModel(product));
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
                              where data.ParentIds != null && data.ParentIds != string.Empty && data.IsActive
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

                var childProductDetails = (from childProduct in dataContext.Products.ToList().Where(x => ids.Contains(parentId) && x.Id == data.Id )
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


        private ProductDetails ConvertModeltoDTO(Product product)
        {
            return new ProductDetails()
            {
                Id = product.Id,
                Chinese_Name = product.Chinese_Name,
                Description = product.Description,
                Dollar_Price = product.Dollar_Price,
                ModifiedDate = product.ModifiedDate,
                English_Name = product.English_Name,
                Quantity = product.Quantity,
                RMB_Price = product.RMB_Price,
                Unit = getEnumValue(product.Unit),
                BOM = product.BOM?? 0 ,
                Cubic_Meter = product.CBM?? 0,
                Weight = product.Weight?? 0
            };
        }

        private Product ConvertDTOtoModel(ProductDetails product)
        {
            return new Product()
            {
                Id = product.Id,
                Chinese_Name = product.Chinese_Name,
                Description = product.Description,
                Dollar_Price = product.Dollar_Price,
                ModifiedDate = product.ModifiedDate,
                English_Name = product.English_Name,
                Quantity = product.Quantity,
                RMB_Price = product.RMB_Price,
                Unit = product.Unit,
                Suppliers = product.Suppliers,
                ParentIds = product.ParentIds,
                CreatedDate = product.CreatedDate,
                IsActive = product.IsActive,
                Parent_Id = product.Parent_Id,
                BOM = product.BOM,
                CBM = product.Cubic_Meter,
                Weight = product.Weight
            };
        }

    }
}