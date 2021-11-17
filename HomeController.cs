using HUDSON.BAL;
using HUDSON.COMMON;
using HUDSON.ENTITY;
using HUDSON.WEB.DocuSign;
using HUDSON.WEB.SecurityCheck;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Web;
using System.Web.Configuration;
using System.Web.Mvc;
using System.Web.Script.Serialization;

namespace HUDSON.WEB.Controllers
{
    public class HomeController : Controller
    {
        log4net.ILog Logger = null;
        private static Random random = new Random();
        public HomeController()
        {
            Logger = log4net.LogManager.GetLogger(typeof(HomeController));
        }

        public ActionResult Index(string id = "0", string eId = "")
        {
            string userSiteUrl = System.Configuration.ConfigurationManager.AppSettings["MyUserSite"].ToString();
            try
            {
                string [] customerID  = id.Split(',');
                for (int i = 0; i < customerID.Length; i++)
                {
                    customerID[i] = customerID[i].Trim();
                }
                var status = Request.QueryString["event"];
                if (status != null && status == "signing_complete")
                {
                    var title = Request.QueryString["title"];
                    EmbeddedSigning embeddedSigning = new EmbeddedSigning();
                    RegisterBAL registerBAL = new RegisterBAL();

                    var bol = embeddedSigning.DownLoadSignedDocument(eId, customerID[0]);
                    if (bol)
                    {
                        var Data = registerBAL.GetDocuments_EmailId(eId);
                        if (Data.Tables.Count > 0)
                        {
                            if (Data.Tables[0].Rows.Count > 0)
                            {
                                List<Attachment> attch = new List<Attachment>();
                                for (int i = 0; i < Data.Tables[0].Rows.Count; i++)
                                {
                                    attch.Add(new Attachment(Server.MapPath("~/AppDocs/") + Data.Tables[0].Rows[i]["Document_Name"].ToString()));
                                }
                                StringBuilder sb = new StringBuilder();
                                sb.Append("<p>Hello,</p>");
                                sb.Append("<p>You have received document(s) for Docu-sign.");
                                sb.Append("Please check the attachemnts for the same.");
                                sb.Append("<p>Thank You.</p>");
                                sb.Append("<p>Sincerely,</p>");
                                sb.Append("<p>Team Loan Mantra</p>");
                                new MailHelper().Send(Data.Tables[0].Rows[0]["Email_Id"].ToString(), sb.ToString(), "Signed Document from Docu-sign.", attch, "Blue generated Documents | Signed Document from Docu-sign");
                            }
                            if (Data.Tables[1].Rows.Count > 0)
                            {
                                MultipleEmbeddedSigning multipleEmbeddedSigning = new MultipleEmbeddedSigning();
                                //var data = multipleEmbeddedSigning.PdfSigningMultipleDocumentForSpouse(Data.Tables[1]);
                                SendEmailToSpouseForDocuSign(Data.Tables[1]);

                                //return Redirect(System.Configuration.ConfigurationManager.AppSettings["sitUrl"].ToString());
                            }

                            if (Data.Tables.Count > 2 && Data.Tables[2].Rows.Count > 0)
                            {
                                DataTable dt = Data.Tables[2];
                                SendEmailToOwnerForDocusign(dt);
                            }
                        }
                    }
                    // return Redirect(userSiteUrl);
                    // return Redirect("http://www.users.loanmantra.com/");
                    //return Redirect("http://localhost:34915/");
                }

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                

            }
            return Redirect(userSiteUrl);
            // return Redirect("http://www.users.loanmantra.com/");
            // return Redirect("http://localhost:34915/");
        }

        public ActionResult UpdateBorrowerEmailAddressLink(string email, int CustomerID)
        {
            try
            {
                string decryp_email = StringCipher.Base64Decode(email);
                DataSet result = new RegisterBAL().CheckBusinessEmailChangeStatus(CustomerID);
                if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    if (result.Tables[0].Rows[0]["VarificationForBusinessEmailChange_Status"].ToString() == "False")
                    {
                        LoanApplicationBAL obj = new LoanApplicationBAL();
                        int UpdateResult = obj.UpdateBusinessEmailInDB(CustomerID, decryp_email);

                        if (UpdateResult > 0)
                        {
                            return View("EmailVerificationSucess");
                        }
                        else
                        {
                            return View("EmailVerificationError");
                        }
                    }
                    else
                    {
                        return View("LinkExpired_MailUpdate");
                    }
                }
                else
                {
                    return View("LinkExpired_MailUpdate");
                }

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            return View();
        }



        [HttpPost]
        public ActionResult CheckUniqueEmailSpouse_ForBusinessOwners(int Owner_ID, string email_id)
        {
            bool ifuser = false;
            try
            {
                LoanApplicationBAL objLoanApplicationBAL = new LoanApplicationBAL();
                ifuser = objLoanApplicationBAL.UnniqueEmailExist_bowner(Owner_ID, email_id);
                return Json(ifuser.ToString());
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                

                return Json(ifuser.ToString());
            }

        }


        [HttpPost]
        public ActionResult CheckUniqueEmail_forLender(string email_id)
        {
            bool ifuser = false;
            try
            {
                LoanApplicationBAL objLoanApplicationBAL = new LoanApplicationBAL();
                ifuser = objLoanApplicationBAL.UnniqueEmailExist_Lender(email_id);
                return Json(ifuser.ToString());
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                

                return Json(ifuser.ToString());
            }

        }



        public ActionResult LmLogs()
        {
            string emailId = new RegisterBAL().GetEmailIDByUserID(Convert.ToInt32(Session["UserId"]));
            ViewBag.emailId = emailId;
            return View();
        }

        public ActionResult EmailVerificationError()
        {
            return View();
        }

        public ActionResult BusinessEmailChangeLinkExpired()
        {
            return View();
        }

        public ActionResult EmailVerificationSucess()
        {
            return View();
        }

        public ActionResult LinkExpired_MailUpdate()
        {
            return View();
        }

        public ActionResult LenderLogin()
        {
            return View();
        }

        public ActionResult DocuSign_LinkExpired()
        {
            return View();
        }

        [HttpPost]
        [ActionName("LmLogs")]
        public ActionResult LmLogs_Post(string dateFrom, string dateTo)
        {
            try
            {

                List<LmLogs> objLMLogs = new LoanApplicationBAL().GetLMLogs(dateFrom, dateTo);
                return PartialView("~/Views/Shared/_LmLogsPartial.cshtml", objLMLogs);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
                return PartialView("~/Views/Shared/_LmLogsPartial.cshtml");
            }
        }


        public ActionResult LinkExpired()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        #region LogOut Added by: Harpreet Singh Dated:22-11-2019
        public ActionResult LogOut()
        {
            try
            {
                Session.Clear();
                Session.RemoveAll();
                Session.Abandon();
                ModelState.AddModelError("", "You are successfully logged off, please log in here.");
                try
                {
                    new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserID"]), "out", "from the system", "logged", "Users", "Users");
                }
                catch { }
                return View("Login");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }



        #endregion


        #region LogOut Added by: Harpreet Singh Dated:02-01-2021
        public ActionResult LenderLogOut()
        {
            try
            {
                Session.Clear();
                Session.RemoveAll();
                Session.Abandon();
                ModelState.AddModelError("", "You are successfully logged off, please log in here.");
                try
                {
                    new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserID"]), "out", "from the system", "logged", "Users", "Users");
                }
                catch { }
                return View("LenderLogin");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }



        #endregion

        public ActionResult SpouseIndex(int id = 0, string eId = "")
        {
            try
            {
                var status = Request.QueryString["event"];
                if (status != null && status == "signing_complete")
                {
                    var title = Request.QueryString["title"];
                    EmbeddedSigning embeddedSigning = new EmbeddedSigning();
                    RegisterBAL registerBAL = new RegisterBAL();
                    var bol = embeddedSigning.DownLoadSpouseSignedDocument(eId);
                    if (bol)
                    {
                        var Data = registerBAL.GetSpouseDocuments_EmailId(eId);
                        if (Data.Tables.Count > 0)
                        {
                            if (Data.Tables[0].Rows.Count > 0)
                            {
                                List<Attachment> attch = new List<Attachment>();
                                for (int i = 0; i < Data.Tables[0].Rows.Count; i++)
                                {
                                    attch.Add(new Attachment(Server.MapPath("~/AppDocs/") + Data.Tables[0].Rows[i]["S_Document_Name"].ToString()));
                                }
                                StringBuilder sb = new StringBuilder();
                                sb.Append("<p>Hello,</p>");
                                sb.Append("<p>You have received document(s) from Docu-sign.</p>");
                                sb.Append("<p>Please check the attachemnts for the same.</p>");
                                sb.Append("<p>Thank You.</p>");
                                sb.Append("<p>Sincerely,</p>");
                                sb.Append("<p>Team Loan Mantra</p>");
                                new MailHelper().Send(Data.Tables[0].Rows[0]["Email_Id"].ToString(), sb.ToString(), "Signed Document from Docu-sign.", attch, "Spouse Signature | Signed Document from Docu-sign");
                            }
                        }
                    }
                    return Redirect("http://www.users.loanmantra.com/");
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }
        #region  Amort Scheduled

        public ActionResult AmortScheduleStandard(int appId)
        {
            try
            {
                GetDropDownlist();
                AmortScheduleStandard objAmortScheduleStandard = new LoanApplicationBAL().GetAmortScheduleStandard(appId);
                objAmortScheduleStandard.CustID = appId;
                Session["CustID"] = objAmortScheduleStandard.CustID;

                //objAmortScheduleStandard.PurchaseEquipment = "10";
                //objAmortScheduleStandard.PurchaseImprovements = "10";
                //objAmortScheduleStandard.PurchaseInventory = "10";
                //objAmortScheduleStandard.LandAndBuilding = "30";
                //objAmortScheduleStandard.TotalAmount = "30";
                return View(objAmortScheduleStandard);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult AmortSchedule(int appId)
        {
            try
            {
                GetDropDownlist();
                AmortScheduleStandard objAmortScheduleStandard = new LoanApplicationBAL().GetAmortScheduleStandard(appId);
                objAmortScheduleStandard.CustID = appId;
                Session["CustID"] = objAmortScheduleStandard.CustID;

                return PartialView("~/Views/Shared/_AmortScheduleStandard.cshtml", objAmortScheduleStandard);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);

                
            }
            return PartialView("~/Views/Shared/_AmortScheduleStandard.cshtml");
        }

        #endregion

        [HttpPost]
        [ActionName("AmortScheduleStandard")]
        public ActionResult AmortScheduleStandard_Post(AmortScheduleStandard AmortScheduleStandard)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    AmortScheduleStandard.CustID = Convert.ToInt16(Session["CustID"]);
                    string[] strDownPaymentOther = Request.Form.GetValues("DownPaymentOther");
                    if (strDownPaymentOther != null)
                        AmortScheduleStandard.DownpaymentOther = String.Join("&", strDownPaymentOther);
                    GetDropDownlist();
                    int result = new LoanApplicationBAL().SaveAmortScheduleStandard(AmortScheduleStandard);
                    if (result > 0)
                    {
                        ModelState.AddModelError("", "Amort schedule standard is saved sucessfully.");
                        return View(AmortScheduleStandard);
                    }
                    else
                    {
                        ModelState.AddModelError("", "an error accured while saving amort schedule standard, please try again.");
                    }
                }

                return View(AmortScheduleStandard);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();

        }

        public ActionResult SignedUserDocsMail()
        {
            LoanApplication LoanApplication = new LoanApplication();
            try
            {
                var AdvisorsList = new LoanApplicationBAL().GetLoanAdvisors();
                ViewBag.AdvisorsList = AdvisorsList;
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
            }
            return PartialView("_SendSignedUserDocsMail", LoanApplication);
        }


        public ActionResult ContactUs()
        {
            try
            {
                var frm = new ContactUs();
                return View("ContactUs", frm);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        public ActionResult ContactUs(ContactUs frm)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    var result = new RegisterBAL().SaveContactUs(frm);
                    frm.Id = result;
                    if (result > 0)
                    {
                        string strBody = ContactUsContent(frm.Email);
                        string subject = "Thank you for your interest in Loan Mantra and our services !!";
                        new MailHelper().Send(frm.Email, strBody, subject, false, "Enquiry");
                        new MailHelper().Send(string.Empty, frm.Message, "Enquiry: " + frm.Subject, true, "Enquiry");


                    }
                    else
                    {
                        ModelState.AddModelError("", "an error accured while confirming your email, please try again.");
                    }
                }
                return View("ContactUs", frm);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }


        [HttpPost]

        public ActionResult PostInquiries(Inquiries frm)
        {
            try
            {

                if (ModelState.IsValid)
                {
                    var result = new RegisterBAL().SaveInquiries(frm);
                    frm.Id = result;
                    if (result > 0)
                    {
                        //string strBody = ContactUsContent(frm.Email);
                        //string subject = "Thank you for your interest in Loan Mantra and our services !!";
                        //new MailHelper().Send(frm.Email, strBody, subject, false, "Enquiry");
                        //new MailHelper().Send(string.Empty, frm.Message, "Enquiry: " + frm.Subject, true, "Enquiry");
                        return Json("Success");

                    }
                    else
                    {
                        return Json("Error");
                        // ModelState.AddModelError("", "an error accured while confirming your email, please try again.");
                    }
                }
                else
                {
                    return Json("Invalid Data");
                    // ModelState.AddModelError("", "an error accured while confirming your email, please try again.");
                }
                // return View("ContactUs", frm);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                return Json("error:" + ex.Message);
                //
            }

        }

        public ActionResult About()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult Advisor()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult Career()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult Testimonaials()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult Partners()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult Portfolio()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult Services()
        {
            return View();
        }

        public ActionResult Tools()
        {
            LoanApplication objLoanApplication = new LoanApplication();
            return View(objLoanApplication);
        }

        public ActionResult ToolsNew()
        {
            LoanApplication objLoanApplication = new LoanApplication();
            return View(objLoanApplication);
        }

        public PartialViewResult GetEMIData(string terms, string intRate, string loanAmount, DateTime? startYear)
        {


            //if (!string.IsNullOrEmpty(terms) && !string.IsNullOrEmpty(intRate) && startYear != null && !string.IsNullOrEmpty(loanAmount))
            //{
            //    int term = Convert.ToInt32(terms);

            //    double rate = Convert.ToDouble((Convert.ToDecimal(intRate) / 100)) / 12;
            //    if (rate > 0)
            //    {
            //        int period = 0;
            //        var totInterest = Convert.ToDouble("0");
            //        var totPrincipal = Convert.ToDouble("0");
            //        DateTime date = startYear.Value;// new DateTime(2015, 05, 01);
            //        double balance = Convert.ToDouble(loanAmount);
            //        CalcModel startMonth = new CalcModel();
            //        startMonth.Period = 0;
            //        startMonth.Balance = balance;
            //        startMonth.Date = date;
            //        startMonth.Interest = 0;
            //        startMonth.Principal = 0;
            //        startMonth.Pandl = 0;
            //        calc.Add(startMonth);
            //        while (Convert.ToDouble(balance) > 0)
            //        {
            //            var monthData = new CalcModel();
            //            int totalNumberOfMonths = (term * 12);
            //            var denominator = Math.Pow((1 + (double)rate), totalNumberOfMonths) - 1;
            //            monthData.Pandl = Convert.ToDouble((double)rate + ((double)rate / denominator)) * startMonth.Balance;
            //            period += 1;
            //            monthData.Date = date.AddMonths(period);
            //            monthData.Period = period;
            //            monthData.Interest = balance * rate;
            //            totInterest += monthData.Interest.Value;
            //            monthData.Principal = monthData.Pandl - monthData.Interest;
            //            totPrincipal += monthData.Principal.Value;
            //            balance -= monthData.Principal.Value;
            //            balance= balance= Math.Round(balance, 4);
            //            monthData.Balance = balance;
            //            calc.Add(monthData);


            //        }
            //calc[0].TotalInterest = Math.Round(totInterest, 2);
            //        calc[0].TotalPrincipal = Math.Round(totPrincipal, 2);
            //    }
            //}
            List<CalcModel> calc = new List<CalcModel>();
            if (startYear == null)
                startYear = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 01);
            if (!string.IsNullOrEmpty(terms) && !string.IsNullOrEmpty(intRate) && startYear != null && !string.IsNullOrEmpty(loanAmount))
            {
                int term = Convert.ToInt32(terms);
                int totalNumberOfMonths = (term * 12);



                double rate = Convert.ToDouble((Convert.ToDecimal(intRate) / 100)) / 12;
                if (rate > 0)
                {
                    int period = 0;
                    var totInterest = Convert.ToDouble("0");
                    var totPrincipal = Convert.ToDouble("0");
                    DateTime date = startYear.Value;// new DateTime(2015, 05, 01);
                    double balance = Convert.ToDouble(loanAmount);
                    CalcModel startMonth = new CalcModel();
                    startMonth.Period = 0;
                    startMonth.Balance = balance;
                    startMonth.Date = date;
                    startMonth.Interest = 0;
                    startMonth.Principal = 0;
                    startMonth.Pandl = 0;
                    calc.Add(startMonth);



                    if (Convert.ToDouble(balance) > 0)
                    {
                        for (int i = 0; i < totalNumberOfMonths; i++)
                        {
                            var monthData = new CalcModel();
                            var denominator = Math.Pow((1 + rate), totalNumberOfMonths) - 1;
                            monthData.Pandl = Convert.ToDouble(rate + (rate / denominator)) * startMonth.Balance;
                            period += 1;
                            monthData.Date = date.AddMonths(period);
                            monthData.Period = period;
                            monthData.Interest = balance * rate;
                            totInterest += monthData.Interest.Value;
                            monthData.Principal = monthData.Pandl - monthData.Interest;
                            totPrincipal += monthData.Principal.Value;
                            balance -= monthData.Principal.Value;
                            monthData.Balance = balance;
                            calc.Add(monthData);
                        }
                        calc[0].TotalInterest = Math.Round(totInterest, 2);
                        calc[0].TotalPrincipal = Math.Round(totPrincipal, 2);
                    }



                }
            }
            return PartialView("_LoanCalculations", calc);
        }


        [HttpPost]
        public ActionResult CheckUniqueEmailSpouse(int id, int cust_id, string email_id)
        {
            bool ifuser = false;
            try
            {
                LoanApplicationBAL objLoanApplicationBAL = new LoanApplicationBAL();
                ifuser = objLoanApplicationBAL.UnniqueEmailExist(id, cust_id, email_id);
                return Json(ifuser.ToString());
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                

                return Json(ifuser.ToString());
            }

        }



        public ActionResult DocsMail(int AppID)
        {
            LoanApplication LoanApplication = new LoanApplication();
            try
            {
                var AdvisorsList = new LoanApplicationBAL().GetLoanAdvisors();
                LoanApplication = new LoanApplicationBAL().GetLoanApplication(AppID);
                ViewBag.AdvisorsList = AdvisorsList;
                Session["CustID"] = AppID;
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("_SendDocsMail", LoanApplication);
        }

        public ActionResult NewApplication()
        {
            try
            {
                GetDropDownlist();
                return PartialView("_NewApplication", new Register());
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
            }
            return PartialView("_NewApplication", new Register());
        }

        [HttpPost]
        public ActionResult NewApplication(List<Register> register)
        {
            int result = 0;
            try
            {
                GetDropDownlist();
                //if (ModelState.AsEnumerable().Where(x => x.Value.Errors.Count>0).Select(x=>x.Key).Count()>0)
                //{
                //    ModelState.Remove("ID");
                //    ModelState.Remove("FirstName");
                //    ModelState.Remove("LastName");
                //    ModelState.Remove("advisorCode");
                //    ModelState.Remove("ConfirmPassword");
                //    ModelState.Remove("ContactNo");
                //    ModelState.Remove("CreatePassword");
                //    ModelState.Remove("EmailAddresss");
                //    ModelState.Remove("TaxId");

                //}

                LoanApplication loanApplication = new LoanApplication();
                MCAForm mCAForm = new MCAForm();
                if (Session.Keys.Count == 0)
                {
                    Session.Clear();
                    Session.RemoveAll();
                    Session.Abandon();
                    ModelState.AddModelError("", "Your session has been expired, please log in here.");
                    return RedirectToAction("Login", "Home");
                }
                if (Convert.ToInt32(Session["UserId"]) > 0 && Session.Keys.Count > 0)
                {


                    loanApplication.UserId = Convert.ToInt32(Session["UserId"]);


                    DataSet ds = new LoanApplicationBAL().NewApplication(register[0]);
                    // mcaId = new RegisterBAL().SaveRegisterInfointoMCAInfo(register);
                    if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                    { result = Convert.ToInt32(ds.Tables[0].Rows[0][0]); }
                    if (ds.Tables.Count > 0 && ds.Tables[1].Rows.Count > 0)
                    {
                        for (int i = 0; i < ds.Tables[1].Rows.Count; i++)
                        {
                            string FileNames = Convert.ToString(ds.Tables[1].Rows[i]["FileName"]);
                            if (!string.IsNullOrEmpty(FileNames))
                            {
                                string[] fileNameArr = FileNames.Split(',');
                                string NewFileNames = "";
                                try
                                {
                                    for (int j = 0; j < fileNameArr.Count(); j++)
                                    {
                                        if (!string.IsNullOrEmpty(fileNameArr[j]) && j == fileNameArr.Length - 1)
                                        {


                                            var tick = "";
                                            string FileName = fileNameArr[j];
                                            if (FileName.Split('_')[0] == "AdvisorGenerated")
                                            {
                                                tick = FileName.Split('_')[1];
                                            }
                                            if (FileName.Split('_')[1] == "Uploaded")
                                            {
                                                tick = FileName.Split('_')[0];
                                            }

                                            string FileNameNew = DateTime.Now.Ticks.ToString() + "_" + FileName.Replace(tick + "_", "");


                                            string path = "";
                                            string pathToFetch = "";

                                            path = WebConfigurationManager.AppSettings["lendserviceDocs"]; //Server.MapPath("~/AppDocs/DocumentMst/");

                                            pathToFetch = WebConfigurationManager.AppSettings["signedAdvisorDocs"]; //Server.MapPath("~/AppDocs/AdvisorSignedDocs/");


                                            if (!Directory.Exists(path))
                                                Directory.CreateDirectory(path);
                                            string fname = "";
                                            fname = Path.Combine(pathToFetch, FileName);
                                            FileInfo file = new FileInfo(fname);
                                            fname = Path.Combine(path, FileNameNew);

                                            file.CopyTo(fname);

                                            string otherServer = WebConfigurationManager.AppSettings["HcaNow"];
                                            string HcaDocs = WebConfigurationManager.AppSettings["HcaDocs"];

                                            //if (!Directory.Exists(otherServer))
                                            //    Directory.CreateDirectory(otherServer);
                                            //fname = Path.Combine(otherServer, fileName + ".cshtml");
                                            //file.SaveAs(fname);



                                            if (!Directory.Exists(HcaDocs))
                                                Directory.CreateDirectory(HcaDocs);
                                            fname = Path.Combine(HcaDocs, FileNameNew);
                                            file.CopyTo(fname);

                                            NewFileNames = NewFileNames + "," + FileNameNew;
                                        }
                                    }
                                    fileCoordinateDetail objFcd = new fileCoordinateDetail();
                                    objFcd.FileName = NewFileNames.Substring(1, NewFileNames.Length - 1);
                                    objFcd.EntityId = Convert.ToString(ds.Tables[1].Rows[i]["EntityId"]);
                                    objFcd.CustId = Convert.ToString(ds.Tables[1].Rows[i]["CustId"]);
                                    objFcd.DocumentId = Convert.ToString(ds.Tables[1].Rows[i]["DocumentId"]);
                                    int x = new LoanApplicationBAL().SaveNewApplicationFiles(objFcd);

                                }
                                catch (Exception ex)
                                {
                                    string url = Request.Url.ToString();
                                    new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);

                                }

                            }
                        }
                    }

                    Session["CustID"] = result;
                    loanApplication.BasicInfo.CustID = result;
                    loanApplication.BasicInfo.CustID = result;
                    if (result > 0)
                    {
                        string encryptedPara = ParameterEncryption.EncryptParametersAtServerEnd("AppId=" + Convert.ToString(Session["CustID"]));
                        return Json(encryptedPara);
                        //string encryptedPara = ParameterEncryption.EncryptParametersAtServerEnd("appId=" + Convert.ToString(Session["CustID"]));
                        //return RedirectToAction("EditApplication", "Home", new { q = encryptedPara });
                    }
                    else
                    {

                        return Json("0");
                    }

                }
                else
                {
                    return Json("0");
                }



            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                return Json("0");
            }

        }

        [ParameterDecryption]
        public ActionResult ApplyLoan(int appId)
        {
            try
            {
                TempData.Keep("Initial");
                if (TempData["Initial"] != null)
                {
                    Session["Initial"] = TempData["Initial"];
                }

                GetDropDownlist();
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                if (Session["myTimeZoneId"] != null)
                {
                    objLoanApplication.Comment.Comments = DateFormatter.GetUpdatedLocalTimeZone(objLoanApplication.Comment.Comments, Session["myTimeZoneId"].ToString());
                }
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                ViewBag.TypeOfBusiness = objLoanApplication.BasicInfo.TypeOfBusiness;
                objLoanApplication.BusinessProfile.BusinessType = objLoanApplication.BasicInfo.TypeOfBusiness;
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetAllDocs(appId);
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);




                return View("ApplyLoan", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("ApplyLoan");
        }
        public ActionResult LoadBusinessProfile(string businessType, string CustID)
        {

            try
            {

                int appId = Convert.ToInt32(CustID);
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                // objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                //if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                //{
                //    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                //    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                //}
                ViewBag.TypeOfBusiness = businessType;
                objLoanApplication.BusinessProfile.BusinessType = businessType;
                //objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                //objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                //objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                //objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                //objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                //objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                //objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();

                return PartialView("~/Views/Shared/_BusinessProfile.cshtml", objLoanApplication);

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                return Json("");
            }
        }

        [ParameterDecryption]
        [Compress]
        public ActionResult EditApplication(int appId)
        {
            var ayear = 0;
            try
            {
                #region MultiTab_Application

                GetDropDownlist();
                Session["CustID"] = appId;
                ViewBag.CustId = appId;
                var objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                var mcaFId = objLoanApplication.BasicInfo.McaFId;
                Session["MCAID"] = mcaFId;
                GetApplicationStatus();
                objLoanApplication.otherBasicInfo.CustID = objLoanApplication.BasicInfo.CustID;
                objLoanApplication.otherBasicInfo.Id = 0;
                if (objLoanApplication.otherBasicInfo.NumberOfEmplyees == null) { objLoanApplication.otherBasicInfo.NumberOfEmplyees = "0"; }
                ViewBag.ApplicationStatus = objLoanApplication.BasicInfo.ApplicationStatus;
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                ViewBag.otherBusinesses = objLoanApplication.otherBusinesses;
                ViewBag.TypeOfBusiness = objLoanApplication.BasicInfo.TypeOfBusiness;
                objLoanApplication.BusinessProfile.BusinessType = objLoanApplication.BasicInfo.TypeOfBusiness;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetAllDocs(appId);
                //Get Advisor generated document section 
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                var smartApiBal = new SmartApiBAL();
                objLoanApplication.BusinessWithOwnerDetails.MiscDocs = smartApiBal.GetSmartApiDocumentByLoanId(appId);
                objLoanApplication.BusinessWithOwnerDetails.Users = new RegisterBAL().GetUsersById(Convert.ToInt32(Session["UserId"]));
                objLoanApplication.MCAForm = new LoanApplicationBAL().GetMCAFormDetailsBasicInformation(appId);
                objLoanApplication.MCAForm.CustID = objLoanApplication.BasicInfo.CustID;


                var amort = new LoanApplicationBAL().GetAmortScheduleStandard(Convert.ToInt32(Session["CustID"]));
                objLoanApplication.LoanAmortization = amort;
                GetDropDownlist();
                GetApplicationStatus();

                #endregion

                #region CashFlow_BorrowerSide || Added by: Harpreet Singh || Date: 16-05-2021

                TempData["DllYearValue"] = "";
                var year = DateTime.Now.Year; 
                ViewBag.fYear = year;

                GetDropDownlist();
                var objSummary = new Summary();

                objLoanApplication.Summary = new SummaryBAL().GetSummaryDetailByCustId(appId);
                objLoanApplication.Summary.Cust_Id = appId;
                Session["CustID"] = objLoanApplication.Summary.Cust_Id;
                Session["UpdateCustIDAfterddlChanged"] = objLoanApplication.Summary.Cust_Id;
                var strYear = "";
                for (var i = 4; i > 0; i--)
                {
                    strYear += (year - i).ToString() + ',';
                }
                strYear += year;

                var lstYear = GetYearDropDownlist(year);
                var years = lstYear.Aggregate("", (current, yr) => current + (yr.Value + ','));
                years = years.Substring(0, years.Length - 1);
                objLoanApplication.Summary.SummaryDetailList =
                    new SummaryBAL().GetSummaryDetailList(appId, strYear, strYear, 1);
                objLoanApplication.Summary.basicInfo = objLoanApplication.BasicInfo;
                objLoanApplication.Summary.BusinessName = objLoanApplication.BasicInfo.BusinessName;
                var loansDetail = new LoanApplicationBAL().GetAmortScheduleStandard(appId);
                var otherDownPayment = Convert.ToDecimal("0");
                if (!string.IsNullOrEmpty(loansDetail.DownpaymentOther))
                {
                    var dnPay = loansDetail.DownpaymentOther.Split('&');
                    otherDownPayment += dnPay.Where(item => item != "").Sum(item => Convert.ToDecimal(item));
                }
                if (loansDetail.LessBorrowerDownPayment != null)
                {
                    objLoanApplication.Summary.DownPaymenet = string.IsNullOrEmpty(loansDetail.LessBorrowerDownPayment) == true ? 0 : Convert.ToDecimal(loansDetail.LessBorrowerDownPayment);
                    objLoanApplication.Summary.DownPaymenet += otherDownPayment;
                }
                objLoanApplication.Summary.DealType = objLoanApplication.BasicInfo.Purpose;
                objLoanApplication.Summary.BorrowingEntity = objLoanApplication.BasicInfo.BusinessName;
                objLoanApplication.Summary.EntityAddress = objLoanApplication.BusinessProfile.BusinessAddressLine1 + (!string.IsNullOrWhiteSpace(objLoanApplication.BusinessProfile.BusinessAddressLine2) ? ", " + objLoanApplication.BusinessProfile.BusinessAddressLine2 : "");
                objLoanApplication.Summary.LoanAmountId = objLoanApplication.LoanAmount.LoanAmountID;
                objLoanApplication.Summary.DealSize = string.IsNullOrEmpty(loansDetail.TotalAmount) ? 0 : Convert.ToDecimal(loansDetail.TotalAmount);
                objLoanApplication.Summary.LoanSize = string.IsNullOrEmpty(loansDetail.EquityLoanAmount) == true ? 0 : Convert.ToDecimal(loansDetail.EquityLoanAmount);
                if (objLoanApplication.Summary.SummaryDetail == null)
                {
                    objLoanApplication.Summary.SummaryDetail = new List<ENTITY.Summary>();
                }


                objLoanApplication.Summary.FormName = GetRandNumber(5);
                objLoanApplication.Summary.PreviousFormName = objSummary.FormName;
                objLoanApplication.Summary.fromYear = year;
                objLoanApplication.Summary.YearDropdownList = GetYearDropDownlist(year);
                var data = new List<SelectListItem>();


                foreach (var item in objLoanApplication.Summary.YearDropdownList)
                {
                    var summaryData = new Summary
                    {
                        SummaryYear = Convert.ToInt32(item.Value),
                        StrSummaryYear = item.Text,
                        FormName = objSummary.FormName,
                        PreviousFormName = objSummary.FormName
                    };
                    objLoanApplication.Summary.SummaryDetail.Add(summaryData);
                    if (item.Text.Contains("*"))
                    {
                        var monthDropdownList = new List<SelectListItem>(){
                                                                    new SelectListItem { Text = "Jan", Value = "1" },
                                                                    new SelectListItem { Text = "Feb", Value = "2" },
                                                                    new SelectListItem { Text = "Mar", Value = "3" },
                                                                    new SelectListItem { Text = "Apr", Value = "4" },
                                                                    new SelectListItem { Text = "May", Value = "5" },
                                                                    new SelectListItem { Text = "Jun", Value = "6" },
                                                                    new SelectListItem { Text = "Jul", Value = "7" },
                                                                    new SelectListItem { Text = "Aug", Value = "8" },
                                                                    new SelectListItem { Text = "Sep", Value = "9" },
                                                                    new SelectListItem { Text = "Oct", Value = "10" },
                                                                    new SelectListItem { Text = "Nov", Value = "11" },
                                                                    new SelectListItem { Text = "Dec", Value = "12" }
                                                                };
                        data = monthDropdownList;
                    }
                }
                ViewBag.Months = data;

                var fromYear1 = DateTime.Now.Year - 1; 
                var toYear1 = fromYear1;
                if (fromYear1 > 0)
                {
                    var value = new DateTime(fromYear1, 1, 1);
                    toYear1 = fromYear1;
                    fromYear1 = value.AddYears(+1).Year;
                }
                var yearDropdownList1 = new List<SelectListItem>();
                for (var i = toYear1; i <= fromYear1; i++)
                {
                    if (ayear < fromYear1)
                    {
                        yearDropdownList1.Add(new SelectListItem
                        {
                            Text = (toYear1).ToString(),
                            Value = (toYear1).ToString()
                        });
                        ayear = (toYear1 + 1);
                    }
                    else
                    {
                        yearDropdownList1.Add(new SelectListItem
                        {
                            Text = (toYear1 + 1).ToString(),
                            Value = (toYear1 + 1).ToString()
                        });

                    }
                }
                ViewBag.datayear = yearDropdownList1;
                objLoanApplication.Summary.StatusDropdown = GetSummaryStatus();
                objLoanApplication.Summary.CashFlowDetails = new SummaryBAL().GetCashFlowDetails(appId, 1, strYear);
                if (Convert.ToDouble(objLoanApplication.Summary.CashFlowDetails.lstCashFlow.Max(x => x.Val4)) > 0 && Convert.ToDouble(objLoanApplication.Summary.CashFlowDetails.lstCashFlow.Max(x => x.Val5)) > 0)
                {
                    ViewBag.checkforyear = objLoanApplication.Summary.CashFlowDetails.lstYears.Max();   //sandeep
                    objLoanApplication.Summary.CashFlowDetails.ddlcashflowyear = objLoanApplication.Summary.CashFlowDetails.lstYears.Max();  //sandeep
                    ViewBag.checkformonth = 1;
                }
                objLoanApplication.Summary.StatusDropdown = GetSummaryStatus();
                GetDropDownlist();


                #endregion


                if (mcaFId > 0)
                {
                    return View("ApplyLoanNew", objLoanApplication);
                }
                if (objLoanApplication.BasicInfo.SixStepFlag != true)
                {
                    var encryptedPara = ParameterEncryption.EncryptParametersAtServerEnd("appId=" + Convert.ToString(Session["CustID"]));
                    return RedirectToAction("ApplyLoan", new { q = encryptedPara });
                }

                if (Session["myTimeZoneId"] != null)
                {
                    objLoanApplication.Comment.Comments = DateFormatter.GetUpdatedLocalTimeZone(objLoanApplication.Comment.Comments, Session["myTimeZoneId"].ToString());
                }
                return View("ApplyLoanNew", objLoanApplication);
            }
            catch (Exception ex)
            {
                var url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("ApplyLoanNew");
        }




        public ActionResult LoadDataOnTabClick_Borrower(int appId, string TabName)
        {
            try
            {
                #region MultiTab_Application

                GetDropDownlist();
                Session["CustID"] = appId;
                ViewBag.CustId = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.Users = new RegisterBAL().GetUsersById(Convert.ToInt32(Session["UserId"]));

                var McaFId = objLoanApplication.BasicInfo.McaFId;
                Session["MCAID"] = McaFId;
                GetApplicationStatus();

                objLoanApplication.otherBasicInfo.CustID = objLoanApplication.BasicInfo.CustID;
                objLoanApplication.otherBasicInfo.Id = 0;
                if (objLoanApplication.otherBasicInfo.NumberOfEmplyees == null) { objLoanApplication.otherBasicInfo.NumberOfEmplyees = "0"; }
                ViewBag.ApplicationStatus = objLoanApplication.BasicInfo.ApplicationStatus;
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);

                ViewBag.otherBusinesses = objLoanApplication.otherBusinesses;
                ViewBag.TypeOfBusiness = objLoanApplication.BasicInfo.TypeOfBusiness;
                objLoanApplication.BusinessProfile.BusinessType = objLoanApplication.BasicInfo.TypeOfBusiness;

                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }


                objLoanApplication.MCAForm = new LoanApplicationBAL().GetMCAFormDetailsBasicInformation(appId);
                objLoanApplication.MCAForm.CustID = objLoanApplication.BasicInfo.CustID;


                AmortScheduleStandard amort = new LoanApplicationBAL().GetAmortScheduleStandard(Convert.ToInt32(Session["CustID"]));
                objLoanApplication.LoanAmortization = amort;
                GetDropDownlist();
                //objLoanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetBorrowerDocs(appId);
                GetApplicationStatus();

                if (TabName == "BP-tab")
                {
                    return PartialView("~/Views/Shared/_BusinessProfileEdit.cshtml", objLoanApplication);
                }
                else if (TabName == "BH-tab")
                {
                    return PartialView("~/Views/Shared/_BusinessHistoryEdit.cshtml", objLoanApplication);
                }
                else if (TabName == "PI-tab")
                {
                    return PartialView("~/Views/Shared/_OwnersListPersonalResume.cshtml", objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList);
                }
                else if (TabName == "PFS-tab")
                {
                    return PartialView("~/Views/Shared/_OwnersListPFS.cshtml", objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList);
                }
                else if (TabName == "LA-tab")
                {
                    return PartialView("~/Views/Shared/_LoanAmountEdit.cshtml", objLoanApplication);
                }
                else if (TabName == "Documents-tab")
                {
                    return PartialView("~/Views/Shared/_Documents.cshtml", objLoanApplication.BusinessWithOwnerDetails);
                }
                else if (TabName == "Notes-tab")
                {
                    if (Session["myTimeZoneId"] != null)
                    {
                        objLoanApplication.Comment.Comments = DateFormatter.GetUpdatedLocalTimeZone(objLoanApplication.Comment.Comments, Session["myTimeZoneId"].ToString());
                    }
                    return PartialView("~/Views/Shared/_Notes.cshtml", objLoanApplication.Comment);
                }
                else if (TabName == "Amort-tab")
                {
                    objLoanApplication.LoanAmortization = amort;
                    return PartialView("~/Views/Shared/_AmortScheduleStandard.cshtml", objLoanApplication.LoanAmortization);
                }

                else if (TabName == "Restaurant-tab")
                {

                    return PartialView("~/Views/Shared/_Restaurant.cshtml", objLoanApplication.Restaurant);
                }
                else if (TabName == "Contractor-tab")
                {

                    return PartialView("~/Views/Shared/_Contractor.cshtml", objLoanApplication.Contractor);
                }
                else if (TabName == "Medical-tab")
                {

                    return PartialView("~/Views/Shared/_Medical.cshtml", objLoanApplication.Medical);
                }
                else if (TabName == "GasStation-tab")
                {

                    return PartialView("~/Views/Shared/_GasStation.cshtml", objLoanApplication.GasStation);
                }
                else if (TabName == "CPA-tab")
                {

                    return PartialView("~/Views/Shared/_CPA.cshtml", objLoanApplication.CPA);
                }
                else if (TabName == "Hotel-tab")
                {

                    return PartialView("~/Views/Shared/_Hotel.cshtml", objLoanApplication.Hotel);
                }

                #endregion
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                Logger.Error(this.ControllerContext.RouteData.Values["action"].ToString() + ": " + ex.ToString());
            }

            return PartialView();

        }


        public static string GetRandNumber(int length)
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var result = new string(
                Enumerable.Repeat(chars, length)
                          .Select(s => s[random.Next(s.Length)])
                          .ToArray());

            return result;
        }


        private List<SelectListItem> GetYearDropDownlist(int fYear)
        {
            try
            {
                List<SelectListItem> yearDropdownList = new List<SelectListItem>();
                ViewBag.Months = null;
                int fromYear = fYear;// DateTime.Now.AddYears(-4).Year;
                int toYear = fromYear + 3;// DateTime.Now.Year;

                if (fYear > 0)
                {
                    DateTime value = new DateTime(fYear, 1, 1);
                    toYear = fYear;
                    fromYear = value.AddYears(-3).Year;
                }
                for (int i = fromYear; i <= toYear; i++)
                {
                    yearDropdownList.Add(new SelectListItem
                    {
                        Text = i == toYear ? (i.ToString() + "(*)") : i.ToString(),
                        Value = i.ToString()
                    });
                }
                if (fYear > 0)
                {
                    yearDropdownList.Add(new SelectListItem
                    {
                        Text = toYear.ToString() + "(A)",
                        Value = toYear.ToString()
                    });
                }

                return yearDropdownList;

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                Logger.Error("GetDropDownlist() : " + ex.ToString());
            }
            return null;
        }

        private List<SelectListItem> GetSummaryStatus()
        {
            List<SelectListItem> summaryStatus = new List<SelectListItem>();

            summaryStatus.Add(new SelectListItem() { Text = "Active", Value = "1" });
            summaryStatus.Add(new SelectListItem() { Text = "In-Active", Value = "2" });
            return summaryStatus;
        }

        //public ActionResult NewApplication(int appId)
        //{
        //    try
        //    {
        //        Session["CustID"] = appId;
        //        LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
        //        objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
        //        if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
        //        {
        //            objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
        //            objLoanApplication.BusinessProfile.TaxReturnForm = "";
        //        }
        //        ViewBag.TypeOfBusiness = objLoanApplication.BasicInfo.TypeOfBusiness;
        //        objLoanApplication.BusinessProfile.BusinessType = objLoanApplication.BasicInfo.TypeOfBusiness;
        //        objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
        //        objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
        //        objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
        //        objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
        //        objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
        //        objLoanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetAllDocs(appId);
        //        objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
        //        objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
        //        GetDropDownlist();

        //        return View("ApplyLoan", objLoanApplication);
        //    }
        //    catch (Exception ex)
        //    {
        //        string url = Request.Url.ToString();
        //        new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
        //        
        //    }
        //    return View("ApplyLoan");
        //}

        public ActionResult HowItWorks()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult LenderCriteria(string ID)
        {
            LenderConsole objLenderConsole = new LenderConsole();
            try
            {
                if (ID != null)
                {
                    objLenderConsole = new LoanApplicationBAL().GetLenderConsoleDetails(Convert.ToInt32(ID));
                }

                else
                {
                    string lenderUserId = Session["UserId"].ToString();
                    objLenderConsole = new LoanApplicationBAL().GetLenderConsoleDetails(Convert.ToInt32(lenderUserId));

                }


                BindDropdowns();

                return View(objLenderConsole);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]

        public ActionResult LenderCriteria(LenderConsole lenderConsole)
        {
            try
            {
                int result = 0;

                if (ModelState.IsValid)
                {
                    lenderConsole.GeographicCoverage = lenderConsole.GeographicCoverageMulti;
                    lenderConsole.PropertyType = lenderConsole.PropertyTypeMulti;
                    lenderConsole.PrescreenRequirement = lenderConsole.PrescreenRequirementMulti;
                    result = new LoanApplicationBAL().SaveLenderConsole(lenderConsole);
                }

                return Json(new { isSuccess = result });

                //return RedirectToAction("LenderConsole");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }


        public ActionResult LenderConsoleDetails(string ID)
        {
            LenderConsole objLenderConsole = new LenderConsole();
            try
            {
                if (ID != null)
                {
                    objLenderConsole = new LoanApplicationBAL().GetLenderConsoleDetails(Convert.ToInt32(ID));

                }

                else
                {
                    string lenderUserId = Session["UserId"].ToString();
                    objLenderConsole = new LoanApplicationBAL().GetLenderConsoleDetails(Convert.ToInt32(lenderUserId));

                }


                BindDropdowns();

                return View(objLenderConsole);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View(objLenderConsole);
        }

        private void BindDropdowns()
        {
            List<SelectListItem> InstitutionTypeList = new List<SelectListItem>();
            InstitutionTypeList.Add(new SelectListItem { Text = "Direct Lender", Value = "Direct Lender" });
            InstitutionTypeList.Add(new SelectListItem { Text = "Broker", Value = "Broker" });

            ViewBag.InstitutionTypeList = InstitutionTypeList;

            List<SelectListItem> ProgrameTypeList = new List<SelectListItem>();
            ProgrameTypeList.Add(new SelectListItem { Text = "SBA / Conventional", Value = "SBA / Conventional" });
            ProgrameTypeList.Add(new SelectListItem { Text = "Alternative / Equity", Value = "Alternative / Equity" });

            ViewBag.ProgramTypes = ProgrameTypeList;

            //List<SelectListItem> ProgramesList = new List<SelectListItem>();
            //ProgramesList.Add(new SelectListItem { Text = "Program 1", Value = "Program 1" });
            //ProgramesList.Add(new SelectListItem { Text = "Program 2", Value = "Program 2" });
            //ProgramesList.Add(new SelectListItem { Text = "Program 3", Value = "Program 3" });
            //ProgramesList.Add(new SelectListItem { Text = "Program 4", Value = "Program 4" });
            //ProgramesList.Add(new SelectListItem { Text = "Program 5", Value = "Program 5" });

            //ViewBag.ProgramsDetail = ProgramesList;

            //GetPrograms();

            List<SelectListItem> PropertyTypeList = new List<SelectListItem>();

            PropertyTypeList.Add(new SelectListItem { Text = "Office", Value = "Office" });
            PropertyTypeList.Add(new SelectListItem { Text = "Office Condo", Value = "Office Condo" });
            PropertyTypeList.Add(new SelectListItem { Text = "Medical / Dental", Value = "Medical / Dental" });
            PropertyTypeList.Add(new SelectListItem { Text = "Warehouse", Value = "Warehouse" });
            PropertyTypeList.Add(new SelectListItem { Text = "Mixed Use", Value = "Mixed Use" });
            PropertyTypeList.Add(new SelectListItem { Text = "Multi Family", Value = "Multi Family" });
            PropertyTypeList.Add(new SelectListItem { Text = "Working Capital", Value = "Working Capital" });
            PropertyTypeList.Add(new SelectListItem { Text = "Equipment", Value = "Equipment" });
            PropertyTypeList.Add(new SelectListItem { Text = "Franchise", Value = "Franchise" });
            PropertyTypeList.Add(new SelectListItem { Text = "Restaurants", Value = "Restaurants" });
            PropertyTypeList.Add(new SelectListItem { Text = "Day Care", Value = "Day Care" });
            PropertyTypeList.Add(new SelectListItem { Text = "Hotels", Value = "Hotels" });
            PropertyTypeList.Add(new SelectListItem { Text = "Convenience stores", Value = "Convenience stores" });
            PropertyTypeList.Add(new SelectListItem { Text = "Start-Ups", Value = "Start-Ups" });
            PropertyTypeList.Add(new SelectListItem { Text = "Retail", Value = "Retail" });
            PropertyTypeList.Add(new SelectListItem { Text = "Industrial", Value = "Industrial" });
            PropertyTypeList.Add(new SelectListItem { Text = "Self Storage", Value = "Self Storage" });
            PropertyTypeList.Add(new SelectListItem { Text = "Others", Value = "Others" });

            ViewBag.PropertyTypes = PropertyTypeList;

            List<SelectListItem> PrescreenRequirementList = new List<SelectListItem>();

            PrescreenRequirementList.Add(new SelectListItem { Text = "2 years business tax returns", Value = "2 years business tax returns" });
            PrescreenRequirementList.Add(new SelectListItem { Text = "2 years personal tax returns", Value = "2 years personal tax returns" });
            PrescreenRequirementList.Add(new SelectListItem { Text = "YTD financials", Value = "YTD financials" });
            PrescreenRequirementList.Add(new SelectListItem { Text = "Personal Financial Statements", Value = "Personal Financial Statements" });
            PrescreenRequirementList.Add(new SelectListItem { Text = "Property Summary (if relevant)", Value = "Property Summary (if relevant)" });
            PrescreenRequirementList.Add(new SelectListItem { Text = "Property Rent Roll (if relevant)", Value = "Property Rent Roll (if relevant)" });
            PrescreenRequirementList.Add(new SelectListItem { Text = "YTD financials", Value = "YTD financials" });

            ViewBag.PrescreenRequirements = PrescreenRequirementList;
            List<SelectListItem> StateList = new List<SelectListItem>();
            StateList.Add(new SelectListItem() { Text = "Alabama", Value = "Alabama" });
            StateList.Add(new SelectListItem() { Text = "Alaska", Value = "Alaska" });
            StateList.Add(new SelectListItem() { Text = "Arizona", Value = "Arizona" });
            StateList.Add(new SelectListItem() { Text = "Arkansas", Value = "Arkansas" });
            StateList.Add(new SelectListItem() { Text = "California", Value = "California" });
            StateList.Add(new SelectListItem() { Text = "Colorado", Value = "Colorado" });
            StateList.Add(new SelectListItem() { Text = "Connecticut", Value = "Connecticut" });
            StateList.Add(new SelectListItem() { Text = "Delaware", Value = "Delaware" });
            StateList.Add(new SelectListItem() { Text = "District of Columbia", Value = "District of Columbia" });
            StateList.Add(new SelectListItem() { Text = "Florida", Value = "Florida" });
            StateList.Add(new SelectListItem() { Text = "Georgia", Value = "Georgia" });
            StateList.Add(new SelectListItem() { Text = "Hawaii", Value = "Hawaii" });
            StateList.Add(new SelectListItem() { Text = "Idaho", Value = "Idaho" });
            StateList.Add(new SelectListItem() { Text = "Illinois", Value = "Illinois" });
            StateList.Add(new SelectListItem() { Text = "Indiana", Value = "Indiana" });
            StateList.Add(new SelectListItem() { Text = "Iowa", Value = "Iowa" });
            StateList.Add(new SelectListItem() { Text = "Kansas", Value = "Kansas" });
            StateList.Add(new SelectListItem() { Text = "Kentucky", Value = "Kentucky" });
            StateList.Add(new SelectListItem() { Text = "Louisiana", Value = "Louisiana" });
            StateList.Add(new SelectListItem() { Text = "Maine", Value = "Maine" });
            StateList.Add(new SelectListItem() { Text = "Maryland", Value = "Maryland" });
            StateList.Add(new SelectListItem() { Text = "Massachusetts", Value = "Massachusetts" });
            StateList.Add(new SelectListItem() { Text = "Michigan", Value = "Michigan" });
            StateList.Add(new SelectListItem() { Text = "Minnesota", Value = "Minnesota" });
            StateList.Add(new SelectListItem() { Text = "Mississippi", Value = "Mississippi" });
            StateList.Add(new SelectListItem() { Text = "Missouri", Value = "Missouri" });
            StateList.Add(new SelectListItem() { Text = "Montana", Value = "Montana" });
            StateList.Add(new SelectListItem() { Text = "Nebraska", Value = "Nebraska" });
            StateList.Add(new SelectListItem() { Text = "Nevada", Value = "Nevada" });
            StateList.Add(new SelectListItem() { Text = "New Hampshire", Value = "New Hampshire" });
            StateList.Add(new SelectListItem() { Text = "New Jersey", Value = "New Jersey" });
            StateList.Add(new SelectListItem() { Text = "New Mexico", Value = "New Mexico" });
            StateList.Add(new SelectListItem() { Text = "New York", Value = "New York" });
            StateList.Add(new SelectListItem() { Text = "North Carolina", Value = "North Carolina" });
            StateList.Add(new SelectListItem() { Text = "North Dakota", Value = "North Dakota" });
            StateList.Add(new SelectListItem() { Text = "Ohio", Value = "Ohio" });
            StateList.Add(new SelectListItem() { Text = "Oklahoma", Value = "Oklahoma" });
            StateList.Add(new SelectListItem() { Text = "Oregon", Value = "Oregon" });
            StateList.Add(new SelectListItem() { Text = "Pennsylvania", Value = "Pennsylvania" });
            StateList.Add(new SelectListItem() { Text = "Rhode Island", Value = "Rhode Island" });
            StateList.Add(new SelectListItem() { Text = "South Carolina", Value = "South Carolina" });
            StateList.Add(new SelectListItem() { Text = "South Dakota", Value = "South Dakota" });
            StateList.Add(new SelectListItem() { Text = "Tennessee", Value = "Tennessee" });
            StateList.Add(new SelectListItem() { Text = "Texas", Value = "Texas" });
            StateList.Add(new SelectListItem() { Text = "Utah", Value = "Utah" });
            StateList.Add(new SelectListItem() { Text = "Vermont", Value = "Vermont" });
            StateList.Add(new SelectListItem() { Text = "Virginia", Value = "Virginia" });
            StateList.Add(new SelectListItem() { Text = "Washington", Value = "Washington" });
            StateList.Add(new SelectListItem() { Text = "West Virginia", Value = "West Virginia" });
            StateList.Add(new SelectListItem() { Text = "Wisconsin", Value = "Wisconsin" });
            StateList.Add(new SelectListItem() { Text = "Wyoming", Value = "Wyoming" });

            ViewBag.StateList = StateList;

            List<SelectListItem> DealClosedOptionList = new List<SelectListItem>();
            DealClosedOptionList.Add(new SelectListItem { Text = "Yes", Value = "Yes" });
            DealClosedOptionList.Add(new SelectListItem { Text = "No", Value = "No" });

            ViewBag.DealClosedOption = DealClosedOptionList;
        }

        [HttpPost]
        [ActionName("LenderConsoleDetails")]
        public ActionResult LenderConsoleDetails_Post(LenderConsole lenderConsole)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    lenderConsole.GeographicCoverage = lenderConsole.GeographicCoverageMulti;
                    lenderConsole.PropertyType = lenderConsole.PropertyTypeMulti;
                    lenderConsole.PrescreenRequirement = lenderConsole.PrescreenRequirementMulti;
                    int result = new LoanApplicationBAL().SaveLenderConsole(lenderConsole);
                }

                return RedirectToAction("LenderConsole");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult LenderConsole()
        {
            try
            {
                List<SelectListItem> InstitutionTypeList = new List<SelectListItem>();
                InstitutionTypeList.Add(new SelectListItem { Text = "Direct Lender", Value = "Direct Lender" });
                InstitutionTypeList.Add(new SelectListItem { Text = "Broker", Value = "Broker" });

                ViewBag.InstitutionTypeList = InstitutionTypeList;

                List<SelectListItem> ProgrameTypeList = new List<SelectListItem>();
                ProgrameTypeList.Add(new SelectListItem { Text = "SBA / Conventional", Value = "SBA / Conventional" });
                ProgrameTypeList.Add(new SelectListItem { Text = "Alternative / Equity", Value = "Alternative / Equity" });

                ViewBag.ProgramTypes = ProgrameTypeList;

                List<SelectListItem> ProgramesList = new List<SelectListItem>();
                ProgramesList.Add(new SelectListItem { Text = "Program 1", Value = "Program 1" });
                ProgramesList.Add(new SelectListItem { Text = "Program 2", Value = "Program 2" });
                ProgramesList.Add(new SelectListItem { Text = "Program 3", Value = "Program 3" });
                ProgramesList.Add(new SelectListItem { Text = "Program 4", Value = "Program 4" });
                ProgramesList.Add(new SelectListItem { Text = "Program 5", Value = "Program 5" });

                ViewBag.Programs = ProgramesList;

                List<SelectListItem> PropertyTypeList = new List<SelectListItem>();

                PropertyTypeList.Add(new SelectListItem { Text = "Office", Value = "Office" });
                PropertyTypeList.Add(new SelectListItem { Text = "Office Condo", Value = "Office Condo" });
                PropertyTypeList.Add(new SelectListItem { Text = "Medical / Dental", Value = "Medical / Dental" });
                PropertyTypeList.Add(new SelectListItem { Text = "Warehouse", Value = "Warehouse" });
                PropertyTypeList.Add(new SelectListItem { Text = "Mixed Use", Value = "Mixed Use" });
                PropertyTypeList.Add(new SelectListItem { Text = "Multi Family", Value = "Multi Family" });
                PropertyTypeList.Add(new SelectListItem { Text = "Working Capital", Value = "Working Capital" });
                PropertyTypeList.Add(new SelectListItem { Text = "Equipment", Value = "Equipment" });
                PropertyTypeList.Add(new SelectListItem { Text = "Franchise", Value = "Franchise" });
                PropertyTypeList.Add(new SelectListItem { Text = "Restaurants", Value = "Restaurants" });
                PropertyTypeList.Add(new SelectListItem { Text = "Day Care", Value = "Day Care" });
                PropertyTypeList.Add(new SelectListItem { Text = "Hotels", Value = "Hotels" });
                PropertyTypeList.Add(new SelectListItem { Text = "Convenience stores", Value = "Convenience stores" });
                PropertyTypeList.Add(new SelectListItem { Text = "Start-Ups", Value = "Start-Ups" });
                PropertyTypeList.Add(new SelectListItem { Text = "Retail", Value = "Retail" });
                PropertyTypeList.Add(new SelectListItem { Text = "Industrial", Value = "Industrial" });
                PropertyTypeList.Add(new SelectListItem { Text = "Self Storage", Value = "Self Storage" });
                PropertyTypeList.Add(new SelectListItem { Text = "Others", Value = "Others" });

                ViewBag.PropertyTypes = PropertyTypeList;

                List<SelectListItem> PrescreenRequirementList = new List<SelectListItem>();

                PrescreenRequirementList.Add(new SelectListItem { Text = "2 years business tax returns", Value = "2 years business tax returns" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "2 years personal tax returns", Value = "2 years personal tax returns" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "YTD financials", Value = "YTD financials" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "Personal Financial Statements", Value = "Personal Financial Statements" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "Property Summary (if relevant)", Value = "Property Summary (if relevant)" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "Property Rent Roll (if relevant)", Value = "Property Rent Roll (if relevant)" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "YTD financials", Value = "YTD financials" });

                ViewBag.PrescreenRequirements = PrescreenRequirementList;

                List<SelectListItem> StateList = new List<SelectListItem>();
                StateList.Add(new SelectListItem() { Text = "Alabama", Value = "Alabama" });
                StateList.Add(new SelectListItem() { Text = "Alaska", Value = "Alaska" });
                StateList.Add(new SelectListItem() { Text = "Arizona", Value = "Arizona" });
                StateList.Add(new SelectListItem() { Text = "Arkansas", Value = "Arkansas" });
                StateList.Add(new SelectListItem() { Text = "California", Value = "California" });
                StateList.Add(new SelectListItem() { Text = "Colorado", Value = "Colorado" });
                StateList.Add(new SelectListItem() { Text = "Connecticut", Value = "Connecticut" });
                StateList.Add(new SelectListItem() { Text = "Delaware", Value = "Delaware" });
                StateList.Add(new SelectListItem() { Text = "District of Columbia", Value = "District of Columbia" });
                StateList.Add(new SelectListItem() { Text = "Florida", Value = "Florida" });
                StateList.Add(new SelectListItem() { Text = "Georgia", Value = "Georgia" });
                StateList.Add(new SelectListItem() { Text = "Hawaii", Value = "Hawaii" });
                StateList.Add(new SelectListItem() { Text = "Idaho", Value = "Idaho" });
                StateList.Add(new SelectListItem() { Text = "Illinois", Value = "Illinois" });
                StateList.Add(new SelectListItem() { Text = "Indiana", Value = "Indiana" });
                StateList.Add(new SelectListItem() { Text = "Iowa", Value = "Iowa" });
                StateList.Add(new SelectListItem() { Text = "Kansas", Value = "Kansas" });
                StateList.Add(new SelectListItem() { Text = "Kentucky", Value = "Kentucky" });
                StateList.Add(new SelectListItem() { Text = "Louisiana", Value = "Louisiana" });
                StateList.Add(new SelectListItem() { Text = "Maine", Value = "Maine" });
                StateList.Add(new SelectListItem() { Text = "Maryland", Value = "Maryland" });
                StateList.Add(new SelectListItem() { Text = "Massachusetts", Value = "Massachusetts" });
                StateList.Add(new SelectListItem() { Text = "Michigan", Value = "Michigan" });
                StateList.Add(new SelectListItem() { Text = "Minnesota", Value = "Minnesota" });
                StateList.Add(new SelectListItem() { Text = "Mississippi", Value = "Mississippi" });
                StateList.Add(new SelectListItem() { Text = "Missouri", Value = "Missouri" });
                StateList.Add(new SelectListItem() { Text = "Montana", Value = "Montana" });
                StateList.Add(new SelectListItem() { Text = "Nebraska", Value = "Nebraska" });
                StateList.Add(new SelectListItem() { Text = "Nevada", Value = "Nevada" });
                StateList.Add(new SelectListItem() { Text = "New Hampshire", Value = "New Hampshire" });
                StateList.Add(new SelectListItem() { Text = "New Jersey", Value = "New Jersey" });
                StateList.Add(new SelectListItem() { Text = "New Mexico", Value = "New Mexico" });
                StateList.Add(new SelectListItem() { Text = "New York", Value = "New York" });
                StateList.Add(new SelectListItem() { Text = "North Carolina", Value = "North Carolina" });
                StateList.Add(new SelectListItem() { Text = "North Dakota", Value = "North Dakota" });
                StateList.Add(new SelectListItem() { Text = "Ohio", Value = "Ohio" });
                StateList.Add(new SelectListItem() { Text = "Oklahoma", Value = "Oklahoma" });
                StateList.Add(new SelectListItem() { Text = "Oregon", Value = "Oregon" });
                StateList.Add(new SelectListItem() { Text = "Pennsylvania", Value = "Pennsylvania" });
                StateList.Add(new SelectListItem() { Text = "Rhode Island", Value = "Rhode Island" });
                StateList.Add(new SelectListItem() { Text = "South Carolina", Value = "South Carolina" });
                StateList.Add(new SelectListItem() { Text = "South Dakota", Value = "South Dakota" });
                StateList.Add(new SelectListItem() { Text = "Tennessee", Value = "Tennessee" });
                StateList.Add(new SelectListItem() { Text = "Texas", Value = "Texas" });
                StateList.Add(new SelectListItem() { Text = "Utah", Value = "Utah" });
                StateList.Add(new SelectListItem() { Text = "Vermont", Value = "Vermont" });
                StateList.Add(new SelectListItem() { Text = "Virginia", Value = "Virginia" });
                StateList.Add(new SelectListItem() { Text = "Washington", Value = "Washington" });
                StateList.Add(new SelectListItem() { Text = "West Virginia", Value = "West Virginia" });
                StateList.Add(new SelectListItem() { Text = "Wisconsin", Value = "Wisconsin" });
                StateList.Add(new SelectListItem() { Text = "Wyoming", Value = "Wyoming" });

                ViewBag.StateList = StateList;
                return View();
                //return View("_LenderConsoleNew");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult LenderConsoleNew()
        {
            try
            {
                List<SelectListItem> InstitutionTypeList = new List<SelectListItem>();
                InstitutionTypeList.Add(new SelectListItem { Text = "Direct Lender", Value = "Direct Lender" });
                InstitutionTypeList.Add(new SelectListItem { Text = "Broker", Value = "Broker" });

                ViewBag.InstitutionTypeList = InstitutionTypeList;

                List<SelectListItem> ProgrameTypeList = new List<SelectListItem>();
                ProgrameTypeList.Add(new SelectListItem { Text = "SBA / Conventional", Value = "SBA / Conventional" });
                ProgrameTypeList.Add(new SelectListItem { Text = "Alternative / Equity", Value = "Alternative / Equity" });

                ViewBag.ProgramTypes = ProgrameTypeList;

                List<SelectListItem> ProgramesList = new List<SelectListItem>();
                ProgramesList.Add(new SelectListItem { Text = "Program 1", Value = "Program 1" });
                ProgramesList.Add(new SelectListItem { Text = "Program 2", Value = "Program 2" });
                ProgramesList.Add(new SelectListItem { Text = "Program 3", Value = "Program 3" });
                ProgramesList.Add(new SelectListItem { Text = "Program 4", Value = "Program 4" });
                ProgramesList.Add(new SelectListItem { Text = "Program 5", Value = "Program 5" });

                ViewBag.Programs = ProgramesList;

                List<SelectListItem> PropertyTypeList = new List<SelectListItem>();

                PropertyTypeList.Add(new SelectListItem { Text = "Office", Value = "Office" });
                PropertyTypeList.Add(new SelectListItem { Text = "Office Condo", Value = "Office Condo" });
                PropertyTypeList.Add(new SelectListItem { Text = "Medical / Dental", Value = "Medical / Dental" });
                PropertyTypeList.Add(new SelectListItem { Text = "Warehouse", Value = "Warehouse" });
                PropertyTypeList.Add(new SelectListItem { Text = "Mixed Use", Value = "Mixed Use" });
                PropertyTypeList.Add(new SelectListItem { Text = "Multi Family", Value = "Multi Family" });
                PropertyTypeList.Add(new SelectListItem { Text = "Working Capital", Value = "Working Capital" });
                PropertyTypeList.Add(new SelectListItem { Text = "Equipment", Value = "Equipment" });
                PropertyTypeList.Add(new SelectListItem { Text = "Franchise", Value = "Franchise" });
                PropertyTypeList.Add(new SelectListItem { Text = "Restaurants", Value = "Restaurants" });
                PropertyTypeList.Add(new SelectListItem { Text = "Day Care", Value = "Day Care" });
                PropertyTypeList.Add(new SelectListItem { Text = "Hotels", Value = "Hotels" });
                PropertyTypeList.Add(new SelectListItem { Text = "Convenience stores", Value = "Convenience stores" });
                PropertyTypeList.Add(new SelectListItem { Text = "Start-Ups", Value = "Start-Ups" });
                PropertyTypeList.Add(new SelectListItem { Text = "Retail", Value = "Retail" });
                PropertyTypeList.Add(new SelectListItem { Text = "Industrial", Value = "Industrial" });
                PropertyTypeList.Add(new SelectListItem { Text = "Self Storage", Value = "Self Storage" });
                PropertyTypeList.Add(new SelectListItem { Text = "Others", Value = "Others" });

                ViewBag.PropertyTypes = PropertyTypeList;

                List<SelectListItem> PrescreenRequirementList = new List<SelectListItem>();

                PrescreenRequirementList.Add(new SelectListItem { Text = "2 years business tax returns", Value = "2 years business tax returns" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "2 years personal tax returns", Value = "2 years personal tax returns" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "YTD financials", Value = "YTD financials" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "Personal Financial Statements", Value = "Personal Financial Statements" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "Property Summary (if relevant)", Value = "Property Summary (if relevant)" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "Property Rent Roll (if relevant)", Value = "Property Rent Roll (if relevant)" });
                PrescreenRequirementList.Add(new SelectListItem { Text = "YTD financials", Value = "YTD financials" });

                ViewBag.PrescreenRequirements = PrescreenRequirementList;

                List<SelectListItem> StateList = new List<SelectListItem>();
                StateList.Add(new SelectListItem() { Text = "Alabama", Value = "Alabama" });
                StateList.Add(new SelectListItem() { Text = "Alaska", Value = "Alaska" });
                StateList.Add(new SelectListItem() { Text = "Arizona", Value = "Arizona" });
                StateList.Add(new SelectListItem() { Text = "Arkansas", Value = "Arkansas" });
                StateList.Add(new SelectListItem() { Text = "California", Value = "California" });
                StateList.Add(new SelectListItem() { Text = "Colorado", Value = "Colorado" });
                StateList.Add(new SelectListItem() { Text = "Connecticut", Value = "Connecticut" });
                StateList.Add(new SelectListItem() { Text = "Delaware", Value = "Delaware" });
                StateList.Add(new SelectListItem() { Text = "District of Columbia", Value = "District of Columbia" });
                StateList.Add(new SelectListItem() { Text = "Florida", Value = "Florida" });
                StateList.Add(new SelectListItem() { Text = "Georgia", Value = "Georgia" });
                StateList.Add(new SelectListItem() { Text = "Hawaii", Value = "Hawaii" });
                StateList.Add(new SelectListItem() { Text = "Idaho", Value = "Idaho" });
                StateList.Add(new SelectListItem() { Text = "Illinois", Value = "Illinois" });
                StateList.Add(new SelectListItem() { Text = "Indiana", Value = "Indiana" });
                StateList.Add(new SelectListItem() { Text = "Iowa", Value = "Iowa" });
                StateList.Add(new SelectListItem() { Text = "Kansas", Value = "Kansas" });
                StateList.Add(new SelectListItem() { Text = "Kentucky", Value = "Kentucky" });
                StateList.Add(new SelectListItem() { Text = "Louisiana", Value = "Louisiana" });
                StateList.Add(new SelectListItem() { Text = "Maine", Value = "Maine" });
                StateList.Add(new SelectListItem() { Text = "Maryland", Value = "Maryland" });
                StateList.Add(new SelectListItem() { Text = "Massachusetts", Value = "Massachusetts" });
                StateList.Add(new SelectListItem() { Text = "Michigan", Value = "Michigan" });
                StateList.Add(new SelectListItem() { Text = "Minnesota", Value = "Minnesota" });
                StateList.Add(new SelectListItem() { Text = "Mississippi", Value = "Mississippi" });
                StateList.Add(new SelectListItem() { Text = "Missouri", Value = "Missouri" });
                StateList.Add(new SelectListItem() { Text = "Montana", Value = "Montana" });
                StateList.Add(new SelectListItem() { Text = "Nebraska", Value = "Nebraska" });
                StateList.Add(new SelectListItem() { Text = "Nevada", Value = "Nevada" });
                StateList.Add(new SelectListItem() { Text = "New Hampshire", Value = "New Hampshire" });
                StateList.Add(new SelectListItem() { Text = "New Jersey", Value = "New Jersey" });
                StateList.Add(new SelectListItem() { Text = "New Mexico", Value = "New Mexico" });
                StateList.Add(new SelectListItem() { Text = "New York", Value = "New York" });
                StateList.Add(new SelectListItem() { Text = "North Carolina", Value = "North Carolina" });
                StateList.Add(new SelectListItem() { Text = "North Dakota", Value = "North Dakota" });
                StateList.Add(new SelectListItem() { Text = "Ohio", Value = "Ohio" });
                StateList.Add(new SelectListItem() { Text = "Oklahoma", Value = "Oklahoma" });
                StateList.Add(new SelectListItem() { Text = "Oregon", Value = "Oregon" });
                StateList.Add(new SelectListItem() { Text = "Pennsylvania", Value = "Pennsylvania" });
                StateList.Add(new SelectListItem() { Text = "Rhode Island", Value = "Rhode Island" });
                StateList.Add(new SelectListItem() { Text = "South Carolina", Value = "South Carolina" });
                StateList.Add(new SelectListItem() { Text = "South Dakota", Value = "South Dakota" });
                StateList.Add(new SelectListItem() { Text = "Tennessee", Value = "Tennessee" });
                StateList.Add(new SelectListItem() { Text = "Texas", Value = "Texas" });
                StateList.Add(new SelectListItem() { Text = "Utah", Value = "Utah" });
                StateList.Add(new SelectListItem() { Text = "Vermont", Value = "Vermont" });
                StateList.Add(new SelectListItem() { Text = "Virginia", Value = "Virginia" });
                StateList.Add(new SelectListItem() { Text = "Washington", Value = "Washington" });
                StateList.Add(new SelectListItem() { Text = "West Virginia", Value = "West Virginia" });
                StateList.Add(new SelectListItem() { Text = "Wisconsin", Value = "Wisconsin" });
                StateList.Add(new SelectListItem() { Text = "Wyoming", Value = "Wyoming" });

                ViewBag.StateList = StateList;

                return View("_LenderConsoleNew");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("_LenderConsoleNew");
        }


        public ActionResult LenderEmailVerificationFromFrontEnd(int id, string email)
        {
            try
            {
                string LenderEmailAddress = email;
                int LenderID = id;
                string decryp_email = StringCipher.Base64Decode(LenderEmailAddress);
                DataSet result = new RegisterBAL().GetDataForLenderEmailVerification(decryp_email, id);
                string HashCode = result.Tables[0].Rows[0]["EmailVerification_TokenHash"].ToString();
                string TokenExp = CommonConstant.DecodeTokenToCheckExpiry(HashCode);

                if (TokenExp == "Token Expired" || result.Tables[0].Rows[0]["EmailVerification_TokenUsed"].ToString() == "True")
                {
                    ModelState.AddModelError("", "This reset password link is expired");
                }
                else
                {
                    LenderConsole lender = new LenderConsole
                    {
                        EmailVerification_DateTime = COMMON.DateFormatter.GetUtcDate()
                    };

                    int UpdateResult = new RegisterBAL().UpdateLenderInfo(lender.EmailVerification_DateTime.Value, decryp_email, LenderID);

                    if (UpdateResult > 0)
                    {
                        ModelState.AddModelError("", "Email Verification is Successfully Completed, please log in here.");
                        //return View("LenderLogin");
                        return new RedirectResult(WebConfigurationManager.AppSettings["LenderLoginMailVerify"]);
                    }

                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
            }

            return View("LenderEmailVerification_LinkExpired");
        }

        [HttpPost]
        [ActionName("LenderConsole")]
        public ActionResult LenderConsole_Post(LenderConsoleFrontEnd lenderConsole)
        {
            try
            {
                int result = 0;
                if (ModelState.IsValid)
                {
                    result = new LoanApplicationBAL().SaveLenderConsole(lenderConsole);
                }

                if (result == 0)
                {
                    LenderConsole MaxID = new LoanApplicationBAL().GetLenderConsoleMaxID();


                    var HashToken = CommonConstant.GenerateTokenWithTimeStamp();
                    lenderConsole.EmailVerification_TokenHash = HashToken;
                    var dt = lenderConsole.EmailVerification_TokenHashExpirationDate ?? COMMON.DateFormatter.GetUtcDate().AddHours(24);
                    int DataResult = new RegisterBAL().UpdateLendersTokenData(MaxID.ID, lenderConsole.Email, lenderConsole.EmailVerification_TokenHash, dt);
                    string parametres = StringCipher.Base64Encode(lenderConsole.Email);
                    string loginUrl = HttpContext.Request.Url.Scheme + "://" + HttpContext.Request.Url.Authority + this.Url.Action("LenderEmailVerificationFromFrontEnd", "Home", new { email = parametres, id = MaxID.ID });

                    //var loginUrl = ConfigurationManager.AppSettings["LenderLogin"];
                    var Email_sent = new RegisterBAL().SendEmail_ForNewLenderOnRegisteration(lenderConsole.Email,lenderConsole.Contact, loginUrl);
                    return Json(new { redirectUrl = Url.Action("LenderConsoleDetails", "Home"), MaxID.ID }, JsonRequestBehavior.AllowGet);
                }

                else
                {
                    return View("_LenderConsoleNew");
                }



                //return RedirectToAction("LenderConsoleDetails", new { MaxID.ID });

                //return Json(new { redirectUrl = Url.Action("Login", "Home") });



                //return View("WelcomeLendor");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }


        public ActionResult MAX_ID()
        {
            try
            {

                int MaxID = 0;// new LoanApplicationBAL().LenderConsoleMaxID();

                return Json(MaxID, JsonRequestBehavior.AllowGet);

                //return View("WelcomeLendor");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return null;
        }


        //public JsonResult IsUserExists(LenderConsoleFrontEnd lenderConsole)
        //{


        //    //check if any of the UserName matches the UserName specified in the Parameter using the ANY extension method.  
        //    return Json(!lenderConsole.Email.Any(x => x.UserName == UserName), JsonRequestBehavior.AllowGet);
        //}

        [Compress]
        public ActionResult Login()
        {
            try
            {
                if (TempData["SessionExpired"] != null && Convert.ToBoolean(TempData["SessionExpired"]))
                {
                    ViewBag.SessionExpired = "True";
                    TempData["SessionExpired"] = null;
                }
                ViewBag.Login = true;
                return View(new Login());

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }


        public ActionResult Login_mobile(string mobile)
        {
            try
            {

                ViewBag.Login = true;
                return View("Login");

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("Login");
        }

        [Compress]
        public ActionResult BusinessLogin()
        {
            try
            {
                ViewBag.Login = true;
                return View("BusinessLogin");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("BusinessLogin");
        }

        public ActionResult PartnerLogin()
        {
            try
            {
                ViewBag.Login = true;
                return View("PartnerLogin");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("PartnerLogin");
        }



        [HttpPost]
        [Compress]
        [ActionName("Login")]
        public ActionResult Login_Post(Login login)
        {
            try
            {
                if (Session["MobileApp"] == null) { Session["MobileApp"] = "web"; }
                if (!(Request.UrlReferrer is null) && (Request.UrlReferrer.ToString().Contains("mobile") || (Session["MobileApp"] != null && Session["MobileApp"].ToString() == "mobile")))
                {
                    Session["MobileApp"] = "mobile";
                }
                else if (!(Request.UrlReferrer is null) && (Request.UrlReferrer.ToString().Contains("mclue") || (Session["MobileApp"] != null && Session["MobileApp"].ToString() == "mclue")))
                {
                    Session["MobileApp"] = "mclue";
                }
                else { Session["MobileApp"] = "web"; }

                if (ModelState.IsValid)
                {
                    var result = new DataSet();
                    result = login.IsPartner == 1
                        ? new RegisterBAL().ValidatePartnerUser(login)
                        : new RegisterBAL().ValidateUser(login);

                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        if (login.IsPartner == 1)
                        {
                            Session["UserId"] = result.Tables[0].Rows[0]["Id"].ToString();
                            Session["CustEmail"] = result.Tables[0].Rows[0]["Email"].ToString();
                            Session["CustName"] = result.Tables[0].Rows[0]["PartnerName"].ToString();
                            Session["IsResetPasswordAllowed"] =
                                result.Tables[0].Rows[0]["IsAllowChange"].ToString();
                            Session["IsPartner"] = true;
                            Session["PartnerId"] = result.Tables[0].Rows[0]["PartnerId"].ToString();
                            Session["myTimeZoneId"] = login.TimeZoneId;
                            Session["myTimeZone"] = login.myTimeZone;
                            Session["myTZOffset"] = login.myTZOffset;

                            var userId = Convert.ToInt64(result.Tables[0].Rows[0]["Id"]);
                            var customerEmailAddress = result.Tables[0].Rows[0]["Email"].ToString();
                            var roleName = result.Tables[0].Rows[0]["RoleName"].ToString();
                            var timeZoneName = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x =>
                                x.StandardName == login.TimeZoneId || x.DaylightName == login.TimeZoneId)?.Id;
                            var utcDate = DateFormatter.GetCurrentUtcByTimeZone(timeZoneName);
                            new RegisterBAL().UpdateLastLoginDateTime(userId, customerEmailAddress, roleName,
                                utcDate);

                            new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserId"]), "in",
                                "the system", "logged", "Users", "Partner");
                        }
                        else
                        {
                            Session["CustID"] = result.Tables[0].Rows[0]["Cust_ID"].ToString();

                            Session["UserId"] = result.Tables[0].Rows[0]["Id"].ToString();
                            Session["CustEmail"] = result.Tables[0].Rows[0]["Email"].ToString();
                            Session["CustName"] = result.Tables[0].Rows[0]["OwnerName"].ToString();
                            Session["IsResetPasswordAllowed"] =
                                result.Tables[0].Rows[0]["IsAllowChange"].ToString();
                            Session["IsPartner"] = false;
                            Session["myTimeZoneId"] = login.TimeZoneId;
                            Session["myTimeZone"] = login.myTimeZone;
                            Session["myTZOffset"] = login.myTZOffset;

                            var customerId = Convert.ToInt64(result.Tables[0].Rows[0]["Id"]);
                            var customerEmail = result.Tables[0].Rows[0]["Email"].ToString();
                            var roleName = result.Tables[0].Rows[0]["RoleName"].ToString();

                            //var timeZoneName = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x => x.StandardName == login.TimeZoneId || x.DaylightName == login.TimeZoneId)?.Id;
                            //var utcDate = DateFormatter.GetCurrentUtcByTimeZone(timeZoneName);
                            var utcDate = DateTime.UtcNow;
                            new RegisterBAL().UpdateLastLoginDateTime(customerId, customerEmail, roleName, utcDate);

                            new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserId"]), "in",
                                "the system", "logged", "Users", "Users");
                        }


                        switch (login.AccessType.ToString().ToLower())
                        {
                            case "loanmantrabusiness":
                                Session["AccessType"] = "loanmantrabusiness";
                                break;
                            case "loanmantrapartner":
                                Session["AccessType"] = "loanmantrapartner";
                                break;
                            default:
                                Session["AccessType"] = "hca";
                                break;
                        }

                        ViewBag.Login = true;
                        return RedirectToAction("MyApplication");
                    }

                    ModelState.AddModelError("", "Email Address and password are incorrect.");
                    ViewBag.Login = false;
                }
                switch (login.AccessType.ToString().ToLower())
                {
                    case "loanmantrabusiness":
                        return View("BusinessLogin");
                    case "loanmantrapartner":
                        return View("PartnerLogin");
                }
            }
            catch (Exception ex)
            {
                var url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            return View();
        }


        [HttpPost]
        [Compress]
        [ActionName("LenderLogin")]
        public ActionResult LenderLogin_Post(Login login)
        {
            try
            {
                if (Session["MobileApp"] == null) { Session["MobileApp"] = "web"; }
                if (Request.UrlReferrer.ToString().Contains("mobile") || (Session["MobileApp"] != null && Session["MobileApp"].ToString() == "mobile"))
                {
                    Session["MobileApp"] = "mobile";
                }
                else if (Request.UrlReferrer.ToString().Contains("mclue") || (Session["MobileApp"] != null && Session["MobileApp"].ToString() == "mclue"))
                {
                    Session["MobileApp"] = "mclue";
                }
                else { Session["MobileApp"] = "web"; }

                if (ModelState.IsValid)
                {
                    DataSet result = new DataSet();

                    result = new RegisterBAL().ValidateLenderUser(login);

                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {


                        Session["UserId"] = result.Tables[0].Rows[0]["Id"].ToString();
                        Session["CustEmail"] = result.Tables[0].Rows[0]["Email"].ToString();
                        Session["RoleId"] = result.Tables[0].Rows[0]["RoleID"].ToString();
                        Session["RoleName"] = result.Tables[0].Rows[0]["RoleName"].ToString();
                        Session["Emp_Name"] = result.Tables[0].Rows[0]["Name"].ToString();

                        Session["IsLender"] = true;

                        long CustId = Convert.ToInt64(result.Tables[0].Rows[0]["Id"]);
                        string CustEmail = result.Tables[0].Rows[0]["Email"].ToString();
                        //new RegisterBAL().UpdateLastLoginDateTime(CustId, CustEmail);

                        new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserId"]), "in", "the system", "logged", "Users", "Users");


                        if (login.AccessType.ToString().ToLower() == "loanmantrabusiness")
                            Session["AccessType"] = "loanmantrabusiness";
                        else if (login.AccessType.ToString().ToLower() == "loanmantrapartner")
                            Session["AccessType"] = "loanmantrapartner";
                        else
                            Session["AccessType"] = "hca";

                        ViewBag.Login = true;
                        //return RedirectToAction("LenderConsoleDashboard");
                        return RedirectToAction("LaunchDashboard_Lender");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Email Address and password are incorrect.");
                        ViewBag.Login = false;
                    }
                }

                var errors = ModelState.Values.SelectMany(v => v.Errors);

                if (login.AccessType.ToString().ToLower() == "loanmantrabusiness")
                    return View("BusinessLogin");
                else if (login.AccessType.ToString().ToLower() == "loanmantrapartner")
                    return View("PartnerLogin");


            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            return View();
        }

        [Compress]
        public ActionResult ResetPassword()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult ResetPassword_Lender()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [Compress]
        public ActionResult ResetPassword_mobile(string mobile)
        {
            try
            {
                return View("ResetPassword");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("ResetPassword");
        }

        [Compress]
        public ActionResult ResetPasswordNew()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult ResetPasswordLink(string email)
        {
            try
            {
                string emailAddress = StringCipher.Base64Decode(email);

                ViewBag.EmailAddress = emailAddress;
                Session["ResetEmailAddress"] = emailAddress;
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        [ActionName("ResetPasswordLink")]
        public ActionResult ResetPasswordLink_Post(ResetPassword resetPassword)
        {
            try
            {
                ModelState.Remove("EmailAddresss");

                if (ModelState.IsValid)
                {
                    resetPassword.ApplicationType = "User";
                    resetPassword.EmailAddresss = Session["ResetEmailAddress"].ToString();
                    DataSet result = new RegisterBAL().ResetPasswordLink(resetPassword);
                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        var login = new Login();

                        #region Commented and updated by:- Ishan Kulshrestha. Ondate:-11/22/2019.
                        //if (result.Tables[0].Rows[0]["RoleId"].ToString() == "4")
                        //{
                        //    login.IsPartner = 1;
                        //}
                        //else
                        //{
                        //    login.IsPartner = 1;

                        //}
                        login.IsPartner = 1;
                        #endregion

                        DataSet dtBasicInfo = new LoanApplicationBAL().GetResetPasswordMessageLink(resetPassword.EmailAddresss);
                        if (dtBasicInfo != null && dtBasicInfo.Tables.Count > 0)
                        {
                            if (dtBasicInfo.Tables[0].Rows.Count > 0)
                            {
                                string applicationNumber = dtBasicInfo.Tables[0].Rows[0]["ApplicationNumber"].ToString();
                                string strBody = ResetPasswordSuccessfullyChanged(dtBasicInfo.Tables[0].Rows[0]["BusinessEmail"].ToString(), dtBasicInfo.Tables[0].Rows[0]["OwnerFirstName"].ToString(), applicationNumber, dtBasicInfo.Tables[0].Rows[0]["Password"].ToString());
                                string subject = "Loan Mantra™ Password Reset Confirmation";
                                new MailHelper().Send(dtBasicInfo.Tables[0].Rows[0]["BusinessEmail"].ToString(), strBody, subject, false, "Reset Password");
                                //results = "Sent successfully..";
                            }
                            //else { results = "User already active."; }
                        }

                        ModelState.AddModelError("", "Your password is sucessfully changed, please log in here.");
                        return View("Login", login);
                    }
                    else
                    {
                        ModelState.AddModelError("", "an error accured while resetting password, please try again.");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult ResetPassword_LenderLink(string email)
        {
            try
            {
                string emailAddress = StringCipher.Base64Decode(email);

                ViewBag.EmailAddress = emailAddress;
                Session["ResetEmailAddress"] = emailAddress;
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }


        [HttpPost]
        public ActionResult ResetPassword_LenderLink(ResetPassword resetPassword)
        {
            try
            {
                ModelState.Remove("EmailAddresss");

                if (ModelState.IsValid)
                {
                    resetPassword.ApplicationType = "Lender";
                    resetPassword.EmailAddresss = Session["ResetEmailAddress"].ToString();
                    DataSet result = new RegisterBAL().ResetPasswordLink(resetPassword);
                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {

                        DataSet dtBasicInfo = new LoanApplicationBAL().GetResetPasswordMessage_lenderLink(resetPassword.EmailAddresss);
                        if (dtBasicInfo != null && dtBasicInfo.Tables.Count > 0)
                        {
                            if (dtBasicInfo.Tables[0].Rows.Count > 0)
                            {

                                string strLenderBody = ResetPasswordSuccessfullyChanged_Lender(dtBasicInfo.Tables[0].Rows[0]["LenderEmail"].ToString(), dtBasicInfo.Tables[0].Rows[0]["LendingInstitution"].ToString());

                                string subject = "Loan Mantra™ Password Reset Confirmation";

                                new MailHelper().Send(dtBasicInfo.Tables[0].Rows[0]["LenderEmail"].ToString(), strLenderBody, subject, false, "Reset Password");


                            }

                        }

                        ModelState.AddModelError("", "Your password is sucessfully changed, please log in here.");
                        return View("LenderLogin");
                    }
                    else
                    {
                        ModelState.AddModelError("", "an error accured while resetting password, please try again.");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult ConfirmEmail(string email)
        {
            try
            {
                string emailAddress = StringCipher.Base64Decode(email);

                ViewBag.EmailAddress = emailAddress;
                Session["ConfirmEmailAddress"] = emailAddress;
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        [ActionName("ConfirmEmail")]
        public ActionResult ConfirmEmail_Post(ConfirmEmail resetPassword)
        {
            try
            {
                resetPassword.EmailAddresss = Session["ConfirmEmailAddress"].ToString();
                if (ModelState.IsValid)
                {
                    DataSet result = new RegisterBAL().ConfirmEmail(resetPassword);
                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        string strBody = ApplicationPending(resetPassword.EmailAddresss);
                        string subject = "Your application is one stop closer to your business growth!!!";
                        new MailHelper().Send(resetPassword.EmailAddresss, strBody, subject, false, "Email Confirmation");
                        return RedirectToAction("Login");
                    }
                    else
                    {
                        ModelState.AddModelError("", "an error accured while confirming your email, please try again.");
                    }
                }
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        [ActionName("ResetPassword")]
        public ActionResult ResetPassword_Post(ResetPassword resetPassword)
        {
            try
            {
                bool isAllowchnage = false;
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
                resetPassword.ApplicationType = "User";
                if (ModelState.IsValid)
                {
                    DataSet result = new RegisterBAL().ResetPassword(resetPassword);
                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {
                        foreach (DataRow dr in result.Tables[0].Rows)
                        {
                            if (!string.IsNullOrEmpty(dr["IsAllowChange"].ToString()))
                                isAllowchnage = Convert.ToBoolean(dr["IsAllowChange"]);
                        }
                        if (!isAllowchnage)
                        {
                            #region Updated By:- Ishan Kulshrestha. Ondate:- 12/14/2019.
                            ModelState.AddModelError("", "You are not allowed to reset the password.");
                            #endregion
                            return View();
                        }

                        // ModelState.AddModelError("", "We have sent password reset link to your registered email address");
                        string strBody = ResetPasswordTemplate(resetPassword.EmailAddresss, resetPassword.Name);
                        string subject = "Loan Mantra™ Password Reset Request";
                        new MailHelper().Send(resetPassword.EmailAddresss, strBody, subject, false, "Reset Password");

                        return View("ForgotPasswordConfirmation");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Email address not registered with us.");
                    }
                }
                return View();
                //return View("ResetPasswordNew");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
            //return View("ResetPasswordNew");
        }


        [HttpPost]
        public ActionResult ResetPassword_Lender(ResetPassword resetPassword)
        {
            try
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
                resetPassword.ApplicationType = "User";
                if (ModelState.IsValid)
                {
                    DataSet result = new RegisterBAL().ResetPasswordForLender(resetPassword);
                    if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                    {


                        string strBody = ResetPasswordTemplate(resetPassword.EmailAddresss, resetPassword.Name);
                        string subject = "Loan Mantra™ Password Reset Request";
                        new MailHelper().Send(resetPassword.EmailAddresss, strBody, subject, false, "Reset Password");

                        return View("ForgotPasswordConfirmation");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Email address not registered with us.");
                    }
                }
                return View();
                //return View("ResetPasswordNew");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
            //return View("ResetPasswordNew");
        }



        [HttpPost]

        public ActionResult UpdateBorrowerEmailAddress(int Cust_ID, string BorrowerEmail)
        {
            try
            {
                bool EmailVerificationStatus = false;

                string strBody = ResetBorrowerEmailTemplate(Cust_ID, BorrowerEmail);
                string subject = "Borrower Email Update Verification";
                new MailHelper().Send(BorrowerEmail, strBody, subject, false, "Basic Info | Borrower Email change request");

                LoanApplicationBAL obj = new LoanApplicationBAL();

                EmailVerificationStatus = obj.UpdateVerifiedValueForEmailChange(Cust_ID);


                return Json("Mail Sent Success", JsonRequestBehavior.AllowGet);


            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();

        }

        private string ResetBorrowerEmailTemplate(int CustomerID, string EmailID)
        {
            string UserSiteURL = WebConfigurationManager.AppSettings["MyUserSite"] + String.Format("/Home/UpdateBorrowerEmailAddressLink?email={0}&CustomerID={1}", StringCipher.Base64Encode(EmailID), CustomerID);
            StringBuilder sb = new StringBuilder();
            sb.Append("<p><h1>Borrower Email Change Request</h1></p>");
            sb.Append("<p>An Email Update within Loan Mantra has been requested for the application associated with this Customer.</p>");
            sb.Append("<p>Please click <a href=" + UserSiteURL + ">here</a> to verify the account to Update Business Email Address</p>");
            sb.Append("<p><I>If you did not request this Email Update, please reach out to our Credit Advisors by calling 855-700-BLUE (2583)</I></p>");
            sb.Append("<p>Thank You,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>1.855.700.BLUE (2583)</p>");



            return sb.ToString();
        }

        public ActionResult Register()
        {
            try
            {
                GetDropDownlist();
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        public ActionResult Register_mobile(string mobile)
        {
            try
            {
                GetDropDownlist();
                return View("Register");
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View("Register");
        }

        public ActionResult RegisterPartner()
        {
            try
            {
                GetDropDownlist();
                return View();
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }


        public ActionResult LogOff()
        {
            try
            {
                string viewName = string.Empty;
                if (Session["AccessType"] != null)
                {
                    if (Session["AccessType"].ToString() == "loanmantrabusiness")
                        viewName = "BusinessLogin";
                    else if (Session["AccessType"].ToString() == "loanmantrapartner")
                        viewName = "PartnerLogin";
                    else
                        viewName = "Login";
                }
                Session.Clear();
                Session.RemoveAll();
                Session.Abandon();
                ModelState.AddModelError("", "You are successfully logged off, please log in here.");
                ViewBag.Login = true;
                //return View("Login");
                return View(viewName);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        [ActionName("Register")]
        public ActionResult Register_Post(Register register)
        {

            try
            {
                GetDropDownlist();
                if (ModelState.AsEnumerable().Where(x => x.Value.Errors.Count > 0).Select(x => x.Key).FirstOrDefault() == "ID")
                {
                    ModelState.Remove("ID");
                }
                if (ModelState.IsValid)
                {

                    DataSet dsRegisterInfo = new RegisterBAL().SaveRegisterInfo(register);

                    #region Added by:- Ishan Kulshrestha. On date:- 10/22/2019.
                    int mcaId = 0;
                    if (dsRegisterInfo != null && dsRegisterInfo.Tables[0].Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dsRegisterInfo.Tables[0].Rows[0]["ErrorNumber"]) == 0)
                        {
                            if (register.BorrowDuration == "Less than 30 days")
                            {
                                mcaId = new RegisterBAL().SaveRegisterInfointoMCAInfo(register);
                            }
                        }
                    }

                    #endregion
                    if (dsRegisterInfo != null && dsRegisterInfo.Tables[0].Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dsRegisterInfo.Tables[0].Rows[0]["ErrorNumber"]) > 0)
                        {
                            TempData["Error"] = "Already registered";
                            ModelState.AddModelError("", "This email is already registered with us. Please login with this email or provide another email to register.");

                            return View("Register");
                        }

                        string recipientEmail = System.Configuration.ConfigurationManager.AppSettings["RecipientEmail"].ToString();
                        string applicationNumber = dsRegisterInfo.Tables[0].Rows[0]["ApplicationNumber"].ToString();

                        #region Updated By:- Ishan Kulshrestha. On date:- 10/22/2019.
                        string strBody;
                        if (mcaId > 0)
                        {
                            strBody = RegisterTemplate(register.EmailAddresss + "," + recipientEmail, register.FirstName, applicationNumber, register.BorrowDuration, mcaId.ToString());
                        }
                        else
                        {
                            strBody = RegisterTemplate(register.EmailAddresss + "," + recipientEmail, register.FirstName, applicationNumber, register.BorrowDuration, "0");
                        }
                        #endregion

                        string subject = "Welcome to Loan Mantra";
                        new MailHelper().Send(register.EmailAddresss, strBody, subject, false, "Application Registration| Welcome Email");

                        return View("WelcomeRegister");


                    }
                    else
                    {
                        ModelState.AddModelError("", "Error accured while saving register information");
                    }
                }

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            GetDropDownlist();
            return View("Register");
        }

        [HttpPost]
        [ActionName("RegisterPartner")]
        public ActionResult RegisterPartner_Post(Partners partner)
        {
            try
            {
                GetDropDownlist();
                if (ModelState.IsValid)
                {

                    DataSet dsRegisterInfo = new RegisterBAL().SaveRegisterPartnerInfo(partner);

                    if (dsRegisterInfo != null && dsRegisterInfo.Tables[0].Rows.Count > 0)
                    {
                        if (Convert.ToInt32(dsRegisterInfo.Tables[0].Rows[0]["ErrorNumber"]) > 0)
                        {
                            ModelState.AddModelError("", "This email is already register with us. Please login with this email or provide another email to register.");

                            return View("RegisterPartner");
                        }

                        int partnerId = Convert.ToInt32(dsRegisterInfo.Tables[0].Rows[0]["ApplicationNumber"].ToString());
                        string strBody = RegisterPartnerTemplate(partner.EmailAddress, partner.FirstName, partnerId);
                        string subject = "Welcome to Loan Mantra";
                        new MailHelper().Send(partner.EmailAddress, strBody, subject, false, "Application Registration | Welcome Email");

                        return View("WelcomeRegister");
                    }
                    else
                    {
                        ModelState.AddModelError("", "Error accured while saving register information");
                    }
                }

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            GetDropDownlist();
            return View("Register");
        }

        public string EncryptParameters(string ParameterStr)
        {
            var result = "";
            try
            {
                result = ParameterEncryption.EncryptParametersAtServerEnd(ParameterStr);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                Log.Logs(ex.Message);
            }

            return result;
        }

        private void GetDropDownlist()
        {
            try
            {
                List<Register> registerList = new LoanApplicationBAL().GetOtherBusinessInfo(Convert.ToInt32(Session["CustID"]), Convert.ToInt32(Session["CustID"]));
                ViewBag.registerList = registerList;
                //BorrowDurationList.Add(new SelectListItem
                //{
                //    Text = "1 week",
                //    Value = "ONE_WEEK",
                //});

                List<SelectListItem> BorrowDuration = new List<SelectListItem>();
                BorrowDuration.Add(new SelectListItem
                {
                    Text = "Less than 30 days",
                    Value = "Less than 30 days"
                });

                BorrowDuration.Add(new SelectListItem
                {
                    Text = "Greater than 30 days",
                    Value = "Greater than 30 days"
                });
                ViewBag.BorrowDurationList = BorrowDuration;


                List<SelectListItem> LoanReasonList = new List<SelectListItem>();
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Select Use of Proceeds",
                    Value = "Select Use of Proceeds",
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Acquire Land and / or Building",
                    Value = "Acquire Land and / or Building",
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Acquire Machinery / Equipment",
                    Value = "Acquire Machinery / Equipment"



                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "A/R Financing",
                    Value = "A/R Financing"
                });



                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Building Improvement or Remodeling",
                    Value = "Building Improvement or Remodeling"
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Partner Buyout",
                    Value = "Partner Buyout"
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Purchase Inventory",
                    Value = "Purchase Inventory"
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Refinance Existing Debt",
                    Value = "Refinance Existing Debt"
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Working Capital",
                    Value = "Working Capital"
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Commercial Real Estate Purchase",
                    Value = "Commercial Real Estate Purchase"

                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Commercial Real Estate Refinance",
                    Value = "Commercial Real Estate Refinance"
                });
                LoanReasonList.Add(new SelectListItem
                {
                    Text = "Other",
                    Value = "Other"
                });
                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Buying equipment",
                //    Value = "Buying equipment",
                //});
                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Remodeling/Expansion",
                //    Value = "Remodeling/Expansion"

                //});
                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Refinancing debt",
                //    Value = "Refinancing debt"
                //});

                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Hiring employees",
                //    Value = "Hiring employees"
                //});

                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Purchasing inventory",
                //    Value = "Purchasing inventory"
                //});

                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Marketing",
                //    Value = "Marketing"
                //});


                //LoanReasonList.Add(new SelectListItem
                //{
                //    Text = "Other",
                //    Value = "Other"
                //});

                ViewBag.LoanReasonList = LoanReasonList;

                List<SelectListItem> TypeOfBusnessList = new List<SelectListItem>();
                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "Select type of business",
                    Value = "Select type of business",
                });
                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "Sole Proprietor",
                    Value = "Sole Proprietor",
                });
                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "Limited Liability Company (LLC)",
                    Value = "Limited Liability Company (LLC)"

                });
                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "C-Corporation",
                    Value = "C-Corporation"
                });

                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "S-Corporation",
                    Value = "S-Corporation"
                });
                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "Partnership",
                    Value = "Partnership"
                });
                TypeOfBusnessList.Add(new SelectListItem
                {
                    Text = "Independent Contractor",
                    Value = "Independent Contractor"
                });



                ViewBag.TypeOfBusnessList = new SelectList(TypeOfBusnessList, "Text", "Value", "Select type of business");

                List<SelectListItem> StateList = new List<SelectListItem>();
                StateList.Add(new SelectListItem() { Text = "Alabama", Value = "Alabama" });
                StateList.Add(new SelectListItem() { Text = "Alaska", Value = "Alaska" });
                StateList.Add(new SelectListItem() { Text = "Arizona", Value = "Arizona" });
                StateList.Add(new SelectListItem() { Text = "Arkansas", Value = "Arkansas" });
                StateList.Add(new SelectListItem() { Text = "California", Value = "California" });
                StateList.Add(new SelectListItem() { Text = "Colorado", Value = "Colorado" });
                StateList.Add(new SelectListItem() { Text = "Connecticut", Value = "Connecticut" });
                StateList.Add(new SelectListItem() { Text = "Delaware", Value = "Delaware" });
                StateList.Add(new SelectListItem() { Text = "District of Columbia", Value = "District of Columbia" });
                StateList.Add(new SelectListItem() { Text = "Florida", Value = "Florida" });
                StateList.Add(new SelectListItem() { Text = "Georgia", Value = "Georgia" });
                StateList.Add(new SelectListItem() { Text = "Hawaii", Value = "Hawaii" });
                StateList.Add(new SelectListItem() { Text = "Idaho", Value = "Idaho" });
                StateList.Add(new SelectListItem() { Text = "Illinois", Value = "Illinois" });
                StateList.Add(new SelectListItem() { Text = "Indiana", Value = "Indiana" });
                StateList.Add(new SelectListItem() { Text = "Iowa", Value = "Iowa" });
                StateList.Add(new SelectListItem() { Text = "Kansas", Value = "Kansas" });
                StateList.Add(new SelectListItem() { Text = "Kentucky", Value = "Kentucky" });
                StateList.Add(new SelectListItem() { Text = "Louisiana", Value = "Louisiana" });
                StateList.Add(new SelectListItem() { Text = "Maine", Value = "Maine" });
                StateList.Add(new SelectListItem() { Text = "Maryland", Value = "Maryland" });
                StateList.Add(new SelectListItem() { Text = "Massachusetts", Value = "Massachusetts" });
                StateList.Add(new SelectListItem() { Text = "Michigan", Value = "Michigan" });
                StateList.Add(new SelectListItem() { Text = "Minnesota", Value = "Minnesota" });
                StateList.Add(new SelectListItem() { Text = "Mississippi", Value = "Mississippi" });
                StateList.Add(new SelectListItem() { Text = "Missouri", Value = "Missouri" });
                StateList.Add(new SelectListItem() { Text = "Montana", Value = "Montana" });
                StateList.Add(new SelectListItem() { Text = "Nebraska", Value = "Nebraska" });
                StateList.Add(new SelectListItem() { Text = "Nevada", Value = "Nevada" });
                StateList.Add(new SelectListItem() { Text = "New Hampshire", Value = "New Hampshire" });
                StateList.Add(new SelectListItem() { Text = "New Jersey", Value = "New Jersey" });
                StateList.Add(new SelectListItem() { Text = "New Mexico", Value = "New Mexico" });
                StateList.Add(new SelectListItem() { Text = "New York", Value = "New York" });
                StateList.Add(new SelectListItem() { Text = "North Carolina", Value = "North Carolina" });
                StateList.Add(new SelectListItem() { Text = "North Dakota", Value = "North Dakota" });
                StateList.Add(new SelectListItem() { Text = "Ohio", Value = "Ohio" });
                StateList.Add(new SelectListItem() { Text = "Oklahoma", Value = "Oklahoma" });
                StateList.Add(new SelectListItem() { Text = "Oregon", Value = "Oregon" });
                StateList.Add(new SelectListItem() { Text = "Pennsylvania", Value = "Pennsylvania" });
                StateList.Add(new SelectListItem() { Text = "Rhode Island", Value = "Rhode Island" });
                StateList.Add(new SelectListItem() { Text = "South Carolina", Value = "South Carolina" });
                StateList.Add(new SelectListItem() { Text = "South Dakota", Value = "South Dakota" });
                StateList.Add(new SelectListItem() { Text = "Tennessee", Value = "Tennessee" });
                StateList.Add(new SelectListItem() { Text = "Texas", Value = "Texas" });
                StateList.Add(new SelectListItem() { Text = "Utah", Value = "Utah" });
                StateList.Add(new SelectListItem() { Text = "Vermont", Value = "Vermont" });
                StateList.Add(new SelectListItem() { Text = "Virginia", Value = "Virginia" });
                StateList.Add(new SelectListItem() { Text = "Washington", Value = "Washington" });
                StateList.Add(new SelectListItem() { Text = "West Virginia", Value = "West Virginia" });
                StateList.Add(new SelectListItem() { Text = "Wisconsin", Value = "Wisconsin" });
                StateList.Add(new SelectListItem() { Text = "Wyoming", Value = "Wyoming" });

                ViewBag.StateList = StateList;

                List<SelectListItem> BusinessCategories = new List<SelectListItem>();
                //BusinessCategories.Add(new SelectListItem() { Text = "Select Business Category", Value = "Select Business Category" });
                //BusinessCategories.Add(new SelectListItem() { Text = "Restaurant", Value = "Restaurant" });
                //BusinessCategories.Add(new SelectListItem() { Text = "Construction", Value = "Contractor" });
                ////BusinessCategories.Add(new SelectListItem() { Text = "CPA", Value = "CPA" });
                //BusinessCategories.Add(new SelectListItem() { Text = "Gas Station", Value = "Gas Station" });                
                //BusinessCategories.Add(new SelectListItem() { Text = "Hotel", Value = "Hotel" });
                //BusinessCategories.Add(new SelectListItem() { Text = "Medical", Value = "Medical" });
                //BusinessCategories.Add(new SelectListItem() { Text = "Other", Value = "Other" });

                #region Updated by:- Ishan Kulshrestha. On date:- 11/29/2019.
                BusinessCategories.Add(new SelectListItem() { Text = "Select Business Category", Value = "--Select--" });
                BusinessCategories.Add(new SelectListItem() { Text = "Construction", Value = "Contractor" });
                BusinessCategories.Add(new SelectListItem() { Text = "Gas Station", Value = "Gas Station" });
                BusinessCategories.Add(new SelectListItem() { Text = "Hotel", Value = "Hotel" });
                BusinessCategories.Add(new SelectListItem() { Text = "Medical", Value = "Medical" });
                BusinessCategories.Add(new SelectListItem() { Text = "Restaurant", Value = "Restaurant" });
                BusinessCategories.Add(new SelectListItem() { Text = "Transportation", Value = "Transportation" });
                BusinessCategories.Add(new SelectListItem() { Text = "Others", Value = "Others" });
                #endregion
                ViewBag.BusinessCategoryList = new SelectList(BusinessCategories, "Text", "Value", "Select Business Category");

                GetApplicationStatus();
                List<SelectListItem> OTitles = new List<SelectListItem>();
                OTitles.Add(new SelectListItem() { Text = "President", Value = "President" });
                OTitles.Add(new SelectListItem() { Text = "Vice President", Value = "VicePresident" });
                OTitles.Add(new SelectListItem() { Text = "Secretary", Value = "Secretary" });
                OTitles.Add(new SelectListItem() { Text = "Treasurer", Value = "Treasurer" });
                OTitles.Add(new SelectListItem() { Text = "Others", Value = "Others" });
                OTitles.Add(new SelectListItem() { Text = "None", Value = "None" });
                ViewBag.OTitleList = OTitles;


                List<SelectListItem> VeteranList = new List<SelectListItem>();
                #region Updated By:- Ishan Kulshrestha. On date:- 10/07/2019.
                VeteranList.Add(new SelectListItem() { Text = "Non Veteran", Value = "1" });
                VeteranList.Add(new SelectListItem() { Text = "Veteran", Value = "2" });
                VeteranList.Add(new SelectListItem() { Text = "Service-Disabled Veteran", Value = "3" });
                VeteranList.Add(new SelectListItem() { Text = "Spouse of Veteran", Value = "5" });
                VeteranList.Add(new SelectListItem() { Text = "Not Disclosed", Value = "4" });
                #endregion
                ViewBag.VeteranList = VeteranList;

                List<SelectListItem> GenderList = new List<SelectListItem>();
                GenderList.Add(new SelectListItem() { Text = "Male", Value = "M" });
                GenderList.Add(new SelectListItem() { Text = "Female", Value = "F" });
                GenderList.Add(new SelectListItem() { Text = "Not Disclosed", Value = "N" });
                ViewBag.GenderList = GenderList;

                List<SelectListItem> RaceList = new List<SelectListItem>();
                RaceList.Add(new SelectListItem() { Text = "American Indian or Alaska Native", Value = "1" });
                RaceList.Add(new SelectListItem() { Text = "Asian", Value = "2" });
                RaceList.Add(new SelectListItem() { Text = "Black or African-American", Value = "3" });
                RaceList.Add(new SelectListItem() { Text = "Native Hawaiian or Pacific Islander", Value = "4" });
                RaceList.Add(new SelectListItem() { Text = "White", Value = "5" });
                RaceList.Add(new SelectListItem() { Text = "Not Disclosed", Value = "X" });
                ViewBag.RaceList = RaceList;


                List<SelectListItem> EthnicityList = new List<SelectListItem>();
                EthnicityList.Add(new SelectListItem() { Text = "Hispanic or Latino", Value = "H" });
                EthnicityList.Add(new SelectListItem() { Text = "Not Hispanic or Latino", Value = "N" });
                EthnicityList.Add(new SelectListItem() { Text = "Not Disclosed", Value = "X" });
                ViewBag.EthnicityList = EthnicityList;

                List<SelectListItem> TypeOfLoanList = new List<SelectListItem>();
                TypeOfLoanList.Add(new SelectListItem() { Text = "SBA", Value = "SBA" });
                TypeOfLoanList.Add(new SelectListItem() { Text = "Disaster", Value = "Disaster" });
                TypeOfLoanList.Add(new SelectListItem() { Text = "Student Loan", Value = "Student Loan" });

                ViewBag.TypeOfLoanList = TypeOfLoanList;

                List<SelectListItem> CurrentOrPastDelinquentList = new List<SelectListItem>();
                CurrentOrPastDelinquentList.Add(new SelectListItem() { Text = "Current", Value = "Yes" });
                CurrentOrPastDelinquentList.Add(new SelectListItem() { Text = "Past Delinquent", Value = "No" });

                ViewBag.CurrentOrPastDelinquentList = CurrentOrPastDelinquentList;
                List<SelectListItem> TypeOfTaxReturnForm = new List<SelectListItem>();
                TypeOfTaxReturnForm.Add(new SelectListItem() { Text = "1065", Value = "1065" });
                TypeOfTaxReturnForm.Add(new SelectListItem() { Text = "1120", Value = "1120" });
                TypeOfTaxReturnForm.Add(new SelectListItem() { Text = "1120S", Value = "1120S" });
                TypeOfTaxReturnForm.Add(new SelectListItem() { Text = "Others", Value = "" });

                ViewBag.TypeOfTaxReturnForm = TypeOfTaxReturnForm;
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                Logger.Error("GetDropDownlist() : " + ex.ToString());
            }
        }


        [ParameterDecryption]
        public ActionResult RegisterLink(string email, string app, bool isLessthanThirtyDays, int mcaId)
        {
            try
            {
                TempData["Initial"] = 1;
                GetDropDownlist();
                string emailAddress = email;
                string applicationNumber = app;

                DataSet result = new RegisterBAL().ConfirmRegistration(emailAddress, applicationNumber);
                if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    Session["CustID"] = result.Tables[0].Rows[0]["Cust_ID"].ToString();
                    Session["CustEmail"] = result.Tables[0].Rows[0]["Email"].ToString();
                    Session["UserId"] = result.Tables[0].Rows[0]["Id"].ToString();
                    Session["CustName"] = result.Tables[0].Rows[0]["OwnerName"].ToString();
                    Session["IsResetPasswordAllowed"] = result.Tables[0].Rows[0]["IsAllowChange"].ToString();
                    //return RedirectToAction("MyApplication");
                    //return RedirectToAction("ApplyLoan", new { email = StringCipher.Base64Encode(emailAddress), app = StringCipher.Base64Encode(applicationNumber) });
                    #region Updated By:- Ishan Kulshrestha. On date:- 10/22/2019.
                    string parametres = "";
                    if (isLessthanThirtyDays && mcaId > 0)
                    {
                        //return RedirectToAction("McaApplicationFromMail", "MCA", new { McaID = mcaId });
                        parametres = ParameterEncryption.EncryptParametersAtServerEnd("McaID=" + mcaId);
                        return RedirectToAction("McaApplicationFromMail", "MCA", new { q = parametres });
                    }
                    else
                    {
                        //RedirectToAction("ApplyLoan", new { appId = Convert.ToInt32(Session["CustID"]) });
                        string param = "appId=" + Convert.ToString(Session["CustID"]) + "";
                        parametres = ParameterEncryption.EncryptParametersAtServerEnd(param);
                        return RedirectToAction("ApplyLoan", new { q = parametres });
                    }
                    #endregion
                }
                else
                {
                    ModelState.AddModelError("", "Confirmation link is expired");
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        public ActionResult RegisterPartnerLink(string email, string app)
        {
            try
            {
                GetDropDownlist();
                string emailAddress = StringCipher.Base64Decode(email);
                string partnerId = StringCipher.Base64Decode(app);

                DataSet result = new RegisterBAL().ConfirmPartnerRegistration(emailAddress, partnerId);
                if (result != null && result.Tables.Count > 0 && result.Tables[0].Rows.Count > 0)
                {
                    StringBuilder sb = new StringBuilder();
                    sb.Append("<p>Thank you for choosing Loan Mantra.  Our partners are very important to us.</p>");
                    sb.Append("<p></p>");
                    sb.Append("<p>In the next few days, one of our advisors will call and walk you through our online portal.");
                    sb.Append("<p></p>");
                    sb.Append("<p>In the meantime, please do not hesitate to contact us.  Here at  Loan Mantra, we are always available to answer any questions or inquiries about our services. We look forward to talking with you!</p>");
                    sb.Append("<p></p>");
                    sb.Append("<p>Thank you and welcome again!</p>");
                    sb.Append("<p></p>");
                    sb.Append("<p>Sincerely,</p>");
                    sb.Append("<p>Team Loan Mantra</p>");
                    string subject = "Welcome to Loan Mantra";
                    new MailHelper().Send(emailAddress, sb.ToString(), subject, false, "Partner Registration | Welcome Email.");
                    return View("ThanksPartnerVerification");
                    //return RedirectToAction("ApplyLoan", new { email = StringCipher.Base64Encode(emailAddress), app = StringCipher.Base64Encode(applicationNumber) });
                }
                else
                {
                    ModelState.AddModelError("", "Confirmation link is expired");
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        private string RegisterTemplate(string email, string name, string applicationNumber, string BorrowDuration, string mcaId)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(name))
                sb.Append("<p>Welcome " + name + ",</p>");
            else
                sb.Append("<p>Welcome,</p>");
            sb.Append("<p>Thank you for choosing Loan Mantra to be a part of your big business plan!  Our goal is to help you achieve your ultimate business goals by guiding you step by step throughout the commercial loan process.  We consider ourselves to be a part of your business and look forward to further doing business with you.</p>");
            sb.Append("<p>Your Application Number is <b>" + applicationNumber + "</b></p>");
            #region Updated By:- Ishan Kulshrestha. On date:- 10/22/2019
            #region Updated By:- Sandeep Sharma. On date:- 12/24/2019
            string parametres = "";
            var url = Request.Url;
            //&& !url.ToString().Contains("localhost")
            //if (!Request.IsSecureConnection)
            //{
            //    var uri = new Uri(url.ToString().Replace("http", "https"));
            //    url = uri;
            //}
            if (BorrowDuration == "Less than 30 days" && mcaId != "")
            {
                parametres = ParameterEncryption.EncryptParametersAtServerEnd("email=" + email + "&app=" + applicationNumber + "&isLessthanThirtyDays=true&mcaId=" + mcaId + "");
                sb.Append("<p>Please click <a href=" + url + "Link?q=" + parametres + ">here</a> to complete your registration.</p>");
            }
            else
            {
                parametres = ParameterEncryption.EncryptParametersAtServerEnd("email=" + email + "&app=" + applicationNumber + "&isLessthanThirtyDays=false&mcaId=" + mcaId + "");
                sb.Append("<p>Please click <a href=" + url + "Link?q=" + parametres + " > here</a> to complete your registration.</p>");
            }
            #endregion
            #endregion
            sb.Append("<p>Please do not hesitate to contact our advisors. Here at Loan Mantra, we are always available to answer any questions or inquiries about our services.  We look forward to talking with you!</p>");
            sb.Append("<p>Thank you and welcome again!</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>1.855.700.BLUE (2583)</p>");
            return sb.ToString();
        }

        private string RegisterPartnerTemplate(string email, string name, int userId)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(name))
                sb.Append("<p>Welcome " + name + ",</p>");
            else
                sb.Append("<p>Welcome,</p>");
            sb.Append("<p>Thank you for choosing Loan Mantra to be a part of your big business plan!  Our goal is to help you achieve your ultimate business goals by guiding you step by step throughout the commercial loan process.  We consider ourselves to be a part of your business and look forward to further doing business with you.</p>");
            sb.Append("<p>Please click <a href=" + Request.Url + "Link?email=" + StringCipher.Base64Encode(email) + "&app=" + StringCipher.Base64Encode(userId.ToString()) + ">here</a> to complete your registration verification.</p>");
            sb.Append("<p>Please do not hesitate to contact our advisors. Here at  Loan Mantra, we are always available to answer any questions or inquiries about our services.  We look forward to talking with you!</p>");
            sb.Append("<p>Thank you and welcome again!</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>1.855.700.BLUE (2583)</p>");
            return sb.ToString();
        }

        private string ResetPasswordTemplate(string email, string name)
        {
            StringBuilder sb = new StringBuilder();
            //sb.Append("<p><h1>Password Reset</h1></p>");
            sb.Append("<p>A password reset message was sent to your email address. Please click the link in the message to reset your password.</p>");

            sb.Append("<p><a href=" + Request.Url + "Link?email=" + StringCipher.Base64Encode(email) + ">Reset Your Password</a></p>");
            sb.Append("<p>If you do not receive the password reset message within a few moments, please check your spam folder.</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>855.700.BLUE (2583)</p>");

            return sb.ToString();
        }

        private string ApplicationPending(string email)
        {
            StringBuilder sb = new StringBuilder();

            if (!string.IsNullOrWhiteSpace(email))
                sb.Append("<p>Dear " + email + ",</p>");
            else
                sb.Append("<p>Dear,</p>");
            sb.Append("<p>Thank you for starting a business loan application with Loan Mantra.  You are almost done!!  As soon as you are finished with your application, one of our advisors will contact you to review the application and potential options for your business’ financial needs.</p>");
            sb.Append("<p>If you have any more question, please contact us at info@loanmantra.com or 855-700-BLUE (2583).</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>1.855.700.BLUE (2583)</p>");
            return sb.ToString();
        }

        private string ContactUsContent(string email)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<p>Hello,</p>");
            sb.Append("<p>Thank you for your interest in Loan Mantra and our services. We have submitted your information to our advisors and you will be contacted shortly. </p>");
            sb.Append("<p>Over at Loan Mantra we work to make sure we can provide our customers and potential customers with the best possible solutions to their financial needs. We consider ourselves to be a part of our client’s business in which, we too, want to grow.</p>");
            sb.Append("<p>We look forward to speaking with you. </p>");
            sb.Append("<p>Best Regards,</p>");
            sb.Append("<p>Loan Mantra</p>");
            sb.Append("<p>1.855.700.BLUE (2583)</p>");
            return sb.ToString();
        }

        public PartialViewResult UploadDoc()
        {
            var fileName = string.Empty;
            //Checking no of files injected in Request object
            if (Request.Files.Count > 0)
            {
                try
                {
                    //  Get all files from Request object  
                    HttpFileCollectionBase files = Request.Files;
                    for (int i = 0; i < 1; i++)
                    {
                        //string path = AppDomain.CurrentDomain.BaseDirectory + "Uploads/";  
                        //string filename = Path.GetFileName(Request.Files[i].FileName);  

                        HttpPostedFileBase file = files[i];
                        string fname;

                        // Checking for Internet Explorer  
                        if (Request.Browser.Browser.ToUpper() == "IE" || Request.Browser.Browser.ToUpper() == "INTERNETEXPLORER")
                        {
                            string[] testfiles = file.FileName.Split(new char[] { '\\' });
                            fname = testfiles[testfiles.Length - 1];
                        }
                        else
                        {
                            fname = DateTime.Now.Ticks.ToString() + "_" + file.FileName;
                        }
                        fileName = fname;
                        // Get the complete folder path and store the file inside it.  
                        fname = Path.Combine(Server.MapPath("~/AppDocs/"), fname);
                        file.SaveAs(fname);
                    }
                    int CustID = Convert.ToInt32(Request.Form["custId"]); // Session["CustID"] != null ? Convert.ToInt32(Session["CustID"]) : 0;
                    var doc = new Documents();
                    doc.Name = fileName;
                    doc.Title = Convert.ToString(Request.Form["Title"]);
                    doc.Year = Convert.ToString(Request.Form["Year"]);
                    doc.Note = Convert.ToString(Request.Form["Note"]);
                    doc.IsSystemGenerated = Convert.ToInt32(Request.Form["IsSystemGenerated"]);
                    doc.Id = Convert.ToInt32(Request.Form["Id"]);
                    doc.Cust_Id = CustID;
                    var result = new RegisterBAL().SaveDocument(doc);
                    var businessWithOwnerDetails = new BusinessWithOwnerDetails();
                    var loanApplicationBAL = new LoanApplicationBAL();
                    businessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(CustID);
                    businessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(CustID);
                    businessWithOwnerDetails.DocumentList = result;
                    businessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                    businessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(CustID);
                    businessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(CustID);
                    ViewBag.CustId = CustID;
                    return PartialView("_DocList", businessWithOwnerDetails);
                }
                catch (Exception ex)
                {
                    string url = Request.Url.ToString();
                    new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public PartialViewResult GetDocList(int CustID)
        {
            int custID = Session["CustID"] != null ? Convert.ToInt32(Session["CustID"]) : 0;
            var businessWithOwnerDetails = new BusinessWithOwnerDetails();
            var loanApplicationBAL = new LoanApplicationBAL();
            businessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(CustID);
            businessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(CustID);
            businessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(CustID);
            businessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
            businessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(CustID);
            businessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(CustID);

            ViewBag.CustId = CustID;

            return PartialView("_DocList", businessWithOwnerDetails);
        }

        public PartialViewResult DeleteDoc(int id)
        {
            int CustID = Session["CustID"] != null ? Convert.ToInt32(Session["CustID"]) : 0;
            var businessWithOwnerDetails = new BusinessWithOwnerDetails();
            var loanApplicationBAL = new LoanApplicationBAL();
            businessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(CustID);
            businessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(CustID);
            businessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
            businessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(CustID);
            businessWithOwnerDetails.DocumentList = new RegisterBAL().DeleteDocument(id, CustID);
            businessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(CustID);

            ViewBag.CustId = CustID;
            return PartialView("_DocList", businessWithOwnerDetails);
        }

        //public ActionResult MyApplication()
        //{
        //    if (Session["UserId"] != null && Convert.ToInt32(Session["UserId"]) > 0)
        //    {
        //        int UserId = Convert.ToInt32(Session["UserID"]);
        //        List<LoanApplication> objLoanApplicationList = new LoanApplicationBAL().GetApplicationByUser(UserId, Convert.ToInt32(Session["PartnerId"]));
        //        return View(objLoanApplicationList);
        //    }
        //    else
        //    {
        //        return RedirectToAction("Login");
        //        //List<LoanApplication> objLoanApplicationList = new LoanApplicationBAL().GetRegisterInfo();
        //        //return View(objLoanApplicationList);
        //    }

        //}


        [Compress]
        public ActionResult MyApplication()
        {
            if (Session["UserId"] == null || Convert.ToInt32(Session["UserId"]) <= 0) return RedirectToAction("Login");
            var flag = new RegisterBAL().CheckChangepasswordFlag(Convert.ToInt32(Session["UserId"]));
            var emailId = new RegisterBAL().GetEmailIDByUserID(Convert.ToInt32(Session["UserId"]));
            ViewBag.emailId = emailId;
            if (Convert.ToString(flag) == "true")
            { ViewBag.changeflag1 = "true"; }
            var userId = Convert.ToInt32(Session["UserID"]);
            var objLoanApplicationList = new LoanApplicationBAL().GetApplicationByUser(userId, Convert.ToInt32(Session["PartnerId"]));
            return View(objLoanApplicationList);
        }

        public ActionResult LenderConsoleDashboard()
        {
            try
            {
                if (Session["UserId"] != null && Convert.ToInt32(Session["UserId"]) > 0)
                {
                    string LenderEmail_ID = new RegisterBAL().GetLenderEmailIDByUserID(Convert.ToInt32(Session["UserId"]));
                    ViewBag.emailId = LenderEmail_ID;

                    int UserId = Convert.ToInt32(Session["UserID"]);
                    List<LoanApplication> objLoanApplicationList = new LoanApplicationBAL().GetRegisterInfoByLender(UserId);
                    return View(objLoanApplicationList);
                }
                else
                {
                    return RedirectToAction("LenderLogin");

                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
                return null;
            }
        }


        public ActionResult LaunchDashboard_Lender()
        {
            try
            {
                if (Session["UserId"] != null && Convert.ToInt32(Session["UserId"]) > 0)
                {
                    string LenderEmail_ID = new RegisterBAL().GetLenderEmailIDByUserID(Convert.ToInt32(Session["UserId"]));
                    ViewBag.emailId = LenderEmail_ID;

                    int UserId = Convert.ToInt32(Session["UserID"]);
                    List<LoanApplication> objLoanApplicationList = new LoanApplicationBAL().GetRegisterInfoByLender(UserId);
                    return View(objLoanApplicationList);
                }
                else
                {
                    return RedirectToAction("LenderLogin");

                }
            }
            catch (Exception ex)
            {
                ex.Message.ToString();
                return null;
            }
        }


        public ActionResult CompanyInfo()
        {
            List<SelectListItem> StateList = new List<SelectListItem>();
            StateList.Add(new SelectListItem() { Text = "Alabama", Value = "Alabama" });
            StateList.Add(new SelectListItem() { Text = "Alaska", Value = "Alaska" });
            StateList.Add(new SelectListItem() { Text = "Arizona", Value = "Arizona" });
            StateList.Add(new SelectListItem() { Text = "Arkansas", Value = "Arkansas" });
            StateList.Add(new SelectListItem() { Text = "California", Value = "California" });
            StateList.Add(new SelectListItem() { Text = "Colorado", Value = "Colorado" });
            StateList.Add(new SelectListItem() { Text = "Connecticut", Value = "Connecticut" });
            StateList.Add(new SelectListItem() { Text = "Delaware", Value = "Delaware" });
            StateList.Add(new SelectListItem() { Text = "District of Columbia", Value = "District of Columbia" });
            StateList.Add(new SelectListItem() { Text = "Florida", Value = "Florida" });
            StateList.Add(new SelectListItem() { Text = "Georgia", Value = "Georgia" });
            StateList.Add(new SelectListItem() { Text = "Hawaii", Value = "Hawaii" });
            StateList.Add(new SelectListItem() { Text = "Idaho", Value = "Idaho" });
            StateList.Add(new SelectListItem() { Text = "Illinois", Value = "Illinois" });
            StateList.Add(new SelectListItem() { Text = "Indiana", Value = "Indiana" });
            StateList.Add(new SelectListItem() { Text = "Iowa", Value = "Iowa" });
            StateList.Add(new SelectListItem() { Text = "Kansas", Value = "Kansas" });
            StateList.Add(new SelectListItem() { Text = "Kentucky", Value = "Kentucky" });
            StateList.Add(new SelectListItem() { Text = "Louisiana", Value = "Louisiana" });
            StateList.Add(new SelectListItem() { Text = "Maine", Value = "Maine" });
            StateList.Add(new SelectListItem() { Text = "Maryland", Value = "Maryland" });
            StateList.Add(new SelectListItem() { Text = "Massachusetts", Value = "Massachusetts" });
            StateList.Add(new SelectListItem() { Text = "Michigan", Value = "Michigan" });
            StateList.Add(new SelectListItem() { Text = "Minnesota", Value = "Minnesota" });
            StateList.Add(new SelectListItem() { Text = "Mississippi", Value = "Mississippi" });
            StateList.Add(new SelectListItem() { Text = "Missouri", Value = "Missouri" });
            StateList.Add(new SelectListItem() { Text = "Montana", Value = "Montana" });
            StateList.Add(new SelectListItem() { Text = "Nebraska", Value = "Nebraska" });
            StateList.Add(new SelectListItem() { Text = "Nevada", Value = "Nevada" });
            StateList.Add(new SelectListItem() { Text = "New Hampshire", Value = "New Hampshire" });
            StateList.Add(new SelectListItem() { Text = "New Jersey", Value = "New Jersey" });
            StateList.Add(new SelectListItem() { Text = "New Mexico", Value = "New Mexico" });
            StateList.Add(new SelectListItem() { Text = "New York", Value = "New York" });
            StateList.Add(new SelectListItem() { Text = "North Carolina", Value = "North Carolina" });
            StateList.Add(new SelectListItem() { Text = "North Dakota", Value = "North Dakota" });
            StateList.Add(new SelectListItem() { Text = "Ohio", Value = "Ohio" });
            StateList.Add(new SelectListItem() { Text = "Oklahoma", Value = "Oklahoma" });
            StateList.Add(new SelectListItem() { Text = "Oregon", Value = "Oregon" });
            StateList.Add(new SelectListItem() { Text = "Pennsylvania", Value = "Pennsylvania" });
            StateList.Add(new SelectListItem() { Text = "Rhode Island", Value = "Rhode Island" });
            StateList.Add(new SelectListItem() { Text = "South Carolina", Value = "South Carolina" });
            StateList.Add(new SelectListItem() { Text = "South Dakota", Value = "South Dakota" });
            StateList.Add(new SelectListItem() { Text = "Tennessee", Value = "Tennessee" });
            StateList.Add(new SelectListItem() { Text = "Texas", Value = "Texas" });
            StateList.Add(new SelectListItem() { Text = "Utah", Value = "Utah" });
            StateList.Add(new SelectListItem() { Text = "Vermont", Value = "Vermont" });
            StateList.Add(new SelectListItem() { Text = "Virginia", Value = "Virginia" });
            StateList.Add(new SelectListItem() { Text = "Washington", Value = "Washington" });
            StateList.Add(new SelectListItem() { Text = "West Virginia", Value = "West Virginia" });
            StateList.Add(new SelectListItem() { Text = "Wisconsin", Value = "Wisconsin" });
            StateList.Add(new SelectListItem() { Text = "Wyoming", Value = "Wyoming" });

            ViewBag.StateList = StateList;
            return View();
        }

        public PartialViewResult GetCompanyInfo(string companyName, string state)
        {
            string url = string.Format("{0}?current_entity_name={1}&dos_process_state={2}", "https://data.ny.gov/resource/n9v6-gdp6.json", companyName, state);
            HttpWebRequest webrequest = (HttpWebRequest)WebRequest.Create(url);
            webrequest.Method = "GET";
            webrequest.ContentType = "application/x-www-form-urlencoded";
            //webrequest.Headers.Add("Username", "xyz");
            //webrequest.Headers.Add("Password", "abc");
            HttpWebResponse webresponse = (HttpWebResponse)webrequest.GetResponse();
            Encoding enc = System.Text.Encoding.GetEncoding("utf-8");
            StreamReader responseStream = new StreamReader(webresponse.GetResponseStream(), enc);
            string result = string.Empty;
            result = responseStream.ReadToEnd();
            webresponse.Close();
            var searchCompany = new List<CompanyInfo>();
            var serializer = new JavaScriptSerializer();
            searchCompany = Newtonsoft.Json.JsonConvert.DeserializeObject<List<CompanyInfo>>(result);
            var getSearchData = new CompanyInfo();
            if (searchCompany.Count > 0)
            {
                getSearchData = searchCompany.FirstOrDefault();
            }
            //viewModel.JsonData = serializer.Serialize(result);
            // return result;
            return PartialView("_CompanyInfo", getSearchData);
        }

        [HttpPost]
        public JsonResult makeCurrencyFormat(string text)
        {
            try
            {
                string val = "";
                val = Convert.ToDecimal(text).ToString("N", new CultureInfo("en-US")).ToString();
                return Json(val);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                return Json("Error:" + ex.Message);
            }
        }

        public PartialViewResult OtherInfo(int custID, int id1)
        {
            try
            {
                //LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);

                //objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                //objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                //Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                //Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                //Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                //if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                //{
                //    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                //    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                //}
                LoanApplication objLoanApplication = new LoanApplicationBAL().EditGetLoanApplication(id1);
                objLoanApplication.otherBasicInfo.CustID = custID;
                objLoanApplication.otherBasicInfo.Id = id1;
                GetDropDownlist();
                ViewBag.ajaxPartialLoad = "true";
                return PartialView("~/Views/Shared/_OtherInfo.cshtml", objLoanApplication.otherBasicInfo);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_OtherInfo.cshtml");
        }

        public PartialViewResult OtherBasicInfo(int id1, int custID)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().EditGetLoanApplication(id1);
                objLoanApplication.otherBasicInfo.CustID = custID;
                objLoanApplication.otherBasicInfo.Id = id1;
                //objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(custID, id);//.GetOtherBusinessInfo(id, id);
                //objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                //Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                //Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                //Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                //if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                //{
                //    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                //    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                //}
                ViewBag.ajaxPartialLoad = "true";
                GetDropDownlist();

                return PartialView("~/Views/Shared/_OtherInfo.cshtml", objLoanApplication.otherBasicInfo);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_OtherInfo.cshtml");
        }

        public PartialViewResult BasicInfo(int appId)
        {
            try
            {
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();

                return PartialView("~/Views/Shared/_BasicInfoEdit.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_BasicInfoEdit.cshtml");
        }

        public PartialViewResult OthersGrid(int id)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                GetDropDownlist();

                return PartialView("~/Views/Shared/_OtherInfoGrid.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_OtherInfoGrid.cshtml");
        }

        public PartialViewResult LoanAmount(int appId)
        {
            try
            {
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                ViewBag.ajaxPartialLoad = "true";
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();

                return PartialView("~/Views/Shared/_LoanAmountEdit.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_LoanAmountEdit.cshtml");
        }

        public PartialViewResult BusinessProfile(int appId)
        {
            try
            {
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                ViewBag.TypeOfBusiness = objLoanApplication.BasicInfo.TypeOfBusiness;
                objLoanApplication.BusinessProfile.BusinessType = objLoanApplication.BasicInfo.TypeOfBusiness;
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();
                ViewBag.ajaxPartialLoad = "true";
                return PartialView("~/Views/Shared/_BusinessProfileEdit.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_BusinessProfileEdit.cshtml");
        }

        public PartialViewResult BusinessHistory(int appId)
        {
            try
            {
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();

                return PartialView("~/Views/Shared/_BusinessHistoryEdit.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_BusinessHistoryEdit.cshtml");
        }

        public PartialViewResult PersonalResume(int appId)
        {
            try
            {
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();

                return PartialView("~/Views/Shared/_PersonalResume.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_PersonalResume.cshtml");
        }

        public PartialViewResult PersonalFinanceStatement(int appId)
        {
            try
            {
                Session["CustID"] = appId;
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                objLoanApplication.BusinessWithOwnerDetails = new LoanApplicationBAL().GetBusinessWithOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.OwnerInfoList = new LoanApplicationBAL().Pr_GetCustomerOwnerList(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                objLoanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(appId);
                objLoanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(appId);
                GetDropDownlist();

                return PartialView("~/Views/Shared/_PersonalFinanceStatementEdit.cshtml", objLoanApplication);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_PersonalFinanceStatementEdit.cshtml");
        }

        public PartialViewResult Notes(int appId)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(appId);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(appId, appId);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                //Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustID"] = appId;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                //GetDropDownlist();
                ViewBag.ApplicationStatus = objLoanApplication.BasicInfo.ApplicationStatus;
                objLoanApplication.Comment.Comments = DateFormatter.GetUpdatedLocalTimeZone(objLoanApplication.Comment.Comments, Session["myTimeZoneId"].ToString());
                return PartialView("~/Views/Shared/_Notes.cshtml", objLoanApplication.Comment);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_Notes.cshtml");
        }

        public PartialViewResult BusinessOwnerDetails(int id)
        {
            try
            {
                LoanApplication loanApplication = new LoanApplication();
                //var businessWithOwnerDetails = new BusinessWithOwnerDetails();
                var loanApplicationBAL = new LoanApplicationBAL();
                loanApplication.BusinessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(id);
                loanApplication.BusinessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(id);
                loanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(id);
                loanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(id);
                loanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
                loanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(id);
                loanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(id);
                ViewBag.CustId = id;
                GetDropDownlist();

                return PartialView("~/Views/Shared/_PartialGrid.cshtml", loanApplication.BusinessWithOwnerDetails);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_PartialGrid.cshtml");
        }

        //public PartialViewResult GetRequiredDocuments(int id)
        //{
        //    try
        //    {
        //        LoanApplication loanApplication = new LoanApplication();
        //        var loanApplicationBAL = new LoanApplicationBAL();
        //        loanApplication.BusinessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(id);
        //        loanApplication.otherBusinesses = loanApplicationBAL.GetOtherBusinessInfo(id, id);
        //        ViewBag.otherBusinesses = loanApplication.otherBusinesses;
        //        loanApplication.BusinessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(id);
        //        loanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(id);
        //        loanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(id);
        //        loanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();
        //        loanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(id);
        //        loanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(id);
        //        ViewBag.CustId = id;
        //        GetDropDownlist();
        //        loanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetBorrowerDocs(id);
        //        return PartialView("~/Views/Shared/_RequiredDocuments.cshtml", loanApplication.BusinessWithOwnerDetails);
        //    }
        //    catch (Exception ex)
        //    {
        //        
        //    }
        //    return PartialView("~/Views/Shared/_RequiredDocuments.cshtml");
        //}

        public PartialViewResult GetRequiredDocuments(int id)
        {
            try
            {
                LoanApplication loanApplication = new LoanApplication();
                var loanApplicationBAL = new LoanApplicationBAL();
                loanApplication.BusinessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(id);
                loanApplication.otherBusinesses = loanApplicationBAL.GetOtherBusinessInfo(id, id);
                ViewBag.otherBusinesses = loanApplication.otherBusinesses;
                loanApplication.BusinessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(id);
                loanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(id);
                loanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(id);
                loanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();

                loanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(id);
                loanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(id);

                loanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetAllDocs(id);

                loanApplication.BusinessWithOwnerDetails.AllBusinessesList = loanApplicationBAL.GetAllBusinessesList(id);
                loanApplication.BusinessWithOwnerDetails.AllOwnersList = loanApplicationBAL.GetAllOwnersList(id);

                ViewBag.CustId = id;
                GetDropDownlist();
                //loanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetBorrowerDocs(id);
                return PartialView("~/Views/Shared/_RequiredDocuments.cshtml", loanApplication.BusinessWithOwnerDetails);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_RequiredDocuments.cshtml");
        }

        public PartialViewResult GetAdvisorDocuments(int id)
        {
            try
            {
                LoanApplication loanApplication = new LoanApplication();
                var loanApplicationBAL = new LoanApplicationBAL();
                loanApplication.BusinessWithOwnerDetails = loanApplicationBAL.GetBusinessWithOwnerList(id);
                loanApplication.otherBusinesses = loanApplicationBAL.GetOtherBusinessInfo(id, id);
                ViewBag.otherBusinesses = loanApplication.otherBusinesses;
                loanApplication.BusinessWithOwnerDetails.OwnerInfoList = loanApplicationBAL.Pr_GetCustomerOwnerList(id);
                loanApplication.BusinessWithOwnerDetails.DocumentList = new RegisterBAL().GetDocuments(id);
                loanApplication.BusinessWithOwnerDetails.RequiredDocumentList = new RegisterBAL().GetRequiredDocuments(id);
                loanApplication.BusinessWithOwnerDetails.DocumentMstList = new RegisterBAL().GetAllDocuments();

                loanApplication.BusinessWithOwnerDetails.DocumentDetailsList = new RegisterBAL().GetCustomerSelectedlist(id);
                loanApplication.BusinessWithOwnerDetails.DocumentMstFileDetailsList = new RegisterBAL().GetCustomerUploadDocumentList(id);

                loanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetAllDocs(id);

                loanApplication.BusinessWithOwnerDetails.AllBusinessesList = loanApplicationBAL.GetAllBusinessesList(id);
                loanApplication.BusinessWithOwnerDetails.AllOwnersList = loanApplicationBAL.GetAllOwnersList(id);

                ViewBag.CustId = id;
                GetDropDownlist();
                //loanApplication.BusinessWithOwnerDetails.AllDocumentList = new RegisterBAL().GetBorrowerDocs(id);
                return PartialView("~/Views/Shared/_PartialGrid.cshtml", loanApplication.BusinessWithOwnerDetails);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_PartialGrid.cshtml");
        }

        public ActionResult DeleteApplicationById(int id)
        {
            var isDeleted = new LoanApplicationBAL().DeleteApplicationById(id);
            new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserID"]), "an application", "Successfully", "deleted", "Primary|Affiliates", "User Console");
            return Json(isDeleted);
        }

        [HttpPost]
        public JsonResult GetOtherBusinessInfo(int CustID, int LoanID)
        {
            List<Register> otherBusiness = new List<Register>();
            try
            {
                otherBusiness = new LoanApplicationBAL().GetOtherBusinessInfo(CustID, LoanID);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            return Json(new { result = otherBusiness });
        }

        public PartialViewResult Restaurant(int id)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                GetDropDownlist();

                return PartialView("~/Views/Shared/_Restaurant.cshtml", objLoanApplication.Restaurant);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_Restaurant.cshtml");
        }

        public PartialViewResult Contractor(int id)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                GetDropDownlist();

                return PartialView("~/Views/Shared/_Contractor.cshtml", objLoanApplication.Contractor);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_Contractor.cshtml");
        }

        public PartialViewResult Hotel(int id)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                GetDropDownlist();

                return PartialView("~/Views/Shared/_Hotel.cshtml", objLoanApplication.Hotel);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_Hotel.cshtml");
        }

        public PartialViewResult Medical(int id)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                GetDropDownlist();

                return PartialView("~/Views/Shared/_Medical.cshtml", objLoanApplication.Medical);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_Medical.cshtml");
        }

        public PartialViewResult GasStation(int id)
        {
            try
            {
                LoanApplication objLoanApplication = new LoanApplicationBAL().GetLoanApplication(id);
                objLoanApplication.otherBusinesses = new LoanApplicationBAL().GetOtherBusinessInfo(id, id);
                objLoanApplication.PersonalResume.CustID = objLoanApplication.BasicInfo.CustID ?? 0;
                Session["CustID"] = objLoanApplication.BasicInfo.CustID;
                Session["CustEmail"] = objLoanApplication.BasicInfo.BusinessEmail;
                Session["CustName"] = objLoanApplication.BusinessProfile.OwnerFirstName;
                if (objLoanApplication.BusinessProfile.TaxReturnForm != "1065" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120" && objLoanApplication.BusinessProfile.TaxReturnForm != "1120S")
                {
                    objLoanApplication.BusinessProfile.TaxReturnFormOthers = objLoanApplication.BusinessProfile.TaxReturnForm;
                    objLoanApplication.BusinessProfile.TaxReturnForm = "";
                }
                GetDropDownlist();

                return PartialView("~/Views/Shared/_GasStation.cshtml", objLoanApplication.GasStation);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("~/Views/Shared/_GasStation.cshtml");
        }

        [HttpPost]
        [ActionName("AddComments")]
        public ActionResult AddComments(Comment comment)
        {
            try
            {
                var timeZoneId = TimeZoneInfo.GetSystemTimeZones().FirstOrDefault(x =>
                    x.StandardName == Session["myTimeZoneId"].ToString() ||
                    x.DaylightName == Session["myTimeZoneId"].ToString())?.Id;
                var commentStr = comment.ApplicationComment;
                //if (ModelState.IsValid)
                //{
                comment.CommentedBy = Session["CustName"].ToString();
                comment.UserId =
                    Convert.ToInt32(Session["UserId"].ToString() == "" ? "0" : Session["UserId"].ToString());
                comment.CustID = Convert.ToInt32(Session["CustID"]);
                if (comment.ReminderDate.HasValue)
                {
                    var newDateTime = Convert.ToDateTime(comment.ReminderDate)
                        .Add(TimeSpan.Parse(comment.ReminderTime));
                    comment.ReminderDate = newDateTime.ToUniversalTime();

                    comment.ReminderTime = TimeZoneInfo
                        .ConvertTimeBySystemTimeZoneId(newDateTime, timeZoneId, "UTC").ToShortTimeString();
                }
                comment.CommentedOn = Convert.ToString(DateFormatter.GetCurrentUtcByTimeZone(timeZoneId));
                var result = new LoanApplicationBAL().SaveApplicationComments_new(comment);
                new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserID"]), "a comment", "Successfully",
                    "added", "Notes", "Application Comment: " + commentStr);
                //}

                comment.Comments = new LoanApplicationBAL().GetApplicationCommentByCustID(comment.CustID);
                ViewBag.ApplicationStatus = comment.CurrentStatus;
                comment.ApplicationStatus = comment.CurrentStatus;
                comment.Comments = DateFormatter.GetUpdatedLocalTimeZone(comment.Comments, Session["myTimeZoneId"].ToString());
                return PartialView("_Notes", comment);
            }
            catch (Exception ex)
            {
                var url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return View();
        }

        [HttpPost]
        public ActionResult DeleteComment(int CustID, int CommentID, string ApplicationComment)
        {
            try
            {
                if (new LoanApplicationBAL().DeleteCommentByCustID(CommentID, CustID))
                {
                    new LoanApplicationBAL().SaveLMLog(Convert.ToInt32(Session["UserID"]), "a comment", "Successfully", "deleted", "Notes", "Application Comment: " + ApplicationComment);
                    return Content("1");
                }

            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
            }
            return Content("0");
        }

        private void GetApplicationStatus()
        {
            List<SelectListItem> ApplicationStatusList = new List<SelectListItem>();

            //ApplicationStatusList.Add(new SelectListItem() { Text = "NEW", Value = "NEW" });
            //ApplicationStatusList.Add(new SelectListItem() { Text = "INPROGRESS", Value = "INPROGRESS" });
            //ApplicationStatusList.Add(new SelectListItem() { Text = "REVIEW REQUIRED", Value = "REVIEW REQUIRED" });
            //ApplicationStatusList.Add(new SelectListItem() { Text = "DOCUMENT REQUIRED", Value = "DOCUMENT REQUIRED" });
            //ApplicationStatusList.Add(new SelectListItem() { Text = "DECLINE", Value = "REJECT" });
            //ApplicationStatusList.Add(new SelectListItem() { Text = "COMPLETE", Value = "COMPLETE" });

            ApplicationStatusList.Add(new SelectListItem() { Text = "New", Value = "New" });
            ApplicationStatusList.Add(new SelectListItem() { Text = "In Progress", Value = "In Progress" });
            ApplicationStatusList.Add(new SelectListItem() { Text = "Documents Required", Value = "Documents Required" });
            ApplicationStatusList.Add(new SelectListItem() { Text = "Review Required", Value = "Review Required" });
            ApplicationStatusList.Add(new SelectListItem() { Text = "On Hold", Value = "On Hold" });
            ApplicationStatusList.Add(new SelectListItem() { Text = "Completed", Value = "Completed" });

            ApplicationStatusList.Add(new SelectListItem() { Text = "Closed", Value = "Closed" });

            ViewBag.ApplicationStatusLists = ApplicationStatusList;
        }

        public ActionResult ChangeAdminPassword(string oldPassword, string NewPassword, string id)
        {
            int result = 0;
            try
            {
                result = new RegisterBAL().savePassword(oldPassword, NewPassword, id, 2);



                DataSet dtBasicInfo = new LoanApplicationBAL().GetResetPasswordMessage(Convert.ToInt32(id));
                if (dtBasicInfo != null && dtBasicInfo.Tables.Count > 0)
                {
                    if (dtBasicInfo.Tables[0].Rows.Count > 0)
                    {
                        string applicationNumber = dtBasicInfo.Tables[0].Rows[0]["ApplicationNumber"].ToString();
                        string strBody = ResetPasswordSuccessfullyChanged(dtBasicInfo.Tables[0].Rows[0]["BusinessEmail"].ToString(), dtBasicInfo.Tables[0].Rows[0]["OwnerFirstName"].ToString(), applicationNumber, dtBasicInfo.Tables[0].Rows[0]["Password"].ToString());
                        string subject = "Loan Mantra™ Password Reset Confirmation";
                        new MailHelper().Send(dtBasicInfo.Tables[0].Rows[0]["BusinessEmail"].ToString(), strBody, subject, false, "Password Change Successfull");
                    }
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return Json(result, JsonRequestBehavior.AllowGet);



        }

        public ActionResult ChangeAdminPassword1(string oldPassword, string NewPassword, string id)
        {

            int result = new RegisterBAL().savePassword(oldPassword, NewPassword, id, 1);

            return Json(result, JsonRequestBehavior.AllowGet);

        }


        public ActionResult ChangeLenderPassword(string oldPassword, string NewPassword, string id)
        {
            int result = 0;
            try
            {
                result = new RegisterBAL().saveLenderPassword(oldPassword, NewPassword, id);

                DataSet dtBasicInfo = new LoanApplicationBAL().GetResetPasswordMessage_lender(Convert.ToInt32(id));
                if (dtBasicInfo != null && dtBasicInfo.Tables.Count > 0)
                {
                    if (dtBasicInfo.Tables[0].Rows.Count > 0)
                    {

                        string strLenderBody = ResetPasswordSuccessfullyChanged_Lender(dtBasicInfo.Tables[0].Rows[0]["LenderEmail"].ToString(), dtBasicInfo.Tables[0].Rows[0]["LendingInstitution"].ToString());

                        string subject = "Loan Mantra™ Password Reset Confirmation";

                        new MailHelper().Send(dtBasicInfo.Tables[0].Rows[0]["LenderEmail"].ToString(), strLenderBody, subject, false, "Password Change Successfull");
                    }
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return Json(result, JsonRequestBehavior.AllowGet);



        }


        public ActionResult CheckAllowToChangePassword(string userEmail)
        {

            int result = 0;
            try
            {
                result = new RegisterBAL().GetPasswdResetStatus(userEmail);
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }

            return Json(result, JsonRequestBehavior.AllowGet);

        }

        private string ResetPasswordSuccessfullyChanged(string email, string name, string applicationNumber, string strNewPassword)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(name))
            { sb.Append("<p>" + name + ":</p>"); }

            //sb.Append("<h1 style='text-align:left;'>The following email was sent to you by Administrator.</h1><br />");
            //sb.Append("<p>Apparently, you needed your password reset - So here it is: <br />");
            sb.Append("<p>Your password has been reset successfully.</p>");
            //sb.Append("New Password <b>" + strNewPassword + "</b><br />");
            //sb.Append("If you didn't request this, then - oops, you might wanna think about changing it, now. </b></p>");
            sb.Append("<p>If you did not request a password reset, please call customer service.</p>");
            sb.Append("<p>Thank You.</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>855.700.BLUE (2583)</p>");
            return sb.ToString();
        }

        private string ResetPasswordSuccessfullyChanged_Lender(string email, string name)
        {
            StringBuilder sb = new StringBuilder();
            if (!string.IsNullOrEmpty(name))
            { sb.Append("<p>" + name + ":</p>"); }

            //sb.Append("<h1 style='text-align:left;'>The following email was sent to you by Administrator.</h1><br />");
            //sb.Append("<p>Apparently, you needed your password reset - So here it is: <br />");
            sb.Append("<p>Your password has been reset successfully.</p>");
            //sb.Append("New Password <b>" + strNewPassword + "</b><br />");
            //sb.Append("If you didn't request this, then - oops, you might wanna think about changing it, now. </b></p>");
            sb.Append("<p>If you did not request a password reset, please call customer service.</p>");
            sb.Append("<p>Thank You.</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            sb.Append("<p>855.700.BLUE (2583)</p>");
            return sb.ToString();
        }

        public ActionResult SignedDocsMail()
        {
            LoanApplication LoanApplication = new LoanApplication();
            try
            {
                var AdvisorsList = new LoanApplicationBAL().GetLoanAdvisors();
                ViewBag.AdvisorsList = AdvisorsList;
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                
            }
            return PartialView("_SendSignedDocsMail", LoanApplication);
        }




        [HttpPost]
        public JsonResult SendEmailToSpouseForDocuSign(DataTable dtModel)
        {
            try
            {
                for (int i = 0; dtModel.Rows.Count > i; i++)
                {
                    List<Documents> listdocument = new List<Documents>();
                    string Email_Guid = EncodeString(Convert.ToString(Guid.NewGuid()));
                    var document = new Documents();
                    document.Email_GuId = Email_Guid;
                    document.EnvolopID = dtModel.Rows[i]["Envelope_Id"].ToString();
                    listdocument.Add(document);
                    new RegisterBAL().SaveUnSignedSpouseDocument(listdocument);
                    string redirectUrl = System.Configuration.ConfigurationManager.AppSettings["webUrl"].ToString() + "Download/UplodeDocusignSpouseDocument?custID=" + dtModel.Rows[i]["CUST_ID"].ToString() + "&Email_Guid=" + Email_Guid;
                    string strBody = SendEmailToSpouse(dtModel.Rows[i]["SpouseName"].ToString(), redirectUrl);
                    if (dtModel.Rows[i]["SpouseEmailId"].ToString() != "")
                    {
                        new MailHelper().Send(dtModel.Rows[i]["SpouseEmailId"].ToString(), strBody, "Spouse sign requied for these documents.", false, "Spause E-signature Email");
                    }
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                throw ex;
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public JsonResult SendEmailToOwnerForDocusign(DataTable dt)
        {
            try
            {
                for (int i = 0; dt.Rows.Count > i; i++)
                {

                    List<Documents> listdocument = new List<Documents>();
                    string Email_Guid = EncodeString(Convert.ToString(Guid.NewGuid()));
                    var document = new Documents();
                    document.Email_GuId = dt.Rows[i]["Email_GuId"].ToString();
                    document.EnvolopID = dt.Rows[i]["Envelope_Id"].ToString();
                    listdocument.Add(document);
                    new RegisterBAL().SaveUnSignedOwnerDocument(listdocument, Email_Guid);
                    string redirectUrl = System.Configuration.ConfigurationManager.AppSettings["webUrl"].ToString() + "Download/UplodeDocusignDocument?custID=" + dt.Rows[i]["Cust_ID"].ToString() + ",&Email_Guid=" + Email_Guid + ",&OwnerId=" + dt.Rows[i]["OwnerID"];
                    string strBody = SendEmailToOwner(dt.Rows[i]["OnwerName"].ToString(), redirectUrl);
                    new MailHelper().Send(dt.Rows[i]["Email_Id"].ToString(), strBody, "Owner sign requied for these documents.", false, "Owner E-Signature Email");
                }
            }
            catch (Exception ex)
            {
                string url = Request.Url.ToString();
                new LoanApplicationBAL().errorTracker(ex.Message, url, ex.StackTrace);
                throw ex;
            }
            return Json("", JsonRequestBehavior.AllowGet);
        }

        private string SendEmailToOwner(string UserName, string redirectUrl)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p>Hello,</p>");
            sb.Append("<p>You have also signed these document(s) with your business owner.</p>");
            sb.Append("<p> Please click <a href='" + redirectUrl + "'>here</a> to complete your sign request.</p>");
            sb.Append("<p>Thank You.</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            return sb.ToString();
        }



        private string SendEmailToSpouse(string UserName, string redirectUrl)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("<p>Hello,</p>");
            sb.Append("<p>You have also signed these document(s) with your spouse.</p>");
            sb.Append("<p> Please click <a href='" + redirectUrl + "'>here</a> to complete your sign request.</p>");
            sb.Append("<p>Thank You.</p>");
            sb.Append("<p>Sincerely,</p>");
            sb.Append("<p>Team Loan Mantra</p>");
            return sb.ToString();
        }

        public string EncodeString(string strID)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(strID));
        }

        public string DecodeString(string encodedstr)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(encodedstr));
        }
    }
}
