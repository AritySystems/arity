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

namespace AritySystems.Controllers
{
    [AllowAnonymous]
    public class ProductController : Controller
    {
        public ActionResult Create(int? Id)
        {
            try
            {
                Product product = new Product();
                ArityEntities dataContext = new ArityEntities();
                product = dataContext.Products.Where(x => x.Id == Id).FirstOrDefault();
                ViewBag.productList = product == null ? new SelectList(dataContext.Products.Where(x => x.Parent_Id == 0).ToList(), "Id", "English_Name") : new SelectList(dataContext.Products.Where(x => x.Parent_Id == 0).ToList(), "Id", "English_Name", product.Parent_Id ?? 0);
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
                existingProduct.Parent_Id = product.Parent_Id == null ? existingProduct.Parent_Id : product.Parent_Id;
                existingProduct.Description = product.Description;
                existingProduct.ModifiedDate = DateTime.Now;
            }
            else
            {
                if (product.Parent_Id == null) product.Parent_Id = 0;
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

            var productList = (from product in dataContext.Products.ToList().Where(_ => _.Parent_Id == 0 && _.IsActive != null && Convert.ToBoolean(_.IsActive))
                               select new
                               {
                                   Id = product.Id,
                                   Chinese_Name = product.Chinese_Name,
                                   English_Name = product.English_Name,
                                   Quantity = product.Quantity,
                                   Dollar_Price = product.Dollar_Price,
                                   RMB_Price = product.RMB_Price,
                                   Unit = product.Unit,
                                   Description = product.Description,
                                   ModifiedDate = product.ModifiedDate.GetValueOrDefault().ToString("MM/dd/yyyy h:m tt"),
                                   ChildProducts = (from subProcduct in dataContext.Products.ToList().Where(_ => _.Parent_Id == product.Id && _.IsActive != null && Convert.ToBoolean(_.IsActive))
                                                    select new
                                                    {
                                                        Id = subProcduct.Id,
                                                        Chinese_Name = subProcduct.Chinese_Name,
                                                        English_Name = subProcduct.English_Name,
                                                        Quantity = subProcduct.Quantity,
                                                        Dollar_Price = subProcduct.Dollar_Price,
                                                        RMB_Price = subProcduct.RMB_Price,
                                                        Unit = subProcduct.Unit,
                                                        Description = subProcduct.Description,
                                                        ModifiedDate = subProcduct.ModifiedDate.GetValueOrDefault().ToString("MM/dd/yyyy h:m tt")
                                                    }).ToList()
                               }).ToList();

            return Json(new { data = productList }, JsonRequestBehavior.AllowGet);
            ///return View(productList,JsonRequestBehavior.AllowGet);
        }

        public ActionResult Delete(int? id)
        {
            ArityEntities dataContext = new ArityEntities();
            id = id ?? 0;
            var product = dataContext.Products.Where(x => x.Id == id).FirstOrDefault();
            if(product != null)
            {
                product.IsActive = false;
                dataContext.SaveChanges();
            }

            return RedirectToAction("List");
        }
    }
}