﻿using AfricanMagicSystem.Models;
using AfricanMagicSystem.ViewModels;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace AfricanMagicSystem.Controllers
{
    public class ShoppingCartController : Controller
    {

        ApplicationDbContext dB = new ApplicationDbContext();
        //
        // GET: /ShoppingCart/Index/5
        public ActionResult Index()
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            // Set up our ViewModel
            var viewModel = new ShoppingCartViewModel()
            {
                CartItems = cart.GetCartItems(),
                CartTotal = cart.GetTotal()
            };
            // Return the view
            return View(viewModel);
        }
        //
        // GET: /ShoppingCart/AddToCart/5
        [HttpPost]
        public ActionResult AddToCart(int id)
        {
            // Retrieve the item from the database
            var addedItem = dB.Products
                .Single(product => product.ID == id);

            // Add it to the shopping cart
            var cart = ShoppingCart.GetCart(this.HttpContext);

            int count = cart.AddToCart(addedItem);

            
            var results = new ShoppingCartRemoveViewModel
            {
                Message = Server.HtmlEncode(addedItem.Name) +
                    " has been added to your shopping cart.",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = count,
                DeleteId = id
            };
            return Json(results);

            // Go back to the main store page for more shopping
            return RedirectToAction("Index");
        }
        //
        // AJAX: /ShoppingCart/RemoveFromCart/5
        [HttpPost]
        public ActionResult RemoveFromCart(int id)
        {
            // Remove the item from the cart
            var cart = ShoppingCart.GetCart(this.HttpContext);

            // Get the name of the item to display confirmation

            // Get the name of the album to display confirmation
            string itemName = dB.Products
                .Single(item => item.ID == id).Name;

            // Remove from cart
            int itemCount = cart.RemoveFromCart(id);

            // Display the confirmation message
            var results = new ShoppingCartRemoveViewModel
            {
                Message = "One (1) " + Server.HtmlEncode(itemName) +
                    " has been removed from your shopping cart.",
                CartTotal = cart.GetTotal(),
                CartCount = cart.GetCount(),
                ItemCount = itemCount,
                DeleteId = id
            };
            return Json(results);
        }
        //
        // GET: /ShoppingCart/CartSummary
        [ChildActionOnly]
        public ActionResult CartSummary()
        {
            var cart = ShoppingCart.GetCart(this.HttpContext);

            ViewData["CartCount"] = cart.GetCount();
            return PartialView("CartSummary");
        }

        // GET: /ShoppingCart/CartSummary
        [ChildActionOnly]
        public ActionResult PointsSummary()
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(new ApplicationDbContext()));
            var currentUser = manager.FindById(User.Identity.GetUserId());
            int Points = Convert.ToInt32(currentUser.Points);
            ViewData["PointSummary"] = Points;
            return PartialView("PointSummary");
        }
    }
}