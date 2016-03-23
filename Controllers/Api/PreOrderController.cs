using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using TCTE.Filters;
using TCTE.Models;
using System.Data.Entity;
using TCTE.ViewModel;
using System.Configuration;

namespace TCTE.Controllers.Api
{
	[IdentityBasicAuthentication( false )]
	public class PreOrderController : ApiController
	{


		[HttpPost]
		[Route( "api/preorder/create" )]
		public HttpResponseMessage Create( PreOrder model )
		{
			using ( var db = new TCTEContext( ) )
			{
				var query = from a in db.PreOrders
							where a.Status != Models.SystemType.PreOrderStatus.Completed
							&& a.Status != Models.SystemType.PreOrderStatus.Refused
                            && a.Status != Models.SystemType.PreOrderStatus.Canceled
							&& a.PlateNumber.ToUpper( ) == model.PlateNumber.ToUpper( )
							select a;
				if ( query.Count( ) > 0 )
				{
					return Request.CreateResponse( HttpStatusCode.OK, new APIResultObject
					{
						StatusCode = APIResultObject.DuplicatePreOrder,
						Description = "重复预约",
						Result = ""
					} );
				}
				// 状态值
				model.Status = Models.SystemType.PreOrderStatus.WaitingApprove;
				// 授权处理预约的商家
				model.CompanyId = int.Parse( ConfigurationManager.AppSettings[ "COMPANY_PREORDER_AUTHENTICATED" ] );
				db.PreOrders.Add( model );
				db.SaveChanges( );
				return Request.CreateResponse( HttpStatusCode.OK, new APIResultObject
				{
					StatusCode = APIResultObject.OK,
					Description = "预约成功,待审核",
					Result = ""
				} );
			}
		}

		[HttpGet]
		[Route( "api/preorder/query/{PreOrderNumber}" )]
		public HttpResponseMessage Query( string PreOrderNumber )
		{
			using ( var db = new TCTEContext( ) )
			{
				var query = db.PreOrders
					.Include( a => a.Order )
					.Include( a => a.Order.SalesMan )
					.FirstOrDefault( a => a.PreOrderNumber == PreOrderNumber );
				if ( query == null )
				{
					return Request.CreateResponse( HttpStatusCode.OK, new APIResultObject
					{
						StatusCode = APIResultObject.NotFound,
						Description = "未找到预约单号",
						Result = ""
					} );
				}
				var o = new APIResultObject( )
				{
					StatusCode = APIResultObject.OK,
					Description = "",
					Result = query.Status
				};
				if ( query.Status == Models.SystemType.PreOrderStatus.Refused )
				{
					o.Description = query.WhyFailure;
				}
				else if ( query.Status == Models.SystemType.PreOrderStatus.Appointed )
				{
					o.Description = string.Format( "业务员:{0},电话:{1}", query.Order.SalesMan.Name, query.Order.SalesMan.Phone );
				}
				return Request.CreateResponse( HttpStatusCode.OK, o );
			}
		}

		[HttpPost]
		[Route( "api/preorder/change" )]
		public HttpResponseMessage Change( ChangePreOrderModel model )
		{
			using ( var db = new TCTEContext( ) )
			{
				var query = db.PreOrders.FirstOrDefault( a => a.PreOrderNumber == model.PreOrderNumber );
                if (query != null)
                {
                    if (query.Status != Models.SystemType.PreOrderStatus.WaitingApprove)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new APIResultObject
                        {
                            StatusCode = APIResultObject.ChangePreOrderFailure,
                            Description = "预约已取消或已完成,变更失败",
                            Result = ""
                        });
                    }

                    if (model.ServiceTime.HasValue)
                    {
                        query.ServiceTime = model.ServiceTime.Value;
                    }
                    if (model.ServiceAddress != null && model.ServiceAddress.Trim().Length > 0)
                    {
                        query.ServiceAddress = model.ServiceAddress.Trim();
                    }
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, new APIResultObject
                    {
                        StatusCode = APIResultObject.OK,
                        Description = "预约变更成功",
                        Result = ""
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, new APIResultObject
                {
                    StatusCode = APIResultObject.NotFound,
                    Description = "没有找到预约单号",
                    Result = ""
                });
			}
		}

		[HttpPost]
		[Route( "api/preorder/cancel" )]
		public HttpResponseMessage Cancel( CancelPreOrderModel model )
		{
			using ( var db = new TCTEContext( ) )
			{
				var query = db.PreOrders.FirstOrDefault( a => a.PreOrderNumber == model.PreOrderNumber );
                if (query != null)
                {
                    if (query.Status != Models.SystemType.PreOrderStatus.WaitingApprove)
                    {
                        return Request.CreateResponse(HttpStatusCode.OK, new APIResultObject
                        {
                            StatusCode = APIResultObject.CancelPreOrderFailure,
                            Description = "只有待审核的预约才能取消",
                            Result = ""
                        });
                    }

                    query.Status = Models.SystemType.PreOrderStatus.Canceled;
                    db.SaveChanges();
                    return Request.CreateResponse(HttpStatusCode.OK, new APIResultObject
                    {
                        StatusCode = APIResultObject.OK,
                        Description = "预约取消成功",
                        Result = ""
                    });
                }
                return Request.CreateResponse(HttpStatusCode.OK, new APIResultObject
                {
                    StatusCode = APIResultObject.NotFound,
                    Description = "没有找到预约单号",
                    Result = ""
                });
			}
		}
	}
}
