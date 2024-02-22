using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace HubstaffDemo.CustomFilter
{
    public class CustomExceptionFilterAttribute : HandleErrorAttribute
    {
        public override void OnException(ExceptionContext filterContext)
        {
            if (filterContext.ExceptionHandled)
            {
                return;
            }

            filterContext.ExceptionHandled = true;

            filterContext.Result = new PartialViewResult
            {
                ViewName = "_ErrorModal",
                ViewData = new ViewDataDictionary
                {
                    Model = "Process is Ongoing: Please Refresh the page"
                }
            };
        }
    }
}