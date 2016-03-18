using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace TCTE.ViewModel
{
	public class ChangePreOrderModel
	{
		public string PreOrderNumber { get; set; }
		public DateTime? ServiceTime { get; set; }
		public string ServiceAddress { get; set; }
	}
}