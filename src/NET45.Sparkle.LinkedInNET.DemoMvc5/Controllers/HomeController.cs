﻿
namespace Sparkle.LinkedInNET.DemoMvc5.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using Ninject;
    using Sparkle.LinkedInNET.DemoMvc5.Domain;
    using Sparkle.LinkedInNET.OAuth2;
    using Sparkle.LinkedInNET.Profiles;
    using Sparkle.LinkedInNET.ServiceDefinition;

    public class HomeController : Controller
    {
        private LinkedInApi api;
        private DataService data;
        private LinkedInApiConfiguration apiConfig;

        public HomeController(LinkedInApi api, DataService data, LinkedInApiConfiguration apiConfig)
        {
            this.api = api;
            this.data = data;
            this.apiConfig = apiConfig;
        }

        public ActionResult Index()
        {
            // step 1: configuration
            this.ViewBag.Configuration = this.apiConfig;
            
            // step 2: authorize url
            var scope = AuthorizationScope.ReadFullProfile | AuthorizationScope.ReadEmailAddress;
            var state = Guid.NewGuid().ToString();
            var redirectUrl = this.Request.Compose() + this.Url.Action("OAuth2");
            this.ViewBag.LocalRedirectUrl = redirectUrl;
            if (this.apiConfig != null && !string.IsNullOrEmpty(this.apiConfig.ApiKey))
            {
                var authorizeUrl = this.api.OAuth2.GetAuthorizationUrl(scope, state, redirectUrl);
                this.ViewBag.Url = authorizeUrl;
            }
            else
            {
                this.ViewBag.Url = null;
            }

            // step 3
            if (this.data.HasAccessToken)
            {
                var token = this.data.GetAccessToken();
                this.ViewBag.Token = token;
                var user = new UserAuthorization(token);

                var watch = new Stopwatch();
                watch.Start();
                try
                {
                    ////var profile = this.api.Profiles.GetMyProfile(user);
                    var acceptLanguages = new string[] { "en-US", "fr-FR", };
                    var profile = this.api.Profiles.GetMyProfile(user, acceptLanguages, FieldSelector.For<Person>().WithAllFields());

                    this.ViewBag.Profile = profile;
                }
                catch (Exception ex)
                {
                    this.ViewBag.ProfileError = ex.ToString();
                }
                watch.Stop();
                this.ViewBag.ProfileDuration = watch.Elapsed;
            }

            return this.View();
        }

        public ActionResult OAuth2(string code, string state)
        {
            var redirectUrl = this.Request.Compose() + this.Url.Action("OAuth2");
            var result = this.api.OAuth2.GetAccessToken(code, redirectUrl);

            this.ViewBag.Code = code;
            this.ViewBag.Token = result.AccessToken;

            this.data.SaveAccessToken(result.AccessToken);

            var user = new UserAuthorization(result.AccessToken);

            ////var profile = this.api.Profiles.GetMyProfile(user);
            ////this.data.SaveAccessToken();
            return this.View();
        }

        public ActionResult Play()
        {
            var token = this.data.GetAccessToken();
            this.ViewBag.Token = token;
            return this.View();
        }

        public ActionResult Definition()
        {
            var filePath = Path.Combine(this.Server.MapPath("~"), "..", "LinkedInApi.xml");
            var builder = new ServiceDefinitionBuilder();
            using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                builder.AppendServiceDefinition(fileStream);
            }

            var result = new ApiResponse<ApisRoot>(builder.Root);

            return this.Json(result, JsonRequestBehavior.AllowGet);
        }

        public class ApiResponse<T>
        {
            public ApiResponse()
            {
            }

            public ApiResponse(T data)
            {
                this.Data = data;
            }

            public string Error { get; set; }
            public T Data { get; set; }
        }
    }
}
