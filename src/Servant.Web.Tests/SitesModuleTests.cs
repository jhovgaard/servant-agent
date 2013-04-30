﻿using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Nancy;
using Nancy.Testing;
using Servant.Business.Objects;
using Servant.Business.Objects.Enums;
using Servant.Web.Helpers;
using Servant.Web.Tests.Helpers;

namespace Servant.Web.Tests
{
    [TestFixture]
    public class SitesModuleTests
    {
        private Site _testSite = new Site
        {
            ApplicationPool = null,
            Bindings = new List<Binding> { new Binding { UserInput = "http://unit-test-site.com:80", Port = 80, Hostname = "unit-test-site.com", Protocol = Protocol.http, IpAddress = "*"} },
            SitePath = @"c:\inetpub\wwwroot",
            Name = "unit-test-site"
        };

        [TestFixtureTearDown, TestFixtureSetUp]
        public void Cleanup()
        {
            var siteManager = new SiteManager();
            siteManager = new Servant.Web.Helpers.SiteManager();
            var site = siteManager.GetSiteByName(_testSite.Name);
            if(site != null)
                siteManager.DeleteSite(site.IisId);
        }

        [Test]
        public void Can_Show_CreateSite_Page()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("/sites/create", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
 
        [Test]
        public void Can_Create_Site()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();

            var response = browser.Post("/sites/create/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                    with.FormValue("name", _testSite.Name);
                    with.FormValue("sitepath", @"c:\inetpub\wwwroot");
                    with.FormValue("bindingsuserinput", "http://unit-test-site.com");
                    with.FormValue("bindingsipaddress", "*");
                    with.FormValue("bindingscertificatename", "Servant");
                    with.FormValue("applicationpool", "");
                });

            Assert.DoesNotThrow(() => Assert.IsTrue(new Regex(@"/sites/(\d+)/settings", RegexOptions.IgnoreCase).IsMatch(response.Headers["Location"])));
        }

        [Test]
        public void Can_Show_Settings_Page()
        {
            var testSite = GetTestSiteFromIis();
            
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var response = browser.Get("/sites/" + testSite.IisId + "/settings/", with =>
                {
                    with.HttpRequest(); 
                    with.Authenticated();
                });

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }
        
        [Test]
        public void Can_Save_Site_Settings()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var originalSite = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + originalSite.IisId + "/settings/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                    with.FormValue("name", originalSite.Name);
                    with.FormValue("sitepath", originalSite.SitePath);
                    with.FormValue("bindingsuserinput", "http://unit-test-site-edited.com");
                    with.FormValue("bindingsipaddress", "*");
                    with.FormValue("bindingscertificatename", "Servant");
                    with.FormValue("applicationpool", originalSite.ApplicationPool);
                });

            var body = response.Body.AsString();

            StringAssert.Contains("var message = \"Settings have been saved.\"", body);
        }

        [Test]
        public void Cannot_Save_Site_Settings_With_Errors()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var originalSite = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + originalSite.IisId + "/settings/", with =>
            {
                with.Authenticated();
                with.HttpRequest();
                with.FormValue("name", originalSite.Name);
                with.FormValue("sitepath", originalSite.SitePath);
                with.FormValue("bindingsuserinput", "http://unit-test-site-edite%%d.com");
                with.FormValue("bindingsipaddress", "*");
                with.FormValue("bindingscertificatename", "Servant");
                with.FormValue("applicationpool", originalSite.ApplicationPool);
            });

            var body = response.Body.AsString();

            StringAssert.Contains("\"Message\":\"The binding is invalid.\",\"PropertyName\":\"bindingsuserinput[0]\"", body);
        }

        [Test]
        public void Can_Delete_Site()
        {
            var browser = new BrowserBuilder().WithDefaultConfiguration().Build();
            var originalSite = GetTestSiteFromIis();

            var response = browser.Post("/sites/" + originalSite.IisId + "/delete/", with =>
                {
                    with.Authenticated();
                    with.HttpRequest();
                });

            response.ShouldHaveRedirectedTo("/");
        }

        private Site GetTestSiteFromIis()
        {
            var siteManager = new SiteManager(); // Refreshes SiteManager due to Can_Create_Site test

            var testSite = siteManager.GetSiteByName(_testSite.Name);

            if (testSite == null)
                siteManager.CreateSite(_testSite);

            testSite = siteManager.GetSiteByName(_testSite.Name);

            return testSite;
        }

    }
}