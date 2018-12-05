using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Ocelot.Middleware;
using Ocelot.DependencyInjection;
using Consul;
using Ocelot.Provider.Consul;
using Microsoft.AspNetCore.Http;

namespace Gateway
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc().SetCompatibilityVersion(CompatibilityVersion.Version_2_1);
            // services.AddOcelot(Configuration);
            services.AddOcelot(Configuration).AddConsul();

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            
            app.UseCors(builder => builder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials()
            ); //for CORS

            app.Use(async (context, next) => {
                //var token = context.Request.Headers["Authorization"];
               // var token = context.Request.Cookies["UserLoginAPItoken"];

               //switch(context.Request.Path.ToString())
               Console.WriteLine(context.Request.Path.ToString());
               switch(context.Request.Path.ToString())
               {
                   case "/Authentication/login":
                   case "/Authentication/signup":
                        Console.WriteLine("Calling next middleware");
                        await next();
                        break;
               }
               Microsoft.AspNetCore.Http.IRequestCookieCollection cookies = context.Request.Cookies;
               var token = cookies["UserLoginAPItoken"];
                Chilkat.Global glob = new Chilkat.Global();
                glob.UnlockBundle("Anything for 30-day trial");

                using (var client = new ConsulClient())
                {
                    Console.WriteLine("---------entered consul----------------");
                    client.Config.Address = new Uri("http://consul:8500");
                    var getpair2 = client.KV.Get("secretkey");
                    Console.WriteLine("------got the getpair2------");
                    Console.WriteLine("-------key-----" + getpair2.Result.Response.Key);
                    Console.WriteLine("------Value-----"+ getpair2.Result.Response.Value);
                    //var getresult = getpair2.Result.Response.Value
                    // if(getpair2.Result.Response.Value != null)
                    // {
                        Console.WriteLine("---------Entered the function");
                        string secret = System.Text.Encoding.UTF8.GetString(getpair2.Result.Response.Value);
                        Console.WriteLine("------------Secret Key------------"+secret);
                        Chilkat.Rsa rsaExportedPublicKey = new Chilkat.Rsa();
                        rsaExportedPublicKey.ImportPublicKey(secret);
                        var publickey = rsaExportedPublicKey.ExportPublicKeyObj();
                        Console.WriteLine("--------publickey--------"+ publickey);
                        Console.WriteLine("-----token-----" + token);
                        var jwt = new Chilkat.Jwt();
                        if (jwt.VerifyJwtPk(token, publickey))
                        {
                            Console.WriteLine("--inside verify");
                            await next();
                        }
                        else
                        {
                            context.Response.StatusCode = 403;
                            await context.Response.WriteAsync("UnAuthorized");
                        }
                    // }
                    // else
                    // {
                    //     context.Response.StatusCode = 403;
                    //     await context.Response.WriteAsync("UnAuthorized");   &&(jwt.IsTimeValid(token,0)
                    // }
                }
            });

            app.UseOcelot().Wait();
        }
    }
}
