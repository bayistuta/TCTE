using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using TCTE.Models.SystemType;

namespace TCTE.Models
{
	public class PreOrder
	{
		[Key]
		public int Id { get; set; }

		[Display( Name = "车牌号" )]
		public string PlateNumber { get; set; }

		[Display( Name = "号牌种类" )]
		public string PlateType { get; set; }

		[Display( Name = "电话号码" )]
		public string Phone { get; set; }

		[Display( Name = "姓名" )]
		public string Name { get; set; }

		[Display( Name = "身份证号" )]
		public string IDCardNumber { get; set; }

		[Display( Name = "档案号" )]
		public string ArchiveNo { get; set; }

		[Display( Name = "预约时间" ), DisplayFormat( DataFormatString = "{0:yyyy-MM-dd HH:mm}" )]
		public DateTime ServiceTime { get; set; }

		[Display( Name = "预约地点" )]
		public string ServiceAddress { get; set; }

		[Display( Name = "预约单号" )]
		public string PreOrderNumber { get; set; }

		[Display( Name = "会员等级" )]
		public string MemberLevel { get; set; }

		[Display( Name = "预约状态" )]
		[UIHint( "SystemTypeEnum" )]
		public PreOrderStatus Status { get; set; }

		[Display( Name = "预约失败原因" )]
		public string WhyFailure { get; set; }

		// FK

		public int? OrderId { get; set; }

		// Navigation Properties

		public virtual Order Order { get; set; }

	}
}