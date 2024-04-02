/* Annnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnnrag 
 * 
 * 
 * 
 * 
 * 
 * 
 * 
 * Annnnnnnnnnnnnnnnnnnnnnnnnnnnrag */





using HubstaffDemo.CustomFilter;

using HubstaffDemo.Data;
using HubstaffDemo.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IdentityModel.Metadata;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using System.Windows.Forms;

namespace HubstaffDemo.Controllers
{
    public class LoginController : Controller
    {
        private readonly ApplicationDbContext _context;
        public static CancellationTokenSource CancellationTokenSource;


        private static string screenshotFolderPath = @"C:\ScreenshortFile\";
        private static string totalHoursBasePath = @"C:\TotalHours\";
        public static object lockobject = new object();
        public LoginController()
        {
            _context = new ApplicationDbContext();
        }
        public ActionResult Index()
        {
          
            return View("Login");
        }
        public ActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public ActionResult Login(LoginData data)
        {
            string EnteredEmail = data.Email;
            string EnteredPassword = data.Password;
           // string UserType = data.Role;
          //need to add the role name so we can fixed the issue 

         var RoleVerification = _context.Roles.FirstOrDefault(x => x.Email == data.Email);
            //data = _context.Role.FirstOrDefault(u => u.Email == data.Email);
            if (RoleVerification != null)
            {
                switch (RoleVerification.Role)
                {
                    case "User":
                        var user = _context.Users.FirstOrDefault(p => p.Email == RoleVerification.Email);
                        if (user != null)
                        {
                            bool doesPasswordMatch = Crypto.VerifyHashedPassword(user.Password, data.Password);
                            if (doesPasswordMatch)
                            {
                                Session["Id"] = user.UId;
                                Session["LoginName"]=user.Name;
                                user.LoginTime = DateTime.Now;
                                //To store URL
                                Session["LoginTime"] = user.LoginTime;
                                //to get screen short when user is login 
                                CancellationTokenSource = new CancellationTokenSource();
                                Task.Run(() => CaptureScreenshotsPeriodically(user.UId, CancellationTokenSource.Token));
                                return RedirectToAction("UserDashboard", "Dashboard");
                            }
                            else
                            {
                                ViewBag.Error = "INVALID CREDENTIALS";
                            }
                        }
                        break;

                    case "Admin":
                        var admin = _context.Admin.FirstOrDefault(p => p.Email == RoleVerification.Email);
                        if (admin != null)
                        {
                            if (EnteredPassword == admin.Password)
                            {
                                Session["Id"] = admin.AId;
                                TempData["AdminID"] = admin.AId;
                                TempData.Keep();
                                return RedirectToAction("AdminDashboard", "Dashboard");
                            }
                            else
                            {
                                ViewBag.Error = "INVALID CREDENTIALS";
                            }
                        }
                        break;

                    case "Organization":
                        var superAdmin = _context.Organizations.FirstOrDefault(p => p.Email == RoleVerification.Email);
                        if (superAdmin != null)
                        {
                            bool doesPasswordMatch = Crypto.VerifyHashedPassword(superAdmin.Password, data.Password);
                            if (doesPasswordMatch)
                            {
                                Session["Id"] = superAdmin.Id;
                                TempData["superAdmin"] = superAdmin.Id;
                                TempData.Keep();
                                return RedirectToAction("OrganizationDashboard", "Dashboard");
                            }
                            else

                            {
                                ViewBag.Error = "INVALID CREDENTIALS";
                            }
                        }
                        break;

                    default:
                        return View();
                }
            }
            else
            {
                ViewBag.Error = "INVALID CREDENTIALS";
            }

            return View();
        }
        //To take the screen short 
        //private async Task CaptureScreenshotsPeriodically()
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            var screenshot = CaptureWebpageScreenshot();
        //            var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
        //            var filePath = Path.Combine(screenshotFolderPath, fileName);
        //            screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

        //            await Task.Delay(TimeSpan.FromMinutes(5));
        //        }
        //        catch (Exception ex)
        //        {
        //            Console.WriteLine($"Error capturing screenshot: {ex.Message}");
        //        }
        //    }
        //}
        private async Task CaptureScreenshotsPeriodically(int userId, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var screenshot = CaptureWebpageScreenshot();
                    var fileName = $"screenshot_{DateTime.Now:yyyyMMdd_HHmmss}.png";
                        // Get the user's admin's email
                        var user = _context.Users.FirstOrDefault(u => u.UId == userId);
                        var admin = _context.Organizations.FirstOrDefault(a => a.Id == user.Id);
                        var adminEmail = admin.Email;
                        var userEmail = user.Email;

                        // Create directories if they don't exist
                        var adminDirectoryPath = Path.Combine(screenshotFolderPath, adminEmail);
                        if (!Directory.Exists(adminDirectoryPath))
                        {
                            Directory.CreateDirectory(adminDirectoryPath);
                        }

                        var userDirectoryPath = Path.Combine(adminDirectoryPath, userEmail);
                        if (!Directory.Exists(userDirectoryPath))
                        {
                            Directory.CreateDirectory(userDirectoryPath);
                        }

                        var filePath = Path.Combine(userDirectoryPath, fileName);
                        screenshot.Save(filePath, System.Drawing.Imaging.ImageFormat.Png);

                        await Task.Delay(TimeSpan.FromSeconds(10));
                    

                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error capturing screenshot: {ex.Message}");
                }
            }
        }
        private Bitmap CaptureWebpageScreenshot()
        {
            int screenWidth = Screen.PrimaryScreen.Bounds.Width;
            int screenHeight = Screen.PrimaryScreen.Bounds.Height;
           
            var bmp = new Bitmap(screenWidth, screenHeight);

            using (var g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(0, 0, 0, 0, bmp.Size);
            }

            return bmp;
        }

        //For Logout
        [CustomExceptionFilter]
        public ActionResult Logout()
        {
            var user = new User();
            var StoredUserID = (int)Session["Id"];
            // string StoredUserLoginTime =(string)Session["LoginTime"];
            
            
            DateTime? storedUserLoginTime = Session["LoginTime"] as DateTime?;
            user.UId = StoredUserID;
            user.LoginTime = storedUserLoginTime;


            if (user != null && user.LoginTime.HasValue)
            {
                var storedElapsedTimeKey = $"{user.UId}_ElapsedMilliseconds";
                var sessionData = Session[storedElapsedTimeKey];
                Console.WriteLine($"Session Data: {sessionData}");

                if (long.TryParse(Convert.ToString(HttpContext.Session[storedElapsedTimeKey]), out long elapsedMilliseconds))
                {
                    user.ElapsedMilliseconds = elapsedMilliseconds;
                }
                else
                {
                    Console.WriteLine($"Could not parse elapsed time: {sessionData}");
                }

                user.LogoutTime = DateTime.Now;

                CalculateAndSaveTotalHours(user);

                // Clear the stored start time
                Response.Cookies["StoredStartTime"].Expires = DateTime.Now.AddDays(-1);

                // Set Session["Usertracking"] to null instead of Session["User"]
                Session["Usertracking"] = null;
                object mySessionVariable = Session["Usertracking"];
                if (mySessionVariable != null)
                {
                    // Output the value of the session variable
                    Response.Write("Session variable value: " + mySessionVariable.ToString());
                }
                var guidString = HttpContext.Session["TaskToken"].ToString();
                Debug.WriteLine($"TaskToken: {guidString}");
                if (guidString != null)
                {
                    var guid = Guid.Parse(guidString);

                    try
                    {
                        if (HttpContext.Session["TaskToken"] is Guid taskToken && DashboardController.TokenSources.TryGetValue(taskToken, out var cts))
                        {
                            cts.Cancel();
                            if (DashboardController.Listener != null)
                            {
                                DashboardController.Listener.Stop();
                                DashboardController.Listener.Close();
                                DashboardController.Listener = null;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }
                    if (CancellationTokenSource != null)
                    {
                        CancellationTokenSource.Cancel();
                        CancellationTokenSource = null;
                    }
                    Session.Clear();
                    Session.Abandon();
                    Session.RemoveAll();

                    return RedirectToAction("Login");
                }
                else
                {
                    return RedirectToAction("Login");
                }
            }
            return View("Login");
       }
        // for time updateding
        private TimeSpan ReadTotalHoursFromFile(string userName, TimeSpan totalHours)
        {
            string filePath = Server.MapPath("~/App_Data/totalHours.txt");

            try
            {
                var lines = System.IO.File.ReadAllLines(filePath);

                var userLine = lines.FirstOrDefault(line => line.Contains(userName));

                if (userLine != null)
                {
                    var totalHoursString = userLine.Split('-').LastOrDefault()?.Trim();

                    if (TimeSpan.TryParse(totalHoursString, out var TH))
                    {
                        return TH;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading total hours from file: {ex.Message}");
            }

            return TimeSpan.Zero;
        }

        private void CalculateAndSaveTotalHours(User user)
        {
            if (user.LoginTime.HasValue && user.LogoutTime.HasValue)
            {
                Console.WriteLine($"Debug - LoginTime: {user.LoginTime}, UserName:{user.Name},LogoutTime: {user.LogoutTime}");

                user.TotalHours = user.LogoutTime.Value - user.LoginTime.Value;
                user.TotalHours = TimeSpan.FromMilliseconds(user.ElapsedMilliseconds);
                SaveTotalHours(user.UId, user.TotalHours.ToString(@"hh\:mm\:ss"));
                // Save total hours to file
                //WriteTotalHoursToFile(user.Name, user.TotalHours);
            }
            else
            {
                Console.WriteLine($"Debug - LoginTime or LogoutTime is null");
            }
        }
        [HttpPost]
        public ActionResult UpdateTotalHours(TimeSpan? totalHours)
        {
            try
            {
                var user = new User();
                int StoredUserID = (int)Session["Id"];
                user.UId = StoredUserID;
                DateTime? storedUserLoginTime = Session["LoginTime"] as DateTime?;
                var LoginTime = user.LoginTime;
                if (user != null)
                {
                    if (totalHours.HasValue)
                    {
                        WriteTotalHoursToFile(user.Name, totalHours.Value);
                        
                    }
                    else
                    {
                        return Json("Total hours is null");
                    }
                }

                return Json("Invalid user");
            }
            catch (Exception ex)
            {
                // Log the exception for further analysis
                Console.WriteLine($"Exception in UpdateTotalHours: {ex.Message}");
                return Json("An error occurred");
            }
        }



        private void WriteTotalHoursToFile(string UID, TimeSpan totalHours )
        {
            string filePath = Server.MapPath("~/App_Data/totalHours.txt");

            //try
            //{
            //    string logEntry = $"{DateTime.Now}: {Name} - Total Hours: {totalHours}{Environment.NewLine}";
            //    System.IO.File.AppendAllText(filePath, logEntry);
            //    Console.WriteLine($"Total hours written to file: {logEntry}");
            //}
            try
            {
                string formattedTotalHours = totalHours.ToString(@"hh\:mm\:ss");

                string logEntry = $"{DateTime.Now}: USER ID - {UID} : Total Hours - {formattedTotalHours}{Environment.NewLine}";
                System.IO.File.AppendAllText(filePath, logEntry);
                Console.WriteLine($"Total hours written to file: {logEntry}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error writing total hours to file: {ex.Message}");
            }
        }

        //public ActionResult GetTotalHours()
        //{
        //    var user = new User();
        //    int StoredUserID = (int)Session["Id"];
        //    user.UId = StoredUserID;
        //    DateTime? storedUserLoginTime = Session["LoginTime"] as DateTime?;
        //    var LoginTime = user.LoginTime;
        //    if (user != null && user.TotalHours != null)
        //    {
        //        WriteTotalHoursToFile(user.Name, user.TotalHours);
        //        // return Json(user.TotalHours.ToString(@"hh\:mm\:ss") ?? "N/A", JsonRequestBehavior.AllowGet);

        //    }

        //    return Json("N/A", JsonRequestBehavior.AllowGet);
        //}
        //public ActionResult StartTracking()
        //{
        //    var user = new User();
        //    int StoredUserID = (int)Session["Id"];
        //    //string StoredUserLoginTime =(string)Session["_SessionUserTime"];
        //    user.UId = StoredUserID;
        //    DateTime? storedUserLoginTime = Session["LoginTime"] as DateTime?;
        //    var LoginTime = user.LoginTime;
        //    if (user != null)
        //    {
        //        var storedStartTime = Request.Cookies["StoredStartTime"];

        //        if (storedStartTime != null && DateTime.TryParse(storedStartTime.Value, out var startTime))
        //        {
        //            user.LoginTime = startTime;
        //        }
        //        else
        //        {
        //            user.LoginTime = user.LastLogoutTime ?? DateTime.Now;
        //            setStoredStartTime(user.LoginTime.Value);
        //        }

        //        return View("TrackedPage", user);
        //    }

        //    return RedirectToAction("Login");
        //}
        public ActionResult StartTracking()
        {
            var user = new User();
            int storedUserId = (int)Session["Id"];
            string storedUserLoginTime =(string) Session["_SessionUserTime"];
            user.UId = storedUserId;
            //user.LoginTime = DateTime.Parse(storedUserLoginTime);

            if (user != null)
            {
                // Check if there is a stored elapsed time for the day
                var storedElapsedTimeKey = $"{user.UId}_ElapsedMilliseconds";
                var storedElapsedTime = HttpContext.Session[storedElapsedTimeKey] ?? 0;

                // If there is stored elapsed time, update the user's elapsed time
                //check the working 
               // user.ElapsedMilliseconds = (long)storedElapsedTime;
                //long.TryParse((string)Session[storedElapsedTimeKey]
                if (long.TryParse((string)Session[storedElapsedTimeKey], out long elapsedMilliseconds))
                {
                    user.ElapsedMilliseconds = elapsedMilliseconds;
                }

                return View("TrackedPage", user);
            }

            return RedirectToAction("Login", "Login");
        }

        private void setStoredStartTime(DateTime time)
        {
            Response.Cookies["StoredStartTime"].Value = time.ToString("yyyy-MM-ddTHH:mm:ss");
        }

        public ActionResult UserInformation()
        {
            var user = new User();
            //Session["Id"] = user.UId;
            int StoredUserID = (int)Session["Id"];
            DateTime? storedUserLoginTime = Session["LoginTime"] as DateTime?;
            var LoginTime = user.LoginTime;
            user.UId = StoredUserID;
           // user.LoginTime = DateTime.Parse(StoredUserLoginTime);

            if (user == null)
            {
                // Redirect to login if not logged in
                return View("login");
            }

            // Check if there is a stored start time in sessionStorage
            var storedStartTime = Request.Cookies["StoredStartTime"];
            if (storedStartTime != null && DateTime.TryParse(storedStartTime.Value, out var startTime))
            {
                user.LoginTime = startTime;
            }

            ViewBag.DisableStartTracking = user.LogoutTime.HasValue;

            return View(user);
        }

        public ActionResult StopTracking(int elapsedMilliseconds)
        {
            var user = new User();
            int StoredUserID = (int)Session["Id"];
            user.UId = StoredUserID;


            //    if (user != null)
            //    {
            //        user.LogoutTime = DateTime.Now;
            //        CalculateAndSaveTotalHours(user);

            //        // Clear the stored start time
            //        Response.Cookies["StoredStartTime"].Expires = DateTime.Now.AddDays(-1);

            //        return RedirectToAction("UserDashboard", "Dashboard");
            //    }

            //    return RedirectToAction("Login");
            //}
            string userlogintime =(string)Session["_SessionUserTime"];
            //user.LoginTime = DateTime.Parse(userlogintime);

            
            if (user != null)
            {
                user.LogoutTime = DateTime.Now;

                user.ElapsedMilliseconds = elapsedMilliseconds;

                var storedElapsedTimeKey = $"{user.UId}_ElapsedMilliseconds";
             
                Session[storedElapsedTimeKey] = elapsedMilliseconds.ToString();
                return RedirectToAction("UserDashboard", "Dashboard");
            }

            return RedirectToAction("UserDashboard");
        }


            public ActionResult UpdateElapsedTime(long elapsedMilliseconds)
        {
            try
            {
                var user = new User();
                int StoredUserID = (int)Session["Id"];
                user.UId = StoredUserID;

                if (user != null)
                {
                    var totalHours = elapsedMilliseconds / (1000 * 60 * 60);
                    user.TotalHours += TimeSpan.FromHours(totalHours);

                    // Save the updated user object or update the total hours in your storage
                    // You may also want to log the elapsed time and total hours to a file or database

                    return Json("Elapsed time updated successfully.");
                }

                return Json("Invalid user");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception in UpdateElapsedTime: {ex.Message}");
                return Json("An error occurred");
            }
        }


        [HttpPost]
        public ActionResult SetElapsedTimeToSession(long elapsedMilliseconds)
        {
            var storedElapsedTimeKey = $"{(int)Session["_UserID"]}_ElapsedMilliseconds";
            // (string)Session[storedElapsedTimeKey, elapsedMilliseconds.ToString()];
            Session[storedElapsedTimeKey] = elapsedMilliseconds.ToString();

            return Content("");
        }
        private void SaveTotalHours(int userId, string totalHours)
        {
            // Get the user's admin and email
            var user = _context.Users.FirstOrDefault(u => u.UId == userId);
            var admin = _context.Organizations.FirstOrDefault(a => a.Id == user.Id);
            var adminEmail = admin.Email;
            var userEmail = user.Email;

            // Create directories if they don't exist
            var adminDirectoryPath = Path.Combine(totalHoursBasePath, adminEmail);
            if (!Directory.Exists(adminDirectoryPath))
            {
                Directory.CreateDirectory(adminDirectoryPath);
            }

            var userDirectoryPath = Path.Combine(adminDirectoryPath, userEmail);
            if (!Directory.Exists(userDirectoryPath))
            {
                Directory.CreateDirectory(userDirectoryPath);
            }

            // Create or append to the totalHours.txt file
            var filePath = Path.Combine(userDirectoryPath, "totalHours.txt");
            if (System.IO.File.Exists(filePath))
            {
                var allLines = System.IO.File.ReadAllLines(filePath).ToList();
                var lastLine = allLines.Last();
                Console.WriteLine(lastLine.Split(' ')[0]);
                Console.WriteLine(lastLine.Split(' ')[1]);
                Console.WriteLine(lastLine.Split(' ')[2]);
                var lastDate = DateTime.ParseExact(lastLine.Split(' ')[0] + " " + lastLine.Split(' ')[1] + " " + lastLine.Split(' ')[2], "dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture);
                if (lastDate.Date < DateTime.Now.Date)
                {
                    // Calculate total hours for the previous day
                    var previousDayLines = allLines.Where(line => DateTime.ParseExact(line.Split(' ')[0] + " " + line.Split(' ')[1] + " " + line.Split(' ')[2], "dd-MM-yyyy hh:mm:ss tt", CultureInfo.InvariantCulture).Date == lastDate.Date).ToList();
                    TimeSpan totalHoursPreviousDay = TimeSpan.Zero;
                    foreach (var line in previousDayLines)
                    {
                        var hours = TimeSpan.Parse(line.Split(new string[] { "Total Hours - " }, StringSplitOptions.None)[1].Trim());
                        totalHoursPreviousDay += hours;
                    }
                    // Add total hours of the previous day to the file
                    allLines.Add($"{lastDate:dd-MM-yyyy hh:mm:ss tt} : USER ID - {userId} : Total Hours of day = {totalHoursPreviousDay.ToString(@"hh\:mm\:ss")}");
                    System.IO.File.WriteAllLines(filePath, allLines);
                }
            }
            var entry = $"{DateTime.Now} : USER ID - {userId} : Total Hours - {totalHours}\n";
            System.IO.File.AppendAllText(filePath, entry);

        }
        [HttpPost]
        public ActionResult UpdateFlagValue(string start, string stored)
        {
            // Or store the flags in Session
            // HttpContext.Session.SetString("startflag", start);
            Session["startflag"] = start;
            Session["StoredFlag"] = stored;
            // HttpContext.Session.SetString("StoredFlag", stored);

            return Json(new { success = true });
        }
    }

}

