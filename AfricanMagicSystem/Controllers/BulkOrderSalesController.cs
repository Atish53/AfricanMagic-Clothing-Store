﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using System.Net;
using System.Web;
using System.Web.Mvc;
using AfricanMagicSystem.Models;
using System.Security.Cryptography;
using Syncfusion.Pdf;
using Syncfusion.Pdf.Graphics;
using System.Drawing;
using Syncfusion.Pdf.Grid;
using System.IO;
using System.Net.Mail;
using System.Text;

namespace AfricanMagicSystem.Controllers
{
    public class BulkOrderSalesController : Controller
    {
        private ApplicationDbContext db = new ApplicationDbContext();

        // GET: BulkOrderSales
        public async Task<ActionResult> Index()
        {
            return View(await db.BulkOrderSales.ToListAsync());
        }

        public async Task<ActionResult> CustomerIndex()
        {
            return View(await db.BulkOrderSales.ToListAsync());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Payment(string value)
        {
            List<BulkOrderSales> checker = (from q in db.BulkOrderSales
                                            select q).ToList();

            foreach (var item in checker)
            {
                if (item.BOSaleID == int.Parse(value))
                {

                    var deposit = item.Total;

                    //deposit -= item.Total;

                    item.BOStatus = "Paid";
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();

                    string name = item.BOEmail;
                    string description = "This is a 40% payment for bulk order";

                    string site = "https://sandbox.payfast.co.za/eng/process";
                    string merchant_id = "";
                    string merchant_key = "";

                    string paymentMode = System.Configuration.ConfigurationManager.AppSettings["PaymentMode"];

                    if (paymentMode == "test")
                    {
                        site = "https://sandbox.payfast.co.za/eng/process?";
                        merchant_id = "10000100";
                        merchant_key = "46f0cd694581a";
                    }

                    // Build the query string for payment site

                    StringBuilder str = new StringBuilder();
                    str.Append("merchant_id=" + HttpUtility.UrlEncode(merchant_id));
                    str.Append("&merchant_key=" + HttpUtility.UrlEncode(merchant_key));
                    str.Append("&return_url=" + HttpUtility.UrlEncode(System.Configuration.ConfigurationManager.AppSettings["PF_NotifyURL"]));
                    str.Append("&cancel_url=" + HttpUtility.UrlEncode(System.Configuration.ConfigurationManager.AppSettings["PF_CancelURL"]));
                    str.Append("&notify_url=" + HttpUtility.UrlEncode(System.Configuration.ConfigurationManager.AppSettings["PF_ReturnURL"]));


                    str.Append("&amount=" + HttpUtility.UrlEncode(deposit.ToString()));
                    str.Append("&item_name=" + HttpUtility.UrlEncode(name));
                    str.Append("&item_description=" + HttpUtility.UrlEncode(description));
                    // Redirect to PayFast
                    return Redirect(site + str.ToString());
                }
            }

            return View("Updated");
        }

        // GET: BulkOrderSales/Details/5
        public async Task<ActionResult> Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BulkOrderSales bulkOrderSales = await db.BulkOrderSales.FindAsync(id);

            var orderDetails = db.BulkOrderSaleDetails.Where(x => x.BOSaleID == bulkOrderSales.BOSaleID);

            bulkOrderSales.BulkOrderSaleDetails = orderDetails.ToList();

            if (bulkOrderSales == null)
            {
                return HttpNotFound();
            }
            return View(bulkOrderSales);
        }

        // GET: BulkOrderSales/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: BulkOrderSales/Create
        public ActionResult SuccessBulk()
        {
            return View("SuccessBulk");
        }

        // POST: BulkOrderSales/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Create(BulkOrderSales bulkOrderSales)
        {
            List<Storage> listStorage = (from q in db.Storages
                                         select q).ToList();
            decimal Totals = 0;
            foreach (var item in listStorage)
            {
                Totals += (item.BulkOrderImages.price * item.Quantity);
            }

            bulkOrderSales.BOSaleDate = DateTime.Now.Date;
            bulkOrderSales.BOStatus = "Pending";
            bulkOrderSales.Total = Totals;

            

            bulkOrderSales.BulkOrderSaleDetails = new List<BulkOrderSaleDetails>();

            foreach (var detadd in listStorage)
            {

                var BOsaleDetail = new BulkOrderSaleDetails()
                {
                    BOSaleID = bulkOrderSales.BOSaleID,
                    BOQuantity = detadd.Quantity,
                    UnitPrice = detadd.BulkOrderImages.price,
                    BulkOrderImagesID = detadd.BulkOrderImagesID
                };
                bulkOrderSales.BulkOrderSaleDetails.Add(BOsaleDetail);
                db.BulkOrderSaleDetails.Add(BOsaleDetail);
            }

            db.BulkOrderSales.Add(bulkOrderSales);

            PdfDocument document = new PdfDocument();
            //Adds page settings
            document.PageSettings.Orientation = PdfPageOrientation.Portrait;
            document.PageSettings.Margins.All = 50;
            //Adds a page to the document
            PdfPage page = document.Pages.Add();
            PdfGraphics graphics = page.Graphics;
            //Loads the image from disk
            PdfImage image = PdfImage.FromFile(Server.MapPath("~/Photos/EmailLogo.png"));
            RectangleF bounds = new RectangleF(10, 10, 200, 200);
            //Draws the image to the PDF page
            page.Graphics.DrawImage(image, bounds);
            PdfBrush solidBrush = new PdfSolidBrush(new PdfColor(126, 151, 173));
            bounds = new RectangleF(0, bounds.Bottom + 90, graphics.ClientSize.Width, 30);
            //Draws a rectangle to place the heading in that region.
            graphics.DrawRectangle(solidBrush, bounds);
            //Creates a font for adding the heading in the page
            PdfFont subHeadingFont = new PdfStandardFont(PdfFontFamily.TimesRoman, 14);
            //Creates a text element to add the invoice number
            PdfTextElement element = new PdfTextElement("Invoice " + bulkOrderSales.BOSaleID.ToString() + " for" + " " + bulkOrderSales.BOEmail, subHeadingFont);
            element.Brush = PdfBrushes.White;

            //Draws the heading on the page
            PdfLayoutResult res = element.Draw(page, new PointF(10, bounds.Top + 8));
            string currentDate = "Date Generated: " + bulkOrderSales.BOSaleDate;
            //Measures the width of the text to place it in the correct location
            SizeF textSize = subHeadingFont.MeasureString(currentDate);
            PointF textPosition = new PointF(graphics.ClientSize.Width - textSize.Width - 10, res.Bounds.Y);
            //Draws the date by using DrawString method
            graphics.DrawString(currentDate, subHeadingFont, element.Brush, textPosition);
            PdfFont timesRoman = new PdfStandardFont(PdfFontFamily.TimesRoman, 10);
            //Creates text elements to add the address and draw it to the page.
            element = new PdfTextElement("Collect Your Order @ 95 Monty Naicker Rd, Durban Central, Durban, 4001, South Africa", timesRoman);
            element.Brush = new PdfSolidBrush(new PdfColor(126, 155, 203));
            res = element.Draw(page, new PointF(10, res.Bounds.Bottom + 25));
            element = new PdfTextElement("Total Price: R" + bulkOrderSales.Total, timesRoman);
            element.Brush = new PdfSolidBrush(new PdfColor(126, 155, 203));
            res = element.Draw(page, new PointF(10, res.Bounds.Bottom + 25));
            PdfPen linePen = new PdfPen(new PdfColor(126, 151, 173), 0.70f);
            PointF startPoint = new PointF(0, res.Bounds.Bottom + 3);
            PointF endPoint = new PointF(graphics.ClientSize.Width, res.Bounds.Bottom + 3);
            //Draws a line at the bottom of the address
            graphics.DrawLine(linePen, startPoint, endPoint);

            //Creates the datasource for the table
            DataTable invoiceDetails = new DataTable();

            //Add columns to the DataTable
            invoiceDetails.Columns.Add("Product Name");
            invoiceDetails.Columns.Add("Quantity");
            invoiceDetails.Columns.Add("Size");
            invoiceDetails.Columns.Add("Colour");
            invoiceDetails.Columns.Add("Price");


            //Add rows to the DataTable
            foreach (var item in listStorage)
            {
                invoiceDetails.Rows.Add(new object[] { item.BulkOrderImages.Name, item.Quantity, item.Size, item.Colour, item.BulkOrderImages.price });
            }

            //Creates a PDF grid
            PdfGrid grid = new PdfGrid();
            //Adds the data source
            grid.DataSource = invoiceDetails;
            //Creates the grid cell styles
            PdfGridCellStyle cellStyle = new PdfGridCellStyle();
            cellStyle.Borders.All = PdfPens.White;
            PdfGridRow header = grid.Headers[0];
            //Creates the header style
            PdfGridCellStyle headerStyle = new PdfGridCellStyle();
            headerStyle.Borders.All = new PdfPen(new PdfColor(126, 151, 173));
            headerStyle.BackgroundBrush = new PdfSolidBrush(new PdfColor(126, 151, 173));
            headerStyle.TextBrush = PdfBrushes.White;
            headerStyle.Font = new PdfStandardFont(PdfFontFamily.TimesRoman, 14f, PdfFontStyle.Regular);

            //Adds cell customizations
            for (int i = 0; i < header.Cells.Count; i++)
            {
                if (i == 0 || i == 1)
                    header.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Left, PdfVerticalAlignment.Middle);
                else
                    header.Cells[i].StringFormat = new PdfStringFormat(PdfTextAlignment.Right, PdfVerticalAlignment.Middle);
            }

            //Applies the header style
            header.ApplyStyle(headerStyle);
            cellStyle.Borders.Bottom = new PdfPen(new PdfColor(217, 217, 217), 0.70f);
            cellStyle.Font = new PdfStandardFont(PdfFontFamily.TimesRoman, 12f);
            cellStyle.TextBrush = new PdfSolidBrush(new PdfColor(131, 130, 136));
            //Creates the layout format for grid
            PdfGridLayoutFormat layoutFormat = new PdfGridLayoutFormat();
            // Creates layout format settings to allow the table pagination
            layoutFormat.Layout = PdfLayoutType.Paginate;
            //Draws the grid to the PDF page.
            PdfGridLayoutResult gridResult = grid.Draw(page, new RectangleF(new PointF(0, res.Bounds.Bottom + 40), new SizeF(graphics.ClientSize.Width, graphics.ClientSize.Height - 100)), layoutFormat);

            MemoryStream outputStream = new MemoryStream();
            document.Save(outputStream);
            outputStream.Position = 0;

            var invoicePdf = new System.Net.Mail.Attachment(outputStream, System.Net.Mime.MediaTypeNames.Application.Pdf);
            string docname = "BulkOrderInvoice.pdf";
            invoicePdf.ContentDisposition.FileName = docname;

            /*MailMessage mail = new MailMessage();
            string emailTo = "africanmagicsystem@gmail.com";
            MailAddress from = new MailAddress("africanmagicsystem@gmail.com");
            mail.From = from;
            mail.Subject = "Your invoice for bulk order number #" + bulkOrderSales.BOSaleID;
            mail.Body = "Dear " + bulkOrderSales.BOEmail + ", find your invoice in the attached PDF document.";
            mail.To.Add(emailTo);

            mail.Attachments.Add(invoicePdf);

            mail.IsBodyHtml = true;
            SmtpClient smtp = new SmtpClient();
            smtp.Host = "smtp.gmail.com";
            smtp.EnableSsl = true;
            NetworkCredential networkCredential = new NetworkCredential("africanmagicsystem@gmail.com", "Afmag19@");
            smtp.UseDefaultCredentials = true;
            smtp.Credentials = networkCredential;
            smtp.Port = 587;
            smtp.Send(mail);
            //Clean-up.
            //Close the document.
            document.Close(true);
            //Dispose of email.
            mail.Dispose();*/

            foreach (var deletetbl in listStorage)
            {
                db.Storages.Remove(deletetbl);
            }

            await db.SaveChangesAsync();

            return RedirectToAction("SuccessBulk");

           // return View(bulkOrderSales);
        }

        // GET: BulkOrderSales/Edit/5
        public async Task<ActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BulkOrderSales bulkOrderSales = await db.BulkOrderSales.FindAsync(id);
            if (bulkOrderSales == null)
            {
                return HttpNotFound();
            }
            return View(bulkOrderSales);
        }

        // POST: BulkOrderSales/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> Edit(BulkOrderSales bulkOrderSales)
        {
            if (ModelState.IsValid)
            {
                db.Entry(bulkOrderSales).State = EntityState.Modified;
                await db.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(bulkOrderSales);
        }

        // GET: BulkOrderSales/Delete/5
        public async Task<ActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BulkOrderSales bulkOrderSales = await db.BulkOrderSales.FindAsync(id);
            if (bulkOrderSales == null)
            {
                return HttpNotFound();
            }
            return View(bulkOrderSales);
        }

        // POST: BulkOrderSales/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<ActionResult> DeleteConfirmed(int id)
        {
            BulkOrderSales bulkOrderSales = await db.BulkOrderSales.FindAsync(id);
            db.BulkOrderSales.Remove(bulkOrderSales);
            await db.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult VerifyStock(string value)
        {
            List<BulkOrderSales> checker = (from q in db.BulkOrderSales
                                            select q).ToList();

            List<BulkOrderSaleDetails> stockchecker = (from q in db.BulkOrderSaleDetails
                                                select q).ToList();

            foreach (var item in checker)
            {
                if (item.BOSaleID == int.Parse(value))
                {

                    item.BOStatus = "Verified";
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return View("Updated");
        }

        public ActionResult RejectStock(string value)
        {
            List<BulkOrderSales> checker = (from q in db.BulkOrderSales
                                            select q).ToList();

            foreach (var item in checker)
            {
                if (item.BOSaleID == int.Parse(value))
                {
                    item.BOStatus = "Rejected";
                    db.Entry(item).State = EntityState.Modified;
                    db.SaveChanges();
                }
            }

            return View("Updated");
        }
        

        /*public async Task<ActionResult> VerifyStock(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            BulkOrderSales bulkOrderSales = await db.BulkOrderSales.FindAsync(id);

            var orderDetails = db.BulkOrderSaleDetails.Where(x => x.BOSaleID == bulkOrderSales.BOSaleID);

            bulkOrderSales.BulkOrderSaleDetails = orderDetails.ToList();

            List<BulkOrderSaleDetails> checker = (from q in db.BulkOrderSaleDetails
                                                  select q).ToList();

            List<String> StockQuantity = new List<string>();
            List<String> StockName = new List<string>();

            foreach (var item in checker)
            {
                if (id == item.BOSaleID)
                {
                    StockQuantity.Add((item.BulkOrderImages.Stock).ToString());
                    StockName.Add(item.BulkOrderImages.Name);
                }
            }

            ViewBag.StockName = StockName;
            ViewBag.StockQuantity = StockQuantity;

            return View();

            if (bulkOrderSales == null)
            {
                return HttpNotFound();
            }
            return View(bulkOrderSales);
        }*/

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
