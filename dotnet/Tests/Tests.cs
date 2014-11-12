﻿using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Reachmail;
using Reachmail.Mailings.Filtered.Post.Request;
using Reachmail.Mailings.Post.Request;
using Reachmail.Lists.Post.Request;
using Reachmail.Lists.Filtered.Post.Request;
using Reachmail.Reports.Mailings.Detail.Post.Request;
using Reachmail.Lists.Recipients.PostByListId.Request;
using Should;

namespace Tests
{
    [TestFixture]
    public class Tests
    {
        private readonly string _accountKey = Environment.GetEnvironmentVariable("RM_TEST_ACCTKEY");
        private readonly string _username = Environment.GetEnvironmentVariable("RM_TEST_USERNAME");
        private readonly string _password = Environment.GetEnvironmentVariable("RM_TEST_PASSWORD");

        private Api _reachmail;

        [SetUp]
        public void Setup()
        {
            _reachmail = Api.Connect(_accountKey, _username, _password, allowSelfSignedCerts: true, timeout: 1200);
        }

        [Test]
        public void should_get_current_user()
        {
            var currentUser = _reachmail.Administration.Users.Current.Get();
            currentUser.AccountId.ShouldNotEqual(Guid.Empty);
            currentUser.AccountKey.ShouldEqual(_accountKey);
            currentUser.Username.ShouldEqual(_username);
        }

        [Test]
        public void should_interact_with_mailing_api()
        {
            var mailingName = "TestMailing-" + Guid.NewGuid().ToString("N");

            // Post
            var postMailing = _reachmail.Mailings.Post(new MailingProperties
            {
                Name = mailingName,
                MailingFormat = MailingFormat.TextAndHtml
            });

            // Get
            var getMailing = _reachmail.Mailings.Get(postMailing.Id);
            getMailing.Id.ShouldEqual(postMailing.Id);
            getMailing.Name.ShouldEqual(mailingName);
            getMailing.MailingFormat.ShouldEqual(Reachmail.Mailings.GetByMailingId.Response.MailingFormat.TextAndHtml);

            // Get many
            var queryLists = _reachmail.Mailings.Filtered.Post(new MailingFilter { NewerThan = DateTime.Now.AddDays(-1) });
            var queryList = queryLists.FirstOrDefault(x => x.Id == postMailing.Id);
            queryList.ShouldNotBeNull();
            queryList.Id.ShouldEqual(postMailing.Id);
            queryList.Name.ShouldEqual(mailingName);
            queryList.MailingFormat.ShouldEqual(Reachmail.Mailings.Filtered.Post.Response.MailingFormat.TextAndHtml);

            // Put
            _reachmail.Mailings.Put(postMailing.Id,
                new Reachmail.Mailings.PutByMailingId.Request.MailingProperties { Name = "New" + mailingName });
            getMailing = _reachmail.Mailings.Get(postMailing.Id);
            getMailing.Id.ShouldEqual(postMailing.Id);
            getMailing.Name.ShouldEqual("New" + mailingName);
            getMailing.MailingFormat.ShouldEqual(Reachmail.Mailings.GetByMailingId.Response.MailingFormat.TextAndHtml);

            // Delete
            _reachmail.Mailings.Delete(postMailing.Id);
            _reachmail.Mailings.Filtered.Post(new MailingFilter { NewerThan = DateTime.Now.AddMinutes(-10) })
                .Any(x => x.Id == postMailing.Id).ShouldBeFalse();
        }

        [Test]
        public void should_interact_with_list_api()
        {
            var listName = "TestList-" + Guid.NewGuid().ToString("N");

            // Post
            var postList = _reachmail.Lists.Post(new ListProperties
            {
                Name = listName,
                Fields = new Fields { "Zip" },
                Type = ListType.Recipient
            });

            // Get
            var getList = _reachmail.Lists.Get(postList.Id);
            getList.Id.ShouldEqual(postList.Id);
            getList.Name.ShouldEqual(listName);
            getList.Type.ShouldEqual(Reachmail.Lists.GetByListId.Response.ListType.Recipient);

            // Get many
            var queryLists = _reachmail.Lists.Filtered.Post(new ListFilter { NewerThan = DateTime.Now.AddDays(-1) });
            var queryList = queryLists.FirstOrDefault(x => x.Id == postList.Id);
            queryList.ShouldNotBeNull();
            queryList.Id.ShouldEqual(postList.Id);
            queryList.Name.ShouldEqual(listName);
            queryList.Type.ShouldEqual(Reachmail.Lists.Filtered.Post.Response.ListType.Recipient);

            // Put
            _reachmail.Lists.Put(postList.Id,
                new Reachmail.Lists.PutByListId.Request.ListProperties { Name = "New" + listName });
            getList = _reachmail.Lists.Get(postList.Id);
            getList.Id.ShouldEqual(postList.Id);
            getList.Name.ShouldEqual("New" + listName);
            getList.Type.ShouldEqual(Reachmail.Lists.GetByListId.Response.ListType.Recipient);

            // Add recipient
            _reachmail.Lists.Recipients.Post(postList.Id, new RecipientProperties { Email = "test@test.com" });

            // Delete recipients
            _reachmail.Lists.Recipients.Filtered.Delete.Post(postList.Id, new Reachmail.Lists.Recipients.Filtered.Delete.PostByListId.Request.RecipientFilter());

            // Delete
            _reachmail.Lists.Delete(postList.Id);
            _reachmail.Lists.Filtered.Post(new ListFilter { NewerThan = DateTime.Now.AddMinutes(-10) })
                .Any(x => x.Id == postList.Id).ShouldBeFalse();
        }

        [Test]
        public void should_interact_with_data_api()
        {
            var data = _reachmail.Data.Post(new MemoryStream(Encoding.ASCII.GetBytes("oh hai")));
            new StreamReader(_reachmail.Data.Get(data.Id)).ReadToEnd().ShouldEqual("oh hai");
        }

        [Test]
        public void should_interact_with_reports()
        {
            _reachmail.Reports.Mailings.Detail.Post(new MailingReportFilter
            {
                ScheduledDeliveryOnOrAfter = DateTime.Now.AddDays(-1)
            }).ShouldNotBeNull();

            _reachmail.Reports.Mailings.Trackedlinks.Summary.Get(Guid.Empty).ShouldBeEmpty();
        }
    }
}
