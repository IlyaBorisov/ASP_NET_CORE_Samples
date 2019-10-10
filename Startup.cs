using Newtonsoft.Json;
using System.IO;
using System.Linq;
using System.Security.Claims;
using compleadapi.BasicAuth;
using compleadapi.Models;
using compleadapi.AnonymousAuth;
using System.Net.Mime;

namespace ASP_NET_CORE_Samples
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers(option => option.EnableEndpointRouting = true)
                    .AddNewtonsoftJson();
            services.AddTransient<DataLayer>();
            services.AddTransient<CommonMethods>();
            services.AddSingleton(FirebaseApp.Create(new AppOptions()
            {
                Credential = GoogleCredential.FromJson(File.ReadAllText("compleadcredentials.json"))
            }));
            services
                .AddAuthentication()
                .AddJwtBearer(options =>
                 {
                     options.Authority = Configuration["FirebaseAuthentication:Issuer"];
                     options.TokenValidationParameters = new TokenValidationParameters
                     {
                         ValidateIssuer = true,
                         ValidIssuer = Configuration["FirebaseAuthentication:Issuer"],
                         ValidateAudience = true,
                         ValidAudience = Configuration["FirebaseAuthentication:Audience"],
                         ValidateLifetime = true,
                         AuthenticationType = JwtBearerDefaults.AuthenticationScheme
                     };
                     options.Events = new JwtBearerEvents
                     {
                         OnTokenValidated = async (context) =>
                          {
                              var _db = context.HttpContext.RequestServices.GetRequiredService<DataLayer>();
                              var uid = context.Principal.Claims.Where(claim => claim.Type == "user_id").First().Value;
                              var sqlparameters = new SqlParameters { { "uid", uid } };
                              string command = $"EXEC dbo.API_CheckBearerAuth {sqlparameters}";
                              var data = await _db.SQLSendQueryAsync<QueryData>(command, sqlparameters).ConfigureAwait(false);
                              if (data[0][0].TryGetValue("PartId", out object value) && (int)value > 0)
                              {
                                  var partinfo = JsonConvert.SerializeObject(new PartnerInfo((int)data[0][0]["PartId"],
                                                                                          (int)data[0][0]["IsAnal"] == 1,
                                                                                          (int)data[0][0]["IsApproved"] == 1,
                                                                                          (int)data[0][0]["IsBlocked"] == 1,
                                                                                          true,
                                                                                          (string)data[0][0]["Idp"],
                                                                                          (string)data[0][0]["PartnerName"],
                                                                                          (int)data[0][0]["ManagerId"],
                                                                                          (string)data[0][0]["ManagerName"],
                                                                                          (string)data[0][0]["ManagerPhone"]));
                                  (context.Principal.Identity as ClaimsIdentity).AddClaim(new Claim(ClaimTypes.Name, partinfo));
                              }
                              else
                              {
                                  context.Fail("No such user");
                              }
                          }
                     };
                 })
                .AddBasic<BasicAuth.AuthenticationService>(o =>
                {
                    o.Realm = "basic";
                })
                .AddIdp<IdpAuth.AuthenticationService>(o =>
                {
                    o.Realm = "idp";
                })
                .AddAnonymous(o =>
                {
                    o.Realm = "anon";
                });
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(
                builder =>
                {
                    builder.AllowAnyOrigin()
                    .AllowAnyMethod()
                    .AllowAnyHeader();
                });
            });
        }
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();
            else
                app.UseHsts();
            app.UseCors();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseStaticFiles(
                new StaticFileOptions
                {
                    ServeUnknownFileTypes = true,
                    DefaultContentType = MediaTypeNames.Text.Plain
                }
            );
            app.UseMiddleware<GETMiddleWare>();
            app.UseOptionsResponse();
            app.UseMiddleware<TechTypeMiddleware>();
            app.UseAuthorization();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
