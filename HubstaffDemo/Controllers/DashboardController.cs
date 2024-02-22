using HubstaffDemo.Data;
using HubstaffDemo.Models;
using Newtonsoft.Json;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;
using System.Web.Helpers;
using System.Web.Mvc;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;



namespace HubstaffDemo.Controllers
{
    public class DashboardController : Controller
    {
        // GET: Dashboard
        private readonly ApplicationDbContext _context;
        private int userId;
        public static ConcurrentDictionary<Guid, CancellationTokenSource> TokenSources = new ConcurrentDictionary<Guid, CancellationTokenSource>();
        public static HttpListener Listener;
        private const string BaseFolderPath = @"C:\UrlFile\";
        private static string screenshotFolderPath = @"C:\ScreenshortFile\";
        private static string totalHoursBasePath = @"C:\TotalHours\";
        //  private readonly IServiceScopeFactory _serviceScopeFactory;

        public DashboardController()
        {
            _context = new ApplicationDbContext();
            // _serviceScopeFactory = serviceScopeFactory;
        }
        public ActionResult UserDashboard(User user)
        {
            userId = (int)Session["Id"];
            var cts = new CancellationTokenSource();
            var guid = Guid.NewGuid();
            TokenSources[guid] = cts;
            // HttpContext.Session.SetString("TaskToken", guid.ToString());
            System.Web.HttpContext.Current.Session["TaskToken"] = guid.ToString();
            Listener = new HttpListener();
            Listener.Prefixes.Add("http://localhost:5000/");
            Task.Run(() => CaptureUrls(userId, cts.Token, Listener));
            return View();
        }

        public ActionResult OrganizationDashboard()
        {
            var organizationId = (int)Session["Id"];
            var Users = _context.Users
                                    .Where(u => u.Id == organizationId)
                                 .ToList();
            return View(Users);

        }
        //Org add there team 
        public ActionResult AddUsers()
        {
            TempData.Keep();
            return View();
        }
        [HttpPost]
        public ActionResult AddUsers(User viewModel)
        {
            var organizationId = (int)Session["Id"];
            if(!ModelState.IsValid)
            {
                ViewBag.Error = "Incomplete Data";
            }
            var user = new User
            {
                //Addition
                Name = viewModel.Name,
                Email = viewModel.Email,
                Password = viewModel.Password,
                Designation = viewModel.Designation,
                UrlsJson = viewModel.UrlsJson,
                IsActive = viewModel.IsActive,
                Id = organizationId, //Change from viewModel.Id to AdminID from Session
            };
            var AddingRole = new Roles
            {
                Email = viewModel.Email,
                Role = "User"
            };
            if (user != null && ModelState.IsValid)
            {
                user.Password = Crypto.HashPassword(viewModel.Password);
                _context.Users.Add(user);
                _context.SaveChanges();
                _context.Roles.Add(AddingRole);
                _context.SaveChanges();
                TempData["UserAdded"] = "User created successfully!";
                SendLoginCredentials(viewModel.Email, viewModel.Password);
                return RedirectToAction("OrganizationDashboard");
            }
            else
            {
                ViewBag.Error = "Incomplete Data";
            }
            return View();
        }


        [HttpGet]
        public ActionResult EditUser(int Id)
        {
            var User = _context.Users.Find(Id);
            return View(User);
        }

        [HttpPost]
        public ActionResult EditUser(User viewModel)
        {
            var UserData = _context.Users.Find(viewModel.UId);
            var RoleData = _context.Roles.Find(UserData.Email);
            if (UserData != null)
            {
                bool isEmailChanged = !UserData.Email.Equals(viewModel.Email);
                bool isPasswordChanged = !UserData.Password.Equals(viewModel.Password);
                UserData.Email = viewModel.Email;
                UserData.Password = viewModel.Password;
                UserData.Name = viewModel.Name;
                UserData.IsActive = viewModel.IsActive;

                if (RoleData != null)
                {
                    _context.Roles.Remove(RoleData);
                    _context.SaveChanges();

                    var newRole = new Roles
                    {
                        Email = viewModel.Email,
                        Role = RoleData.Role
                    };

                    _context.Roles.Add(newRole);
                }
                UserData.Password = Crypto.HashPassword(viewModel.Password);
                _context.SaveChanges();
                TempData["UserUpdate"] = "User Updated successfully!";
                if (isEmailChanged || isPasswordChanged)
                {
                    SendLoginCredentials(viewModel.Email, viewModel.Password);
                }
                return RedirectToAction("OrganizationDashboard");
            }
            return View();
        }

        [HttpGet]
        public ActionResult DeleteUser(int id)
        {
            var User = _context.Users.Find(id);
            return View(User);
        }
        [HttpPost]
        public ActionResult DeleteUser(User viewModel)
        {
            var user = _context.Users.FirstOrDefault(x => x.UId == viewModel.UId);
            var role = _context.Roles.FirstOrDefault(r => r.Email == user.Email);
            if (user != null && role != null)
            {
                _context.Users.Remove(user);
                _context.Roles.Remove(role);
                _context.SaveChanges();
                TempData["UserDelete"] = "User Deleted successfully!";
                return RedirectToAction("OrganizationDashboard");
            }
            return View();
        }
        //Admin can Add the ORG

        public ActionResult AdminDashboard()
        {
            var Users = _context.Organizations.ToList();
            return View(Users);
        }
        public ActionResult AddAdmin()
        {
            return View();
        }
        [HttpPost]
        public ActionResult AddAdmin(Organization viewModel)
        {
            //if (viewModel == null || viewModel.Id==0)
            //{
            //    ViewBag.Error = "Incomplete Data";
            //}
           // 
            if (!ModelState.IsValid)
            {
                ViewBag.Error = "Incomplete Data";
            }
           var organization = new Organization
            {
                Email = viewModel.Email,
                Password = viewModel.Password,
                IsActive = viewModel.IsActive,
                City = viewModel.City,
                Name = viewModel.Name,
                OrganizationName = viewModel.OrganizationName,
                TeamSize = viewModel.TeamSize
            };
            var AddingRole = new Roles
            {
                Email = viewModel.Email,
                Role = "Organization"
            };
            if (AddingRole != null && organization != null && ModelState.IsValid)
            {

                organization.Password = Crypto.HashPassword(viewModel.Password);
                _context.Organizations.Add(organization);
                _context.SaveChanges();
                _context.Roles.Add(AddingRole);
                
                _context.SaveChanges();
                TempData["OrgAdded"] = "Organization created successfully!";
                SendLoginCredentials(viewModel.Email, viewModel.Password);
                return RedirectToAction("AdminDashboard");
            }

            else
            { 
                ViewBag.Error = "Incomplete Data";
            }
            return View();
        }
        [HttpGet]
        public ActionResult EditAdmin(int id)
        {
            var User = _context.Organizations.Find(id);
            return View(User);
        }
        [HttpPost]
        public ActionResult EditAdmin(Organization viewModel, Roles roles)
        {
            var AdminData = _context.Organizations.Find(viewModel.Id);
            var RoleData = _context.Roles.FirstOrDefault(r => r.Email == AdminData.Email);
            bool isEmailChanged = !AdminData.Email.Equals(viewModel.Email);
            bool isPasswordChanged = !AdminData.Password.Equals(viewModel.Password);
            if (AdminData != null)
            {
                AdminData.Email = viewModel.Email;
                AdminData.Password = viewModel.Password;
                AdminData.IsActive = viewModel.IsActive;
                AdminData.City = viewModel.City;
                AdminData.Name = viewModel.Name;
                AdminData.OrganizationName = viewModel.OrganizationName;
                AdminData.TeamSize = viewModel.TeamSize;

                if (RoleData != null)
                {
                    _context.Roles.Remove(RoleData);
                    _context.SaveChanges();

                    var newRole = new Roles
                    {
                        Email = viewModel.Email,
                        Role = RoleData.Role
                    };

                    _context.Roles.Add(newRole);
                }
                AdminData.Password = Crypto.HashPassword(viewModel.Password);
                _context.SaveChanges();
                TempData["OrgUpdate"] = "Organization Updated successfully!";
                if (isEmailChanged || isPasswordChanged)
                {
                    SendLoginCredentials(viewModel.Email, viewModel.Password);
                }
                return RedirectToAction("AdminDashboard");
            }
            return View();
        }
            //To Delete record of Admins
            [HttpGet]
        public ActionResult DeleteAdmin(int id)
        {
            var User = _context.Organizations.Find(id);
            return View(User);
        }
        [HttpPost]
        public ActionResult DeleteAdmin(Organization viewModel)
        {
            var user = _context.Organizations.FirstOrDefault(x => x.Id == viewModel.Id);
            var role = _context.Roles.FirstOrDefault(r => r.Email == user.Email);
            if (user != null && role != null)
            {
                _context.Organizations.Remove(user);
                //Need to check 
                _context.Roles.Remove(role);
                _context.SaveChanges();
                TempData["OrgDelete"] = "Organization Deleted successfully!";
                return RedirectToAction("AdminDashboard");
            }
            return View();
        }
        //To store the URL
        //[DllImport("user32.dll")]
        //private static extern bool EnumWindows(EnumWindowsProc enumProc, IntPtr lParam);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        //[DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        //private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        //public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        //public async Task CaptureUrls(User user)
        //{
        //    while (true)
        //    {
        //        try
        //        {
        //            var storedUserId = userId;
        //            user.UId = storedUserId;

        //            if (_context != null)
        //            {
        //                List<string> urlsList = new List<string>();

        //                EnumWindows((hWnd, lParam) =>
        //                {
        //                    try
        //                    {
        //                        StringBuilder windowClass = new StringBuilder(256);
        //                        GetClassName(hWnd, windowClass, windowClass.Capacity);

        //                        if (windowClass.ToString() == "Chrome_WidgetWin_1" ||
        //                            windowClass.ToString() == "MozillaWindowClass" ||
        //                            windowClass.ToString() == "IEFrame")
        //                        {
        //                            StringBuilder urlBuilder = new StringBuilder(256);
        //                            GetWindowText(hWnd, urlBuilder, urlBuilder.Capacity);

        //                            string url = urlBuilder.ToString();

        //                            if (!string.IsNullOrEmpty(url))
        //                            {
        //                                string formattedUrl = $"URL:'{url}' - {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
        //                                urlsList.Add(formattedUrl);

        //                            }
        //                        }
        //                    }
        //                    catch (Exception ex)
        //                    {
        //                        Debug.WriteLine($"Error capturing URL: {ex.Message}");
        //                    }

        //                    return true;
        //                }, IntPtr.Zero);

        //                // Serialize URLs to JSON
        //                string urlsJson = JsonConvert.SerializeObject(urlsList);

        //                // Debug information
        //                Debug.WriteLine($"Total URLs captured: {urlsList.Count}");
        //                Debug.WriteLine($"Serialized URLs JSON: {urlsJson}");

        //                // Save the captured URLs to the database
        //                var usertracking = _context.Users.FirstOrDefault(u => u.UId == storedUserId);

        //                if (usertracking != null)
        //                {
        //                    // Append the new URLs to the existing ones or create a new JSON array if it's the first capture
        //                    if (string.IsNullOrEmpty(usertracking.UrlsJson))
        //                    {
        //                        usertracking.UrlsJson = urlsJson;
        //                    }
        //                    else
        //                    {
        //                        usertracking.UrlsJson = usertracking.UrlsJson.Substring(0, usertracking.UrlsJson.Length - 1) + "," + urlsJson.Substring(1);
        //                    }

        //                    // Save the changes to the database
        //                    _context.SaveChanges();
        //                }

        //                // Save the captured URLs to a text file
        //                SaveUrlsToFile(urlsList);


        //            }

        //        }
        //        catch (Exception ex)
        //        {
        //            Debug.WriteLine($"Error in CaptureUrls: {ex.Message}");

        //        }
        //        await Task.Delay(10000);
        //    }
        //}

        //private void SaveUrlsToFile(List<string> urls)
        //{
        //    try
        //    {
        //        string filePath = Path.Combine(Server.MapPath("~/App_Data"), "webhistory.txt");

        //        // Save each URL to the file
        //        foreach (var url in urls)
        //        {
        //            System.IO.File.AppendAllText(filePath, $"{url}{Environment.NewLine}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"Error saving URLs to file: {ex.Message}");
        //    }
        //}

        public ActionResult DisplayWebHistory()
        {
            try
            {
                //var user = new User();
                //int StoredUserID = (int)Session["Id"];
                //user.UId = StoredUserID;
                var Orgnazation = new Organization();
                var organizationId = (int)Session["Id"];
                Orgnazation.Id = organizationId;

                // Retrieve all users belonging to the organization
                var users = _context.Users.Where(u => u.Id == organizationId).ToList();

                // List to store aggregated web history
                var aggregatedWebHistory = new List<string>();

                // Iterate through each user to retrieve their web history
                foreach (var user in users)
                {
                    try
                    {
                        // Check if the JSON data is null or empty
                        if (!string.IsNullOrEmpty(user.UrlsJson))
                        {
                            // Deserialize the JSON web history and add it to the aggregated list
                            var userWebHistory = JsonConvert.DeserializeObject<List<string>>(user.UrlsJson);
                            aggregatedWebHistory.AddRange(userWebHistory);
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log the error and information about the problematic user
                        Debug.WriteLine($"Error deserializing web history for user {user.Id}: {ex.Message}");
                        Debug.WriteLine($"UrlsJson for user {user.Id}: {user.UrlsJson}");
                    }
                }
                // Pass the aggregated web history to the view
                return View((object)string.Join(Environment.NewLine, aggregatedWebHistory));
            }
            catch (Exception ex)
            {
                // Log the error
                Debug.WriteLine($"Error in DisplayWebHistory: {ex.Message}");
                return Content("An error occurred while trying to display the web history.");
            }
        }



        private void SaveUrlToNotepad(string url, string time)
        {
            try
            {
                // Save each URL to the file immediately
                string filePath = Path.Combine(Server.MapPath("~/App_Data"), "webhistory.txt");
                System.IO.File.AppendAllText(filePath, "USER ID : " + userId + " | URL :" + url + " | TIME OF ACCESS : " + time + Environment.NewLine);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error saving URL to file: {ex.Message}");
            }
        }
        //
        public async Task CaptureUrls(int userId, CancellationToken token, HttpListener listener)
        {
            try
            {


                int UId = userId;
                listener.Start();


                while (true)
                {
                    if (token.IsCancellationRequested)
                    {
                        break;
                    }
                    HttpListenerContext context = listener.GetContext();
                    HttpListenerResponse response = context.Response;
                    HttpListenerRequest request = context.Request; // Define the request variable here
                    string time = request.QueryString["time"];

                    // Read the URL from the request
                    //string url = request.QueryString["url"];
                    string url = HttpUtility.UrlDecode(request.QueryString["url"]);

                    string urlwithtime = "[URL: " + url + " | TIME OF ACCESS: " + DateTime.Now + "]";

                    string urlJson = JsonConvert.SerializeObject(urlwithtime);


                    // var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    var usertracking = _context.Users.FirstOrDefault(u => u.UId == userId);
                    if (usertracking != null)
                    {
                        // Append the new URLs to the existing ones or create a new JSON array if it's the first capture

                        var urls = string.IsNullOrEmpty(usertracking.UrlsJson)
             ? new List<string>()
             : JsonConvert.DeserializeObject<List<string>>(usertracking.UrlsJson);
                        // Add the new URL to the list
                        urls.Add(urlJson);

                        // Serialize the list back into a JSON string
                        usertracking.UrlsJson = JsonConvert.SerializeObject(urls);

                        // Save the changes to the database
                        _context.SaveChanges();


                    }
                    SaveWebHistory(userId, url);
                    SaveUrlToNotepad(url, time);
                    // TODO: Save the URL to a file or a database

                    // Send a response
                    string responseString = "URL received";
                    byte[] buffer = Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    Stream output = response.OutputStream;
                    output.Write(buffer, 0, buffer.Length);
                    output.Close();
                }
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.Message);
            }
        }

     
        [NonAction]
        public bool SendLoginCredentials(string email, string password)
        {
            var httpContext = this.HttpContext; // Access HttpContext using the property provided by ControllerBase
            var port = httpContext.Request.Url.Port;
            var loginUrl = $"{httpContext.Request.Url.Scheme}://{httpContext.Request.Url.Host}:{port}/Login/Login";
            var fromEmail = new MailAddress("gadekaromus@gmail.com", "Login");
            var toEmail = new MailAddress(email);
            var fromEmailPassword = "wwtjmxjgbgjtspxu";
            string subject = "Your Account is Successfully Created!";

            string body = $"<br/>Dear {email},<br/><br/>We are excited to inform you that your Usertracking account has been successfully created.<br/>" +
                $"Below are your login credentials:<br/><br/>Username: {email}<br/>Password: {password}<br/><br/>" +
                $"You can log in using the following link:<br/><a href='{loginUrl}'>{loginUrl}</a>";

            var smtp = new SmtpClient
            {
                Host = "smtp.gmail.com",
                Port = 587, // Gmail SMTP port
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail.Address, fromEmailPassword)
            };

            using (var message = new MailMessage(fromEmail, toEmail)
            {
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            })
            {
                try
                {
                    smtp.Send(message);
                    TempData["SuccessMessage"] = "The email was sent successfully!";

                    return true;
                }
                catch (Exception ex)
                {
                    TempData["ErrorMessage"] = "The email could not be sent.";
                    return false;
                }
            }
        }
        private void SaveWebHistory(int userId, string url)
        {
            

                // Get the user's admin and email
                var user = _context.Users.FirstOrDefault(u => u.UId == userId);
                var admin = _context.Organizations.FirstOrDefault(a => a.Id == user.Id);
                var adminEmail = admin.Email;
                var userEmail = user.Email;

                // Create directories if they don't exist
                if (!Directory.Exists(BaseFolderPath))
                {
                    Directory.CreateDirectory(BaseFolderPath);
                }
                var adminDirectoryPath = Path.Combine(BaseFolderPath, adminEmail);
                if (!Directory.Exists(adminDirectoryPath))
                {
                    Directory.CreateDirectory(adminDirectoryPath);
                }

                var userDirectoryPath = Path.Combine(adminDirectoryPath, userEmail);
                if (!Directory.Exists(userDirectoryPath))
                {
                    Directory.CreateDirectory(userDirectoryPath);
                }

                // Create or append to the webHistory.txt file
                var filePath = Path.Combine(userDirectoryPath, "webHistory.txt");
                var entry = $"{DateTime.Now}: USER ID - {userId} : URL - {url}\n";
                System.IO.File.AppendAllText(filePath, entry);
            }
        [HttpGet]
        public ActionResult DownloadData(string email, string dataType)
        {
            // Validate the data type
            if (dataType != "Urls" && dataType != "TotalHours" && dataType != "Screenshots")
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid data type.");
            }

            // Determine the base path based on the data type
            string basePath;
            if (dataType == "Urls")
            {
                basePath = BaseFolderPath;
            }
            else if (dataType == "TotalHours")
            {
                basePath = totalHoursBasePath;
            }
            else // dataType == "Screenshots"
            {
                basePath = screenshotFolderPath;
            }

            // Retrieve the requested data
            string adminFolderPath = Path.Combine(basePath, email);

            if (!Directory.Exists(adminFolderPath))
            {
                return HttpNotFound("Admin folder not found.");
            }

            // Create a ZIP file of the admin's folder
            var tempZipFilePath = Path.GetTempFileName() + ".zip";
            ZipFile.CreateFromDirectory(adminFolderPath, tempZipFilePath);
            byte[] fileBytes = System.IO.File.ReadAllBytes(tempZipFilePath);
            // Delete the temporary ZIP file
            System.IO.File.Delete(tempZipFilePath);
            string fileName = email + "_" + dataType + ".zip";

            // Return the data as a file download
            return File(fileBytes, "application/octet-stream", fileName);
        }


        //For download the user data from Organzations 
        [HttpGet]
        public ActionResult DownloadUserData(int id, string email, string dataType)
        {
            // Get the admin's email by id
            var admin = _context.Organizations.Find(id);
            if (admin == null)
            {
                return HttpNotFound("Admin folder not found.");
            }

            var adminEmail = admin.Email;
            string basePath;
            if (dataType == "Urls")
            {
                basePath = BaseFolderPath;
            }
            else if (dataType == "TotalHours")
            {
                basePath = totalHoursBasePath;
            }
            else // dataType == "Screenshots"
            {
                basePath = screenshotFolderPath;
            }

            // Get the user's folder within the admin's directory
            var userFolder = Path.Combine(basePath, adminEmail, email);

            // Check if the user's folder exists
            if (!Directory.Exists(userFolder))
            {
                return HttpNotFound("Admin folder not found.");
            }



            // Create a zip file of the data folder
            // Create a zip file of the data folder
            var zipPath = Path.Combine(Path.GetTempPath(), email + "_" + dataType + ".zip");

            // Check if the file already exists, if so, delete it
            if (System.IO.File.Exists(zipPath))
            {
                System.IO.File.Delete(zipPath);
            }

            ZipFile.CreateFromDirectory(userFolder, zipPath);

            // Return the zip file for download
            var bytes = System.IO.File.ReadAllBytes(zipPath);
            return File(bytes, "application/zip", email + "_" + dataType + ".zip");
        }
        [HttpGet]
        [Route("Dashboard/GetTodaysTotalHours")]
        public TimeSpan GetTodaysTotalHours(int userId)
        
        {
            var users = _context.Users.Find(userId);
            if (users == null)
            {
                return TimeSpan.Zero;
            }
            string userEmail = users.Email;
            string adminEmail = _context.Organizations.Find(users.Id).Email;
            string filePath = Path.Combine(totalHoursBasePath, adminEmail, userEmail, "totalHours.txt");

            if (!System.IO.File.Exists(filePath))
            {
                return TimeSpan.Zero;
            }

            var lines = System.IO.File.ReadLines(filePath).ToList();
            var todaysEntries = lines;
            TimeSpan todaysTotalHours = TimeSpan.Zero;


            foreach (var entry in todaysEntries)
            {
                var parts = entry.Split(new string[] { " : " }, StringSplitOptions.None);

                if (parts.Length >= 3)
                {
                    var timePart = parts[2].Replace("Total Hours - ", "");
                    var splitdate = parts[0].Split(' ');

                    DateTime date = DateTime.Parse(splitdate[0]).Date;
                    var todaydatesplit = DateTime.Now.ToString().Split(' ');
                    var datesplit = date.ToString().Split(' ');
                    if (todaydatesplit[0] == datesplit[0])
                    {
                        if (TimeSpan.TryParse(timePart, out TimeSpan time))
                        {
                            todaysTotalHours += time;
                        }
                    }

                }
            }

            return todaysTotalHours;
        }

      
    }
}





