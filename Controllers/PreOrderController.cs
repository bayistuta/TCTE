using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using TCTE.Filters;
using TCTE.Models;
using TCTE.Models.SystemType;
using TCTE.Utility;

namespace TCTE.Controllers
{
	[CheckSessionState]
	public class PreOrderController : Controller
	{

		private TCTEContext db = new TCTEContext( );

		// 默认查找待审核预约
		public ActionResult Index( )
		{
			// 只有指定商家才能处理预约
			var COMPANY_PREORDER_AUTHENTICATED = int.Parse( ConfigurationManager.AppSettings[ "COMPANY_PREORDER_AUTHENTICATED" ] );
			var user = Session[ "user" ] as User;
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN )
				&& user.CompanyId == COMPANY_PREORDER_AUTHENTICATED )
			{
				var query = from a in db.PreOrders
							where a.Status == PreOrderStatus.WaitingApprove
							orderby a.Status ascending
							select a;
				ViewBag.s = PreOrderStatus.WaitingApprove;
				return View( query );
			}
			return new HttpStatusCodeResult( HttpStatusCode.Forbidden, "无权处理预约" );
		}

		// 按条件查询
		public ActionResult Query( PreOrderStatus? s )
		{
			if ( !s.HasValue )
			{
				s = PreOrderStatus.WaitingApprove;
			}
			// 只有指定商家才能处理预约
			var COMPANY_PREORDER_AUTHENTICATED = int.Parse( ConfigurationManager.AppSettings[ "COMPANY_PREORDER_AUTHENTICATED" ] );
			var user = Session[ "user" ] as User;
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN )
				&& user.CompanyId == COMPANY_PREORDER_AUTHENTICATED )
			{
				var query = from a in db.PreOrders
							where a.Status == s
							select a;
				// 如果是查已受理的预约, 只获取未派单的
				if ( s == PreOrderStatus.Approved )
				{
					query = query.Where( a => !a.OrderId.HasValue );
				}
				// 先预约的排前面
				query = query.OrderBy( a => a.Id );
				// 回传查询条件
				ViewBag.s = s;
				return View( "Index", query );
			}
			return new HttpStatusCodeResult( HttpStatusCode.Forbidden, "无权处理预约" );
		}

		// 处理预约
		public ActionResult Handle( int id )
		{
			var preorder = db.PreOrders.Find( id );
			// 查违章信息
			var car = PeccancyHelper.GetPeccancyInfo2( preorder.PlateNumber, preorder.VIN );
			// 回传页面
			ViewBag.PlateNumber = preorder.PlateNumber.StartsWith( "川" ) ? preorder.PlateNumber.Substring( 1 ) : preorder.PlateNumber;
			ViewBag.VIN = preorder.VIN;
			ViewBag.PreOrder = preorder;
			return View( car );
		}

		// 通过审核但不生成订单
		public ActionResult ApproveButNotCreateOrder( int id )
		{
			var preorder = db.PreOrders.Find( id );
			preorder.Status = PreOrderStatus.Approved;
			db.SaveChanges( );
			return RedirectToAction( "Index" );
		}

		// 通过审核并准备生成订单
		public ActionResult ApproveAndPrePrepareToCreateOrder( int id )
		{
			var preorder = db.PreOrders.Find( id );
			preorder.Status = PreOrderStatus.Approved;
			db.SaveChanges( );

			var user = Session[ "user" ] as User;
			//处于上岗状态且和终端绑定的业务员
			var salesMen = db.SalesMen.Where( s => s.CompanyId == user.CompanyId && s.TerminalId != null ).ToList( );
			ViewBag.SalesMen = new SelectList( salesMen, "Id", "Name" );
			//回传页面
			var order = new Order( )
			{
				PlateNumber = preorder.PlateNumber,
				VIN = preorder.VIN,
				Name = preorder.Name,
				Phone = preorder.Phone,
				Address = preorder.ServiceAddress,
				Comment = string.Format( "预约时间：{0:yyyy-MM-dd hh:mm} 预约地址：{1}", preorder.ServiceTime, preorder.ServiceAddress )
			};
			ViewBag.PlateNumber = preorder.PlateNumber;
			ViewBag.VIN = preorder.VIN;
			ViewBag.PreOrder = preorder;
			return View( order );
		}

		// 生成订单
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreateOrder( int preOrderId, [Bind( Include = "PlateNumber,VIN,Name,Phone,Address,Comment,SalesManId" )]Order order )
		{
			// 添加订单备注
			var preorder = db.PreOrders.Find( preOrderId );
			order.Comment = string.Format( "预约时间:{0}, 预约地址:{1}", preorder.ServiceTime.ToString( "yyyy-MM-dd hh:mm" ), preorder.ServiceAddress );
			// 设置PreOrder.Order
			preorder.Order = order;
            preorder.Status = PreOrderStatus.Appointed;

			var user = Session[ "user" ] as User;
			order.CompanyId = user.CompanyId.Value;
			order.CreatedDate = DateTime.Now;
			order.Status = Models.SystemType.OrderStatus.Created;

			var salesman = db.SalesMen.SingleOrDefault( s => s.Id == order.SalesManId );
			if ( salesman != null )
				order.TerminalId = salesman.TerminalId;

			if ( ModelState.IsValid )
			{
				db.Orders.Add( order );
				db.SaveChanges( );
				//生成订单Code
				order.Code = string.Format( "{0:yyyyMMdd}{1}", order.CreatedDate, order.Id );
				//更新商家服务次数
				var company = db.Companies.Find( user.CompanyId.Value );
				company.OrderCount += 1;
				//更新商家首次服务时间
				if ( company.OrderCount == 1 )
				{
					company.FirstServiceDate = order.CreatedDate;
				}
				db.SaveChanges( );

			}
			return RedirectToAction( "Index" );
		}

		// 拒绝预约
		public ActionResult Refuse( int id, string WhyFailure )
		{
			var preorder = db.PreOrders.Find( id );
			preorder.Status = PreOrderStatus.Refused;
            preorder.WhyFailure = WhyFailure;
			db.SaveChanges( );
			return RedirectToAction( "Index" );
		}
	}
}