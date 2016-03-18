using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.Entity;
using System.Data.Entity.ModelConfiguration.Conventions;
namespace TCTE.Models
{
	public class TCTEContext : DbContext
	{
		static TCTEContext( )
		{
			Database.SetInitializer( new TCTEDBInitializerForTest( ) );
		}
		public TCTEContext( )
			: base( "TCTE" )
		{

		}
		public DbSet<Province> Provinces { get; set; }
		public DbSet<City> Cities { get; set; }
		public DbSet<User> Users { get; set; }
		public DbSet<Company> Companies { get; set; }
		public DbSet<SalesMan> SalesMen { get; set; }
		public DbSet<Client> Clients { get; set; }
		public DbSet<Terminal> Terminals { get; set; }
		public DbSet<Order> Orders { get; set; }
		public DbSet<OrderDetail> OrderDetails { get; set; }
		public DbSet<Role> Roles { get; set; }
		public DbSet<Function> Functions { get; set; }
		public DbSet<RegistrationRequest> RegistrationRequests { get; set; }
		public DbSet<RegistrationToken> RegistrationTokens { get; set; }
		public DbSet<OrderImage> OrderImages { get; set; }
		public DbSet<PreOrder> PreOrders { get; set; }


		protected override void OnModelCreating( DbModelBuilder modelBuilder )
		{
			modelBuilder.Conventions.Remove<PluralizingTableNameConvention>( );
			modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>( );
			modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>( );

			//[hx][新增][20150630] Fluent Api 设置 一对一 关系
			modelBuilder.Entity<Terminal>( ).HasOptional( t => t.SalesMan ).WithMany( );
		}

	}

	public class TCTEDBInitializer : CreateDatabaseIfNotExists<TCTEContext>
	{
		protected override void Seed( TCTEContext context )
		{
			base.Seed( context );
			//1. create super admin
			context.Users.Add( new User( )
			{
				UserName = "admin",
				Password = Utility.EncryptHelper.MD5Encrypt( "qianyunadmin" ),
				CreatedDate = DateTime.Now
			} );
			context.SaveChanges( );
			//2.city and province and roles
			var province = new Province( ) { Name = "四川", Abbr = "川" };
			context.Provinces.Add( province );
			context.Cities.Add( new City { Name = "成都", Abbr = "CD", Provice = province } );
			context.Cities.Add( new City { Name = "自贡市", Abbr = "ZG", Provice = province } );
			context.Cities.Add( new City { Name = "泸州市", Abbr = "LZ", Provice = province } );
			context.Cities.Add( new City { Name = "攀枝花市", Abbr = "PZH", Provice = province } );
			context.Cities.Add( new City { Name = "南充市", Abbr = "NC", Provice = province } );
			context.Cities.Add( new City { Name = "达州市", Abbr = "DZ", Provice = province } );
			context.Cities.Add( new City { Name = "乐山市", Abbr = "LS", Provice = province } );
			context.Cities.Add( new City { Name = "雅安市", Abbr = "YA", Provice = province } );
			context.Cities.Add( new City { Name = "宜宾市", Abbr = "YB", Provice = province } );
			context.Cities.Add( new City { Name = "内江市", Abbr = "NJ", Provice = province } );
			context.Cities.Add( new City { Name = "眉山市", Abbr = "MS", Provice = province } );
			context.Cities.Add( new City { Name = "遂宁市", Abbr = "SN", Provice = province } );
			context.Cities.Add( new City { Name = "阿坝藏族羌族自治州", Abbr = "ABZ", Provice = province } );
			context.Cities.Add( new City { Name = "甘孜藏族自治州", Abbr = "GZZ", Provice = province } );
			context.Cities.Add( new City { Name = "凉山彝族自治州", Abbr = "LSZ", Provice = province } );
			var roles = new List<Role>( );
			roles.Add( new Role { Name = "超级管理员" } );
			roles.Add( new Role { Name = "商家管理员" } );
			context.Roles.AddRange( roles );
			context.SaveChanges( );
			//3.set the super admin role to the user named "admin"
			context.Users.FirstOrDefault( a => a.UserName == "admin" ).Role = roles[ 0 ];
			context.SaveChanges( );
			//4.Function
			var roles_SuperAdmin = context.Roles.Where( r => r.Name == "超级管理员" ).ToList( );
			var roles_CompanyAdmin = context.Roles.Where( r => r.Name == "商家管理员" ).ToList( );
			var roles_All = context.Roles.ToList( );
			var functions = new List<Function> { 
                new Function{ Name="设备激活", Controller="Terminal", Action="Register", Roles= roles_SuperAdmin },
                new Function{ Name="设备管理", Controller="Terminal", Action="Index", Roles= roles_All },
                new Function{ Name="用户管理", Controller="User", Action="Index", Roles= roles_SuperAdmin },
                new Function{ Name="商家管理", Controller="Company", Action="Index", Roles= roles_SuperAdmin },
                new Function{ Name="业务员管理", Controller="SalesMan", Action="Index", Roles= roles_CompanyAdmin },
                new Function{ Name="客户管理", Controller="Client", Action="Index", Roles= roles_CompanyAdmin },
                new Function{ Name="订单管理", Controller="Order", Action="Index", Roles= roles_All }
            };
			context.Functions.AddRange( functions );
			context.SaveChanges( );
			//5.companies
			Random rand = new Random( );
			var companies = new List<Company>( );
			for ( int i = 1; i <= 2; i++ )
			{
				var phone = "1390000" + rand.Next( 1000, 9999 );
				companies.Add( new Company( )
				{
					Name = "测试公司" + i.ToString( ),
					Abbr = "TE" + i.ToString( ),
					CreatedDate = DateTime.Now,
					ContactName = "联系人某某",
					Users = new List<User>
                    {
                        new User()
                        {
                            UserName = "test"+i.ToString(),
                            Password = Utility.EncryptHelper.MD5Encrypt("666666"),
                            CreatedDate = DateTime.Now, 
                            Role = roles[1]
                        }
                    },
					Address = "测试公司" + i.ToString( ) + "的地址",
					Phone = phone,
					City = context.Cities.SingleOrDefault( c => c.Abbr == "CD" )
				} );
			}
			context.Companies.AddRange( companies );
			context.SaveChanges( );
			//2.1 generate company code
			context.Companies.Include( c => c.City ).ToList( ).All( c =>
			{
				c.Code = string.Format( "{0}{1}{2:000}", c.City.Abbr, c.Abbr, c.Id );
				return true;
			} );
			context.SaveChanges( );

		}
	}

	public class TCTEDBInitializerForTest : DropCreateDatabaseIfModelChanges<TCTEContext>
	{
		protected override void Seed( TCTEContext context )
		{
			//base.Seed(context);
			//1. create super admin
			context.Users.Add( new User( )
			{
				UserName = "admin",
				Password = Utility.EncryptHelper.MD5Encrypt( "admin" ),
				CreatedDate = DateTime.Now
			} );
			context.SaveChanges( );
			//random tool
			Random rand = new Random( );
			//1.city and province and roles
			var province = new Province( ) { Name = "四川", Abbr = "川" };
			context.Cities.Add( new City { Name = "成都", Abbr = "CD", Provice = province } );
			context.Cities.Add( new City { Name = "自贡市", Abbr = "ZG", Provice = province } );
			context.Cities.Add( new City { Name = "泸州市", Abbr = "LZ", Provice = province } );
			context.Cities.Add( new City { Name = "攀枝花市", Abbr = "PZH", Provice = province } );
			context.Cities.Add( new City { Name = "南充市", Abbr = "NC", Provice = province } );
			context.Cities.Add( new City { Name = "达州市", Abbr = "DZ", Provice = province } );
			context.Cities.Add( new City { Name = "乐山市", Abbr = "LS", Provice = province } );
			context.Cities.Add( new City { Name = "雅安市", Abbr = "YA", Provice = province } );
			context.Cities.Add( new City { Name = "宜宾市", Abbr = "YB", Provice = province } );
			context.Cities.Add( new City { Name = "内江市", Abbr = "NJ", Provice = province } );
			context.Cities.Add( new City { Name = "眉山市", Abbr = "MS", Provice = province } );
			context.Cities.Add( new City { Name = "遂宁市", Abbr = "SN", Provice = province } );
			context.Cities.Add( new City { Name = "阿坝藏族羌族自治州", Abbr = "ABZ", Provice = province } );
			context.Cities.Add( new City { Name = "甘孜藏族自治州", Abbr = "GZZ", Provice = province } );
			context.Cities.Add( new City { Name = "凉山彝族自治州", Abbr = "LSZ", Provice = province } );
			context.Provinces.Add( province );
			var roles = new List<Role>( );
			roles.Add( new Role { Name = "超级管理员" } );
			roles.Add( new Role { Name = "商家管理员" } );
			context.Roles.AddRange( roles );
			context.SaveChanges( );
			//set the super admin role to the user named "admin"
			context.Users.FirstOrDefault( a => a.UserName == "admin" ).Role = roles[ 0 ];
			context.SaveChanges( );
			//2.companies
			var companies = new List<Company>( );
			var city = context.Cities.SingleOrDefault( a => a.Name == "成都" );
			for ( int i = 1; i <= 10; i++ )
			{
				var phone = "1390000" + rand.Next( 1000, 9999 );
				companies.Add( new Company( )
				{
					Name = "商家" + i.ToString( ),
					Abbr = "TE" + i.ToString( ),
					CreatedDate = DateTime.Now,
					ContactName = "采虹",
					Users = new List<User>
                    {
                        new User()
                        {
                            UserName = "test"+i.ToString(),
                            Password = Utility.EncryptHelper.MD5Encrypt("okay"),
                            CreatedDate = DateTime.Now, 
                            Role = roles[1]
                        }
                    },
					Address = "-",
					Phone = phone,
					City = city
				} );
			}
			context.Companies.AddRange( companies );
			context.SaveChanges( );
			//2.1 generate company code
			context.Companies.Include( c => c.City ).ToList( ).All( c =>
			{
				c.Code = string.Format( "{0}{1}{2:000}", c.City.Abbr, c.Abbr, c.Id );
				return true;
			} );
			context.SaveChanges( );
			//3.salesmen
			for ( int i = 1; i <= 60; i++ )
			{
				var company = context.Companies.OrderBy( a => Guid.NewGuid( ) ).Take( 1 ).FirstOrDefault( );
				var salesman = new SalesMan
				{
					CompanyId = company.Id,
					Comment = "无",
					Gender = SystemType.Gender.Male,
					Name = "业务员" + i,
					Address = "-",
					Phone = "1381111" + rand.Next( 1000, 9999 ),
					CreatedDate = DateTime.Now,
					IdentityCard = "50000000000000" + rand.Next( 1000, 9999 ),
					IsLicenced = true,
					TranningDate = DateTime.Now
				};
				salesman.Code = string.Format( "{0}{1:000}", company.Code, i );
				context.SalesMen.Add( salesman );
			}
			context.SaveChanges( );
			//3.1 generate salesman code
			context.SalesMen.Include( c => c.Company ).ToList( ).All( c =>
			{
				c.Code = string.Format( "{0}{1:000}", c.Company.Code, c.Id );
				return true;
			} );
			context.SaveChanges( );
			//4.clients
			var clients = new List<Client>( );
			for ( int i = 1; i <= 100; i++ )
			{
				var company = context.Companies.OrderBy( a => Guid.NewGuid( ) ).Take( 1 ).First( );
				clients.Add( new Client
				{
					CompanyId = company.Id,
					Comment = "无",
					Gender = SystemType.Gender.Male,
					Name = "客户" + i,
					PlateNumber = "川A" + rand.Next( 1000, 9999 ),
					VIN = Guid.NewGuid( ).ToString( ),
					Source = SystemType.Source.Phone,
					Address = "-",
					Phone = "1380000" + new Random( ).Next( 1000, 9999 ),
					CityId = city.Id
				} );
			}
			context.Clients.AddRange( clients );
			context.SaveChanges( );
			//4.1 generate client code
			context.Clients.Include( c => c.Company ).ToList( ).ForEach( c =>
			{
				c.Code = string.Format( "{0}{1:000}", c.Company.Code, c.Id );
			} );
			context.SaveChanges( );
			//5.terminals
			foreach ( var item in context.SalesMen )
			{
				var terminal = new Terminal
				{
					Status = SystemType.TerminalStatus.Normal,
					CreateDate = DateTime.Now,
					AccessToken = Guid.NewGuid( ).ToString( ),
					FingerPrint = Guid.NewGuid( ).ToString( ),
					SalesManId = item.Id,
					CompanyId = item.Company.Id,
					LastInitialDate = DateTime.Now
				};
				item.Terminal = terminal;
				context.Terminals.Add( terminal );
			}
			context.SaveChanges( );

			// generate terminal's code
			context.Terminals.ToList( ).ForEach( t =>
			{
				t.Code = string.Format( "{0}{1:000}", t.Company.Code, t.Id );
			} );
			context.SaveChanges( );

			//6.orders
			for ( int i = 1; i <= 200; i++ )
			{
				var client = context.Clients.OrderBy( a => Guid.NewGuid( ) ).Take( 1 ).First( );
				var sales = context.SalesMen.OrderBy( a => Guid.NewGuid( ) ).Take( 1 ).First( );
				context.Orders.Add( new Order
				{
					SalesManId = sales.Id,
					TerminalId = sales.Terminal.Id,
					CompanyId = sales.Company.Id,
					Client = client,
					PlateNumber = client.PlateNumber,
					VIN = client.VIN,
					Name = client.Name,
					Phone = client.Phone,
					Address = client.Address,
					Comment = "无",
					CreatedDate = DateTime.Now.Subtract( TimeSpan.FromDays( rand.Next( 7 ) ) ).Subtract( TimeSpan.FromHours( rand.Next( 24 ) ) ),
					Status = SystemType.OrderStatus.Ended,
					StartTime = DateTime.Now.Subtract( TimeSpan.FromDays( rand.Next( 6 ) ) ).Subtract( TimeSpan.FromHours( rand.Next( 24 ) ) ),
					EndTime = DateTime.Now.Subtract( TimeSpan.FromDays( rand.Next( 5 ) ) ).Subtract( TimeSpan.FromHours( rand.Next( 24 ) ) ),
				} );
			}
			context.SaveChanges( );
			context.Orders.Include( o => o.Company ).ToList( ).ForEach( o =>
			{
				o.Code = string.Format( "{0}{1}{2:000}", o.Company.Code, DateTime.Now.ToString( "yyyyMMdd" ), o.Id );
			} );
			context.SaveChanges( );
			//7. token
			context.RegistrationTokens.Add( new RegistrationToken( )
			{
				Token = "123",
				Category = "dd"
			} );
			context.SaveChanges( );
			//---------------------------------[hx][20150714][新增]-----------------------------------------------------
			//8. RegistrationRequest
			var RegistrationRequests = new List<RegistrationRequest>( );
			for ( int i = 1; i <= 20; i++ )
			{
				RegistrationRequests.Add( new RegistrationRequest { RegistrationTokenId = 1, AccessToken = Guid.NewGuid( ).ToString( ), ApproveDate = DateTime.Now, RefreshToken = Guid.NewGuid( ).ToString( ), RequestDate = DateTime.Now, Status = SystemType.RegistrationRequestStatus.WaitingApprove } );
			}
			context.RegistrationRequests.AddRange( RegistrationRequests );
			context.SaveChanges( );
			//9. Function
			var roles_SuperAdmin = context.Roles.Where( r => r.Name == "超级管理员" ).ToList( );
			var roles_CompanyAdmin = context.Roles.Where( r => r.Name == "商家管理员" ).ToList( );
			var roles_All = context.Roles.ToList( );
			var functions = new List<Function> { 
                new Function{ Name="设备激活", Controller="Terminal", Action="Register", Roles= roles_SuperAdmin },
                new Function{ Name="设备管理", Controller="Terminal", Action="Index", Roles= roles_All },
                new Function{ Name="用户管理", Controller="User", Action="Index", Roles= roles_SuperAdmin },
                new Function{ Name="商家管理", Controller="Company", Action="Index", Roles= roles_SuperAdmin },
                new Function{ Name="业务员管理", Controller="SalesMan", Action="Index", Roles= roles_CompanyAdmin },
                new Function{ Name="客户管理", Controller="Client", Action="Index", Roles= roles_CompanyAdmin },
                new Function{ Name="订单管理", Controller="Order", Action="Index", Roles= roles_All },
				new Function{ Name="订单统计", Controller="Order", Action="Count", Roles= roles_All }
            };
			context.Functions.AddRange( functions );
			context.SaveChanges( );

			// order details
			foreach ( var order in context.Orders )
			{
				for ( int i = 0; i < 2; i++ )
				{
					var detail = new OrderDetail
					{
						DecisionNumber = Guid.NewGuid( ).ToString( ).Substring( 0, 20 ),
						OrderId = order.Id,
						IsPay = true,
						BankSequenceNumber = Guid.NewGuid( ).ToString( ),
						Deduction = rand.Next( 2, 7 ),
						Money = rand.Next( 100, 300 ),
						HandlePersonNo = Guid.NewGuid( ).ToString( ),
						PeccancyPersonNo = Guid.NewGuid( ).ToString( ),
						PeccancyAddress = "-",
						PeccancyBehavior = "-",
						PeccancyTime = DateTime.Now.Subtract( TimeSpan.FromDays( rand.Next( 15 ) ) )
					};
					context.OrderDetails.Add( detail );
				}
			}
			context.SaveChanges( );
		}
	}
}
