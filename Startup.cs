using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;
using Microsoft.AspNetCore.Authorization;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure;
using DotNetCoreRazor_MSGraph.Graph;
using Newtonsoft.Json.Serialization;

namespace DotNetCoreRazor_MSGraph {
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddMvc().AddNewtonsoftJson(x => { x.SerializerSettings.ContractResolver = new DefaultContractResolver(); })
                .AddNewtonsoftJson(x => x.SerializerSettings.DateFormatHandling = Newtonsoft.Json.DateFormatHandling.MicrosoftDateFormat)
                .AddNewtonsoftJson(x => x.SerializerSettings.DateTimeZoneHandling = Newtonsoft.Json.DateTimeZoneHandling.Local);

            // Retrieve required permissions from appsettings
            string[] initialScopes = Configuration.GetValue<string>("DownstreamApi:Scopes")?.Split(' ');

            services
                // Add support for OpenId authentication
                .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)

                // Microsoft identity platform web app that requires an auth code flow
                .AddMicrosoftIdentityWebApp(Configuration)

                // Add ability to call Microsoft Graph APIs with specific permissions
                .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)

                // Enable dependency injection for GraphServiceClient
                .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))

                // Add in-memory token cache
                .AddInMemoryTokenCaches();

            // Require an authenticated user
            services.AddControllersWithViews(options => {
                var policy = new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .Build();
                options.Filters.Add(new AuthorizeFilter(policy));
            });
            services.AddAntiforgery(o => o.HeaderName = "XSRF-TOKEN");
            services
                // Add Razor Pages support
                .AddRazorPages()

                // Add Microsoft Identity UI pages that provide user 
                .AddMicrosoftIdentityUI();

            // .AddMicrosoftIdentityWebApp(options =>
            // {
            //     Configuration.Bind("AzureAd", options);
            //     options.Prompt = "select_account";

            //     // Could use graphClient here to prefetch and cache profile, etc. if wanted
            //     // options.Events.OnTokenValidated = async context =>
            //     // {
            //     //     var tokenAcquisition = context.HttpContext.RequestServices
            //     //         .GetRequiredService<ITokenAcquisition>();
            //     //     var logger = context.HttpContext.RequestServices
            //     //         .GetRequiredService<ILogger<Startup>>();
            //     //     var token = await tokenAcquisition
            //     //         .GetAccessTokenForUserAsync(initialScopes, user: context.Principal);

            //     //     var graphClient = new GraphServiceClient(
            //     //         new DelegateAuthenticationProvider(async (request) =>
            //     //         {
            //     //             request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            //     //         })
            //     //     );

            //     //     // Example of using graphClient
            //     //     var profile = await graphClient.Me.Request().GetAsync();
            //     //     logger.LogInformation(profile.GivenName);
            //     // };

            //     options.Events.OnAuthenticationFailed = context =>
            //     {
            //         var error = WebUtility.UrlEncode(context.Exception.Message);
            //         context.Response
            //             .Redirect($"/Home/ErrorWithMessage?message=Authentication+error&debug={error}");
            //         context.HandleResponse();

            //         return Task.FromResult(0);
            //     };

            //     options.Events.OnRemoteFailure = context =>
            //     {
            //         if (context.Failure is OpenIdConnectProtocolException)
            //         {
            //             var error = WebUtility.UrlEncode(context.Failure.Message);
            //             context.Response
            //                 .Redirect($"/Home/ErrorWithMessage?message=Sign+in+error&debug={error}");
            //             context.HandleResponse();
            //         }

            //         return Task.FromResult(0);
            //     };
            // })
            // .EnableTokenAcquisitionToCallDownstreamApi(initialScopes)
            // .AddMicrosoftGraph(Configuration.GetSection("DownstreamApi"))
            // .AddInMemoryTokenCaches();

            // uncomment the following 3 lines to get ClientSecret from KeyVault
            // string tenantId = Configuration.GetValue<string>("AzureAd:TenantId");
            // services.Configure<MicrosoftIdentityOptions>(
            //    options => { options.ClientSecret = GetSecretFromKeyVault(tenantId, "ENTER_YOUR_SECRET_NAME_HERE"); });

            services.AddScoped<GraphProfileClient>();
            services.AddScoped<GraphEmailClient>();
            services.AddScoped<GraphCalendarClient>();
            services.AddScoped<GraphFilesClient>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }
            else {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapRazorPages();
            });
        }

        /// Gets the secret from key vault via an enabled Managed Identity.
        /// </summary>
        /// <remarks>https://github.com/Azure-Samples/app-service-msi-keyvault-dotnet/blob/master/README.md</remarks>
        /// <returns></returns>
        private string GetSecretFromKeyVault(string tenantId, string secretName) {
            // this should point to your vault's URI, like https://<yourkeyvault>.vault.azure.net/
            string uri = Environment.GetEnvironmentVariable("KEY_VAULT_URI");
            DefaultAzureCredentialOptions options = new DefaultAzureCredentialOptions();

            // Specify the tenant ID to use the dev credentials when running the app locally
            options.VisualStudioTenantId = tenantId;
            options.SharedTokenCacheTenantId = tenantId;
            SecretClient client = new SecretClient(new Uri(uri), new DefaultAzureCredential(options));

            // The secret name, for example if the full url to the secret is https://<yourkeyvault>.vault.azure.net/secrets/ENTER_YOUR_SECRET_NAME_HERE
            Response<KeyVaultSecret> secret = client.GetSecretAsync(secretName).Result;

            return secret.Value.Value;
        }
    }
}
