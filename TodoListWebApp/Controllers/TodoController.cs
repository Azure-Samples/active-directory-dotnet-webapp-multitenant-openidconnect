using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TodoListWebApp.Models;
using TodoListWebApp.DAL;
using System.Security.Claims;

namespace TodoListWebApp.Controllers
{
    // claims-aware controller for CRUD operations on the Todo collection
    [Authorize]
    public class TodoController : Controller
    {
        private TodoListWebAppContext db = new TodoListWebAppContext();

        // GET: /Todo/
        public ActionResult Index()
        {
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            var currentUserToDos = db.Todoes.Where(a => a.Owner == owner);
            return View(currentUserToDos.ToList());
        }

        // GET: /Todo/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Todo todo = db.Todoes.Find(id);
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (todo == null || (todo.Owner != owner))
            {
                return HttpNotFound();
            }
            return View(todo);
        }

        // GET: /Todo/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: /Todo/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,Description")] Todo todo)
        {
            if (ModelState.IsValid)
            {
                todo.Owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
                db.Todoes.Add(todo);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(todo);
        }
        // GET: /Todo/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Todo todo = db.Todoes.Find(id);
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (todo == null || (todo.Owner != owner))
            {
                return HttpNotFound();
            }
            return View(todo);
        }

        // POST: /Todo/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,Description")] Todo todo)
        {
            if (ModelState.IsValid)
            {
                db.Entry(todo).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            return View(todo);
        }

        // GET: /Todo/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            Todo todo = db.Todoes.Find(id);
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (todo == null || (todo.Owner != owner))
            {
                return HttpNotFound();
            }
            return View(todo);
        }

        // POST: /Todo/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            Todo todo = db.Todoes.Find(id);
            string owner = ClaimsPrincipal.Current.FindFirst(ClaimTypes.NameIdentifier).Value;
            if (todo == null || (todo.Owner != owner))
            {
                return HttpNotFound();
            }
            db.Todoes.Remove(todo);
            db.SaveChanges();
            return RedirectToAction("Index");
        }

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
