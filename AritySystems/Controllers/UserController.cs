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
using AritySystems.Models;

namespace AritySystems.Controllers
{
    [AllowAnonymous]
    public class UserController : Controller
    {

        ArityEntities dataContext;

        public UserController()
        {
            dataContext = new ArityEntities();
        }
        [AllowAnonymous]
        [HttpGet]
        public ActionResult Login(string ReturnUrl)
        {
            string returnURL = string.Empty;
            try
            {
                if (Request.Cookies["UserType"] != null)
                {
                    var type = Convert.ToInt32(Request.Cookies["UserType"].Value);
                    if (type == (int)AritySystems.Common.EnumHelpers.UserType.Admin)
                        return RedirectToAction("orders", "order");
                    else if (type == (int)AritySystems.Common.EnumHelpers.UserType.Supplier)
                        return RedirectToAction("suppliersorder", "order");
                    return RedirectToAction("orderlist", "order");
                }
                // We do not want to use any existing identity information  
                //EnsureLoggedOut();
                // Store the originating URL so we can attach it to a form field  
                LoginModel objLogin = new LoginModel();
                objLogin.ReturnURL = ReturnUrl;
                return View(objLogin);
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
            // if (Request.IsAuthenticated)
            Logout();
        }

        //POST: Logout  
        //[HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            try
            {
                //// First we clean the authentication ticket like always  
                ////required NameSpace: using System.Web.Security;  
                //FormsAuthentication.SignOut();

                //// Second we clear the principal to ensure the user does not retain any authentication  
                ////required NameSpace: using System.Security.Principal;  
                //HttpContext.User = new GenericPrincipal(new GenericIdentity(string.Empty), null);

                //Session.Clear();
                //System.Web.HttpContext.Current.Session.RemoveAll();
                if (Request.Cookies["UserId"] != null)
                    Response.Cookies["UserId"].Expires = DateTime.Now.AddDays(-1);
                if (Request.Cookies["UserType"] != null)
                    Response.Cookies["UserType"].Expires = DateTime.Now.AddDays(-1);
                if (Request.Cookies["Username"] != null)
                    Response.Cookies["Username"].Expires = DateTime.Now.AddDays(-1);
                // Last we redirect to a controller/action that requires authentication to ensure a redirect takes place  
                // this clears the Request.IsAuthenticated flag since this triggers a new request  
                return RedirectToAction("Login", "User");
            }
            catch
            {
                throw;
            }
        }


        [AllowAnonymous]
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

                    if (userInfo != null && userInfo.IsActive == true)
                    {
                        FormsAuthentication.SetAuthCookie(entity.Email, true);
                        //Set A Unique name in cookie  
                        var Username = new HttpCookie("Username");
                        Username.Value = userInfo.EmailId;
                        Username.Expires = DateTime.Now.AddDays(7d);
                        var UserType = new HttpCookie("UserType");
                        UserType.Value = userInfo.UserType.ToString();
                        UserType.Expires = DateTime.Now.AddDays(7d);
                        var UserId = new HttpCookie("UserId");
                        UserId.Value = userInfo.Id.ToString();
                        UserId.Expires = DateTime.Now.AddDays(7d);
                        Response.Cookies.Add(Username);
                        Response.Cookies.Add(UserType);
                        Response.Cookies.Add(UserId);
                        //Session["Username"] = userInfo.EmailId;
                        //Session["UserType"] = userInfo.UserType;
                        //Session["UserId"] = userInfo.Id;
                        if (!string.IsNullOrEmpty(entity.ReturnURL))
                            return RedirectToAction(entity.ReturnURL);
                        if (userInfo.UserType == (int)AritySystems.Common.EnumHelpers.UserType.Admin)
                            return RedirectToAction("orders", "order");
                        else if (userInfo.UserType == (int)AritySystems.Common.EnumHelpers.UserType.Supplier)
                            return RedirectToAction("suppliersorder", "order");
                        return RedirectToAction("orderlist", "order");
                    }
                    else
                    {
                        //Login Fail  
                        TempData["ErrorMSG"] = "Access Denied! Wrong Credential or User is not Active";
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
            currentContext.User user = new currentContext.User();
            UserModel userModel = new UserModel();

            if (id > 0)
            {
                user = dataContext.Users.Where(x => x.Id == id).FirstOrDefault();

                if (user != null)
                {
                    ViewBag.userId = user.Id;
                    ViewBag.userType = user.UserType;
                    userModel = ConvertDTOtoModel(user);
                }
            }
            return View(userModel);
        }

        [HttpPost]
        public ActionResult Create(UserModel user, HttpPostedFileBase logo)
        {
            var data = dataContext.Users.Where(x => x.EmailId == user.EmailId || x.Prefix == user.Prefix).FirstOrDefault();

            if (data != null && data.Id != user.Id)
            {
                if (data.EmailId == user.EmailId)
                    TempData["ErrorMSG"] = "Same Email Exist, Please choose another one";
                else if (data.Prefix == user.Prefix)
                    TempData["ErrorMSG"] = "Same Prefix/Shipping Mark Exist, Please choose another one";
                return View(user);
            }

            else if (user != null && user.Id > 0)
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
                existingProduct.ModifiedDate = DateTime.Now;
                existingProduct.IsActive = true;
                if (logo != null && logo.ContentLength > 0)
                {
                    if (!Directory.Exists(Server.MapPath("~/Content/img/user")))
                        Directory.CreateDirectory(Server.MapPath("~/Content/img/user"));
                    if (existingProduct.Logo != null && !string.IsNullOrEmpty(existingProduct.Logo) && System.IO.File.Exists(Server.MapPath("~" + existingProduct.Logo)))
                        System.IO.File.Delete(Server.MapPath("~" + existingProduct.Logo));
                    string filePath = Path.Combine(Server.MapPath("~/Content/img/user"), user.Id.ToString() + Path.GetExtension(logo.FileName));
                    logo.SaveAs(filePath);
                    existingProduct.Logo = "/Content/img/user/" + user.Id.ToString() + Path.GetExtension(logo.FileName);
                }

            }
            else
            {


                user.CreatedDate = DateTime.Now;
                user.ModifiedDate = DateTime.Now;
                user.IsActive = true;
                Data.User userModel = ConvertModeltoDTO(user);
                dataContext.Users.Add(userModel);
                dataContext.SaveChanges();
                var userData = dataContext.Users.Where(_ => _.Id == userModel.Id).FirstOrDefault();
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
                var userList = (from user in dataContext.Users.Where(x => x.IsActive == true).ToList()
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
                user.IsActive = false;
                dataContext.SaveChanges();
            }

            return RedirectToAction("List");
        }

        private User ConvertModeltoDTO(UserModel user)
        {
            return new User
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Prefix = user.Prefix,
                EmailId = user.EmailId,
                PhoneNumber = user.PhoneNumber,
                Address = user.EmailId,
                IECCode = user.IECCode,
                GSTIN = user.GSTIN,
                UserName = user.UserName,
                Password = user.Password,
                CompanyName = user.CompanyName,
                UserType = user.UserType,
                Logo = user.Logo,
                IsActive = user.IsActive
            };
        }

        private UserModel ConvertDTOtoModel(currentContext.User user)
        {
            return new UserModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Prefix = user.Prefix,
                EmailId = user.EmailId,
                PhoneNumber = user.PhoneNumber,
                Address = user.EmailId,
                IECCode = user.IECCode,
                GSTIN = user.GSTIN,
                UserName = user.UserName,
                Password = user.Password,
                CompanyName = user.CompanyName,
                UserType = user.UserType,
                Logo = user.Logo,
                IsActive = user.IsActive

            };
        }
    }
}