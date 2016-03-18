using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TCTE.Models.SystemType
{
	/// <summary>
	/// 预约状态
	/// </summary>
	public enum PreOrderStatus
	{
		/// <summary>
		/// 待审核
		/// </summary>
		WaitingApprove = 1,

		/// <summary>
		/// 拒绝受理
		/// </summary>
		Refused = 2,

		/// <summary>
		/// 已受理
		/// </summary>
		Approved = 3,

		/// <summary>
		/// 已派单
		/// </summary>
		Appointed = 4,

		/// <summary>
		/// 已完成
		/// </summary>
		Completed = 5,

		/// <summary>
		/// 已取消
		/// </summary>
		Canceled = 6
	}
}