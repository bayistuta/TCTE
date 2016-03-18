using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TCTE.Models;
namespace TCTE.ViewModel
{
	public class OrderViewModel
	{
		public Order Order { get; set; }
		public List<OrderImage> OrderImages { get; set; }
	}
	/// <summary>
	/// 封装订单统计条件, 统计结果的ViewModel
	/// </summary>
	public class OrderCountViewModel
	{
		public int? CompanyId { get; set; }
		public int? SalesManId { get; set; }
		public int? TerminalId { get; set; }
		public string DateRange { get; set; }
		public IEnumerable<OrderSummaryViewModel> Result { get; set; }
	}
	/// <summary>
	/// 封装订单统计结果的ViewModel
	/// </summary>
	public class OrderSummaryViewModel
	{
		public SalesMan SalesMan { get; set; }
		[Display( Name = "订单数" )]
		public int OrderCount { get; set; }
		[Display( Name = "处理条数" )]
		public int DoneCount { get; set; }
	}
}