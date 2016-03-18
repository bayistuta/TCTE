using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace TCTE.Controllers
{
	using TCTE.Models;
	using System.Data.Entity;
	public class AjaxQueryController : Controller
	{
		private TCTEContext db = new TCTEContext( );

		public ActionResult CompanyCascade( int companyId )
		{
			var data = ( from a in db.Companies
						 where a.Id == companyId
						 select new
						 {
							 CompanyId = a.Id,
							 CompanyName = a.Name,
							 SalesMen = from s in a.SalesMen
										select new { s.Id, s.Name },
							 Terminals = from t in a.Terminals
										 select new { t.Id, t.Code }
						 } ).FirstOrDefault( );
			return Json( data, JsonRequestBehavior.AllowGet );
		}
	}
}