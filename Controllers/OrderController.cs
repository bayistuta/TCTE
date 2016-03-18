using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TCTE.Models;
using TCTE.ViewModel;
using System.Data.Entity;
using TCTE.Filters;
using TCTE.Utility;
using System.IO;
using System.Text;

namespace TCTE.Controllers
{
	[CheckSessionState]
	public class OrderController : Controller
	{
		private TCTEContext db = new TCTEContext( );

		//获取违章信息
		public ActionResult GetPeccancyInfo( string PlateNumber, string VIN )
		{
			if ( string.IsNullOrEmpty( PlateNumber ) || string.IsNullOrEmpty( VIN ) )
			{
				return RedirectToAction( "Index", "Home" );
			}

			PlateNumber = PlateNumber.ToUpper( );
			//查违章信息
			var car = PeccancyHelper.GetPeccancyInfo2( PlateNumber, VIN );

			//回传页面
			ViewBag.PlateNumber = PlateNumber;
			ViewBag.VIN = VIN;

			return View( car );
		}
		/// <summary>
		/// 获取驾驶人信息
		/// </summary>
		/// <param name="plateNumber"></param>
		/// <param name="archiveId"></param>
		/// <returns></returns>
		public JsonResult GetDriverInfo( string personNo, string archiveId )
		{
			var driver = PeccancyHelper.GetDriverInfo( personNo, archiveId );
			if ( driver == null )
			{
				return new JsonResult( )
				{
					JsonRequestBehavior = JsonRequestBehavior.AllowGet,
					Data = ""
				};
			}
			return new JsonResult( )
			{
				JsonRequestBehavior = JsonRequestBehavior.AllowGet,
				Data = driver
			};
		}

		//生成订单
		[HttpPost]
		public ActionResult Create( string PlateNumber, string VIN )
		{
			var user = Session[ "user" ] as User;
			//处于上岗状态且和终端绑定的业务员
			var salesMen = db.SalesMen.Where( s => s.CompanyId == user.CompanyId && s.TerminalId != null ).ToList( );
			ViewBag.SalesMen = new SelectList( salesMen, "Id", "Name" );
			//回传页面
			ViewBag.PlateNumber = PlateNumber;
			ViewBag.VIN = VIN;
			return View( );
		}
		//生成订单
		[HttpPost]
		[ValidateAntiForgeryToken]
		public ActionResult CreateOK( [Bind( Include = "PlateNumber,VIN,Name,Phone,Address,Comment,SalesManId" )]Order order )
		{
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

		//订单列表

		public ActionResult Index( )
		{
			var orders = db.Orders.Include( o => o.SalesMan );
			var user = Session[ "user" ] as User;
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				orders = from o in orders
						 where o.CompanyId == user.CompanyId
						 orderby o.Status ascending, o.Id descending
						 select o;
			}
			else if ( RoleHelper.IsInRole( SystemRole.SUPER_ADMIN ) )
			{
				orders = from o in orders
						 orderby o.Status ascending, o.Id descending
						 select o;
			}
			return View( orders.ToList( ) );
		}

		//订单详情

		public ActionResult Details( int id )
		{
			var order = db.Orders.Include( o => o.OrderDetails ).Include( o => o.SalesMan ).SingleOrDefault( o => o.Id == id );
			var orderImages = db.Database.SqlQuery<OrderImage>( @"SELECT  *
FROM    dbo.OrderImage
WHERE   DecisionNumber IN ( SELECT  DecisionNumber
                            FROM    dbo.OrderDetail
                            WHERE   OrderId = " + id.ToString( ) + ")" ).ToList( );
			ViewBag.IsAdmin = RoleHelper.IsInRole( SystemRole.SUPER_ADMIN );
			return View( new OrderViewModel { Order = order, OrderImages = orderImages } );
		}

		public ActionResult Count( )
		{
			var user = Session[ "user" ] as User;
			if ( RoleHelper.IsInRole( SystemRole.SUPER_ADMIN ) )
			{
				ViewBag.Companies = new SelectList( db.Companies.ToList( ), "Id", "Name" );
				ViewBag.SalesMen = new SelectList( new List<SalesMan>( ), "Id", "Name" );
				ViewBag.Terminals = new SelectList( new List<Terminal>( ), "Id", "Code" );
			}
			else if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				ViewBag.SalesMen = new SelectList( db.SalesMen.Where( a => a.CompanyId == user.CompanyId ).ToList( ), "Id", "Name" );
				ViewBag.Terminals = new SelectList( db.Terminals.Where( a => a.CompanyId == user.CompanyId ).ToList( ), "Id", "Code" );
			}
			return View( );
		}

		[HttpPost]
		public ActionResult Count( OrderCountViewModel model )
		{
			var user = Session[ "user" ] as User;
			if ( RoleHelper.IsInRole( SystemRole.SUPER_ADMIN ) )
			{
				ViewBag.Companies = new SelectList( db.Companies.ToList( ), "Id", "Name" );
				if ( model.CompanyId.HasValue )
				{
					ViewBag.SalesMen = new SelectList( db.SalesMen.Where( a => a.CompanyId == model.CompanyId ).ToList( ), "Id", "Name" );
					ViewBag.Terminals = new SelectList( db.Terminals.Where( a => a.CompanyId == model.CompanyId ).ToList( ), "Id", "Code" );
				}
				else
				{
					ViewBag.SalesMen = new SelectList( new List<SalesMan>( ), "Id", "Name" );
					ViewBag.Terminals = new SelectList( new List<SalesMan>( ), "Id", "Code" );
				}
			}
			else if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				ViewBag.SalesMen = new SelectList( db.SalesMen.Where( a => a.CompanyId == user.CompanyId ).ToList( ), "Id", "Name" );
				ViewBag.Terminals = new SelectList( db.Terminals.Where( a => a.CompanyId == user.CompanyId ).ToList( ), "Id", "Code" );
			}
			var query = from o in db.Orders select o;
			// 二级管理员只能查看本公司订单
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				query = query.Where( a => a.CompanyId == user.CompanyId );
			}
			// 筛选条件			
			if ( model.SalesManId.HasValue )
			{
				query = query.Where( a => a.SalesManId == model.SalesManId );
			}
			if ( model.TerminalId.HasValue )
			{
				query = query.Where( a => a.TerminalId == model.TerminalId );
			}
			if ( model.CompanyId.HasValue )
			{
				query = query.Where( a => a.CompanyId == model.CompanyId );
			}
			if ( model.DateRange != null && model.DateRange.Contains( '-' ) )
			{
				try
				{
					var array = model.DateRange.Split( '-' );
					var startDate = Convert.ToDateTime( array[ 0 ].Trim( ) );
					var endDate = Convert.ToDateTime( array[ 1 ].Trim( ) );
					query = query.Where( a => a.CreatedDate >= startDate && a.CreatedDate <= endDate );
				}
				catch
				{
					ModelState.AddModelError( "", "日期格式不正确" );
					return View( model );
				}
			}

			// 统计
			var result = from a in query
						 group a by a.SalesMan into g
						 select new OrderSummaryViewModel( )
						 {
							 SalesMan = g.Key,
							 OrderCount = g.Count( ),
							 DoneCount = g.Sum( a => a.OrderDetails.Count( ) )
						 };
			// 排序
			result = result.OrderByDescending( a => a.OrderCount );
			model.Result = result;
			return View( model );
		}

		// 统计后查看订单列表
		public ActionResult ViewOrdersOfSalesMan( int? SalesManId, int? TerminalId, int? CompanyId, string DateRange )
		{
			var user = Session[ "user" ] as User;
			var query = from o in db.Orders select o;
			// 二级管理员只能查看本公司订单
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				query = query.Where( a => a.CompanyId == user.CompanyId );
			}
			// 筛选条件			
			if ( SalesManId.HasValue )
			{
				query = query.Where( a => a.SalesManId == SalesManId );
			}
			if ( TerminalId.HasValue )
			{
				query = query.Where( a => a.TerminalId == TerminalId );
			}
			if ( CompanyId.HasValue )
			{
				query = query.Where( a => a.CompanyId == CompanyId );
			}
			if ( DateRange != null && DateRange.Contains( '-' ) )
			{
				try
				{
					var array = DateRange.Split( '-' );
					var startDate = Convert.ToDateTime( array[ 0 ].Trim( ) );
					var endDate = Convert.ToDateTime( array[ 1 ].Trim( ) );
					query = query.Where( a => a.CreatedDate >= startDate && a.CreatedDate <= endDate );
				}
				catch
				{

				}
			}

			query = query.OrderByDescending( a => a.CreatedDate );

			return View( "Index", query );
		}

		// 统计后查看处理条数
		public ActionResult ViewOrderPeccancy( int? SalesManId, int? TerminalId, int? CompanyId, string DateRange )
		{
			var user = Session[ "user" ] as User;
			var query = from o in db.OrderDetails select o;
			// 二级管理员只能查看本公司订单
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				query = query.Where( a => a.Order.CompanyId == user.CompanyId );
			}
			// 筛选条件			
			if ( SalesManId.HasValue )
			{
				query = query.Where( a => a.Order.SalesManId == SalesManId );
			}
			if ( TerminalId.HasValue )
			{
				query = query.Where( a => a.Order.TerminalId == TerminalId );
			}
			if ( CompanyId.HasValue )
			{
				query = query.Where( a => a.Order.CompanyId == CompanyId );
			}
			if ( DateRange != null && DateRange.Contains( '-' ) )
			{
				try
				{
					var array = DateRange.Split( '-' );
					var startDate = Convert.ToDateTime( array[ 0 ].Trim( ) );
					var endDate = Convert.ToDateTime( array[ 1 ].Trim( ) );
					query = query.Where( a => a.Order.CreatedDate >= startDate && a.Order.CreatedDate <= endDate );
				}
				catch
				{

				}
			}

			query = query.OrderByDescending( a => a.Order.CreatedDate );

			return View( query );
		}

		// 导出excel
		public ActionResult ExportExcel( int? SalesManId, int? TerminalId, int? CompanyId, string DateRange )
		{
			var query = from o in db.Orders select o;
			// 二级管理员只能查看本公司订单
			var user = Session[ "user" ] as User;
			if ( RoleHelper.IsInRole( SystemRole.COMPANY_ADMIN ) )
			{
				query = query.Where( a => a.CompanyId == user.CompanyId );
			}
			// 筛选条件			
			if ( SalesManId.HasValue )
			{
				query = query.Where( a => a.SalesManId == SalesManId );
			}
			if ( TerminalId.HasValue )
			{
				query = query.Where( a => a.TerminalId == TerminalId );
			}
			if ( CompanyId.HasValue )
			{
				query = query.Where( a => a.CompanyId == CompanyId );
			}
			if ( DateRange != null && DateRange.Contains( '-' ) )
			{
				try
				{
					var array = DateRange.Split( '-' );
					var startDate = Convert.ToDateTime( array[ 0 ].Trim( ) );
					var endDate = Convert.ToDateTime( array[ 1 ].Trim( ) );
					query = query.Where( a => a.CreatedDate >= startDate && a.CreatedDate <= endDate );
				}
				catch
				{
				}
			}

			// 统计
			var result = from a in query
						 group a by a.SalesMan into g
						 select new OrderSummaryViewModel( )
						 {
							 SalesMan = g.Key,
							 OrderCount = g.Count( ),
							 DoneCount = g.Sum( a => a.OrderDetails.Count( ) )
						 };
			// 排序
			result = result.OrderByDescending( a => a.OrderCount );

			// 生成excel
			var excel = new StringBuilder( );
			excel.Append( "<table>" );
			excel.Append( "<tr>" );
			if ( RoleHelper.IsInRole( SystemRole.SUPER_ADMIN ) )
			{
				excel.Append( "<td>商家</td>" );
			}
			excel.Append( "<td>业务员</td>" );
			excel.Append( "<td>订单数量</td>" );
			excel.Append( "<td>处理条数</td>" );
			excel.Append( "</tr>" );
			foreach ( var item in result )
			{
				excel.Append( "<tr>" );
				if ( RoleHelper.IsInRole( SystemRole.SUPER_ADMIN ) )
				{
					excel.AppendFormat( "<td>{0}</td>", item.SalesMan.Company.Name );
				}
				excel.AppendFormat( "<td>{0}</td>", item.SalesMan.Name );
				excel.AppendFormat( "<td>{0}</td>", item.OrderCount );
				excel.AppendFormat( "<td>{0}</td>", item.DoneCount );
				excel.Append( "</tr>" );
			}
			excel.Append( "</table>" );

			var download_filename = string.Format( "订单统计报表_{0}.xls", DateTime.Now.ToString( "yyyyMMddhhmmss" ) );
			return File( Encoding.UTF8.GetBytes( excel.ToString( ) ), "application/ms-excel", download_filename );
		}

	}



}