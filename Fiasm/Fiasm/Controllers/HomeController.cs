using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Fiasm.Core.ServiceInterfaces;
using Microsoft.AspNetCore.Mvc;

namespace Fiasm.Controllers
{
    public class HomeController : Controller
    {
        IUserService userService = null;

        public HomeController(IUserService userService)
        {
            this.userService = userService;
        }

        public IActionResult Index()
        {
            //userService.AuthenticateUser(HttpContext.Authentication)
            return View();
        }

        public IActionResult Error()
        {
            ViewData["RequestId"] = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
            return View();
        }
    }
}
