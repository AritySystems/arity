using System.Linq;
using System.Web.Mvc;
using AritySystems.Models;
using System.Web.Security;
using System.Security.Principal;
using currentContext = AritySystems.Data;
using AritySystems.Data;
using System;
using System.Web;
using System.IO;

namespace AritySystems.Controllers
{
    //[Authorize]
    public class UserController : Controller
    {

        ArityEntities dataContext;

        public UserController()
        {
            dataContext = new ArityEntities();
        }

        [HttpGet]
        public ActionResult Login(string ReturnUrl)
        {
            string returnURL = string.Empty;
            try
            {
                // We do not want to use any existing identity information  
                EnsureLoggedOut();
                // Store the originating URL so we can attach it to a form field  
                returnURL = ReturnUrl;
                return View(returnURL);
            }
            catch
            {
                throw;
            }
        }

        //GET: EnsureLoggedOut  
        private void EnsureLoggedOut()
        {
            // If the request is (still) marked as authenticated we send the user to the logout action  
            if (Request.IsAuthenticated)
                Logout();
        }

        //POST: Logout  
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                // First we clean the authentication ticket like always  
                //required NameSpace: using System.Web.Security;  
                FormsAuthentication.SignOut();

                // Second we clear the principal to ensure the user does not retain any authentication  
                //required NameSpace: using System.Security.Principal;  
                HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);

                Session.Clear();
                System.Web.HttpContext.Current.Session.RemoveAll();

                // Last we redirect to a controller/action that requires authentication to ensure a redirect takes place  
                // this clears the Request.IsAuthenticated flag since this triggers a new request  
                return RedirectToAction("Login", "User");
            }
            catch
            {
                throw;
            }
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel entity)
        {
            try
            {
                using (var db = new ArityEntities())
                {
                    if (!ModelState.IsValid)
                        return View(entity);
                    //check user credentials
                    var userInfo = db.Users.Where(s => s.EmailId == entity.Email.Trim() && s.Password == entity.Password.Trim()).FirstOrDefault();

                    if (userInfo != null)
                    {
                        FormsAuthentication.SetAuthCookie(entity.Email, true);
                        //Set A Unique name in session  
                        Session["Username"] = userInfo.EmailId;
                        Session["UserType"] = userInfo.UserTypes.Select(_ => _.Type).FirstOrDefault();
                       
                            return RedirectToAction("OrderList", "Order", "Order");
                    }
                    else
                    {
                        //Login Fail  
                        TempData["ErrorMSG"] = "Access Denied! Wrong Credential";
                        return View(entity);
                    }
                }
            }
            catch
            {
                throw;
            }
        }

        public ActionResult Create(int? id)
        {
            currentContext.User userModel = new currentContext.User();

            if (id > 0)
            {
                userModel = dataContext.Users.Where(x => x.Id == id).FirstOrDefault();
            }
            return View(userModel);
        }

        [HttpPost]
        public ActionResult Create(currentContext.User user, HttpPostedFileBase logo)
        {
            if (user != null && user.Id > 0)
            {
                var existingProduct = dataContext.Users.Where(_ => _.Id == user.Id).FirstOrDefault();
                existingProduct.FirstName = user.FirstName;
                existingProduct.LastName = user.LastName;
                existingProduct.Prefix = user.Prefix;
                existingProduct.EmailId = user.EmailId;
                existingProduct.PhoneNumber = user.PhoneNumber;
                existingProduct.Address = user.Address;
                existingProduct.GSTIN = user.GSTIN;
                existingProduct.IECCode = user.IECCode;
                existingProduct.UserName = user.UserName;
                existingProduct.Password = user.Password;
                existingProduct.CompanyName = user.CompanyName;
                existingProduct.Logo = user.Logo;
                existingProduct.ModifiedDate = DateTime.Now;
                if (logo != null && logo.ContentLength > 0)
                {
                    if (!Directory.Exists(Server.MapPath("~/Content/img/user")))
                        Directory.CreateDirectory(Server.MapPath("~/Content/img/user"));
                    if (existingProduct.Logo != null && !string.IsNullOrEmpty(existingProduct.Logo) && System.IO.File.Exists(Server.MapPath("~"+ existingProduct.Logo)))
                        System.IO.File.Delete(Server.MapPath("~"+ existingProduct.Logo));
                    string filePath = Path.Combine(Server.MapPath("~/Content/img/user"), user.Id.ToString() + Path.GetExtension(logo.FileName));
                    logo.SaveAs(filePath);
                    existingProduct.Logo= "/Content/img/user/"+ user.Id.ToString() + Path.GetExtension(logo.FileName);
                }
               
            }
            else
            {
                user.CreatedDate = DateTime.Now;
                user.ModifiedDate = DateTime.Now;
                dataContext.Users.Add(user);
                dataContext.SaveChanges();
                var userData = dataContext.Users.Where(_ => _.Id == user.Id).FirstOrDefault();
                if (logo != null && logo.ContentLength > 0)
                {
                    if (!Directory.Exists(Server.MapPath("~/Content/img/user")))
                        Directory.CreateDirectory(Server.MapPath("~/Content/img/user"));
                    if (userData.Logo != null && !string.IsNullOrEmpty(userData.Logo) && System.IO.File.Exists(Server.MapPath("~" + userData.Logo)))
                        System.IO.File.Delete(Server.MapPath("~" + userData.Logo));
                    string filePath = Path.Combine(Server.MapPath("~/Content/img/user"), user.Id.ToString() + Path.GetExtension(logo.FileName));
                    logo.SaveAs(filePath);
                    userData.Logo = "/Content/img/user/" + user.Id.ToString() + Path.GetExtension(logo.FileName);
                }
            }
            dataContext.SaveChanges();
            return RedirectToAction("list", "user");
        }

        public ActionResult List()
        {
            return View();
        }

        [HttpGet]
        public ActionResult UserList()
        {
            try
            {
                var userList = (from user in dataContext.Users.ToList()
                                select new
                                {
                                    Id = user.Id,
                                    Name = user.FirstName + " " + user.LastName,
                                    Prefix = user.Prefix,
                                    EmailId = user.EmailId,
                                    PhoneNumber = user.PhoneNumber,
                                    Address = user.Address,
                                    GSTIN = user.GSTIN,
                                    IECCode = user.IECCode,
                                    UserName = user.UserName,
                                    Password = user.Password,
                                    CompanyName = user.CompanyName
                                }).ToList();

                return Json(new { data = userList }, JsonRequestBehavior.AllowGet);
            }

            catch (Exception ex)
            {
                var data = ex;
            }
            return null;
        }

        public ActionResult Delete(int? id)
        {
            id = id ?? 0;
            var user = dataContext.Users.Where(x => x.Id == id).FirstOrDefault();
            if (user != null)
            {
                // user.IsActive = false;
                dataContext.SaveChanges();
            }

            return RedirectToAction("List");
        }
    }
}