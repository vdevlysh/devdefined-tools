﻿using System;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using Castle.MonoRail.Framework;

namespace DevDefined.OAuth.Core
{
    public class OAuthContextFactory
    {
        public OAuthContext FromMonoRailRequest(IRequest request)
        {
            return new OAuthContext
               {
                   Headers = request.Headers,
                   Cookies = CollectCookies(request),
                   QueryParameters = request.QueryString,
                   RequestMethod = request.HttpMethod,
                   RawUri = GetRequestUri(request),
                   FormEncodedParameters = request.Form
               };
        }

        public OAuthContext FromUri(string httpMethod, Uri uri)
        {
            if (httpMethod == null) throw new ArgumentNullException("httpMethod");
            if (uri == null) throw new ArgumentNullException("uri");

            return new OAuthContext
                       {
                           RawUri = uri,
                           RequestMethod = httpMethod
                       };
        }

        public OAuthContext FromWebRequest(HttpWebRequest request, Stream rawBody)
        {
            using (var reader = new StreamReader(rawBody))
            {
                return FromWebRequest(request, reader.ReadToEnd());
            }
        }

        public OAuthContext FromWebRequest(HttpWebRequest request, string body)
        {
            var context = new OAuthContext
                              {
                                  RawUri = request.RequestUri,
                                  Cookies = CollectCookies(request),
                                  Headers = request.Headers,
                                  RequestMethod = request.Method
                              };

            if (request.Headers[HttpRequestHeader.ContentType] == "application/x-www-form-urlencoded")
            {
                context.FormEncodedParameters = HttpUtility.ParseQueryString(body);
            }

            return context;
        }

        private NameValueCollection CollectCookies(HttpWebRequest request)
        {
            return CollectCookiesFromHeaderString(request.Headers[HttpRequestHeader.Cookie]);
        }

        private NameValueCollection CollectCookies(IRequest request)
        {
            string cookieHeader = request.Headers["Set-Cookie"];

            return CollectCookiesFromHeaderString(cookieHeader);
        }

        private NameValueCollection CollectCookiesFromHeaderString(string cookieHeader)
        {
            var cookieCollection = new NameValueCollection();

            if (!string.IsNullOrEmpty(cookieHeader))
            {
                string[] cookies = cookieHeader.Split(';');
                foreach (string cookie in cookies)
                {
                    //Remove the trailing and Leading white spaces
                    string strCookie = cookie.Trim();

                    var reg = new Regex(@"^(\S*)=(\S*)$");
                    if (reg.IsMatch(strCookie))
                    {
                        Match match = reg.Match(strCookie);
                        if (match.Groups.Count > 2)
                        {
                            cookieCollection.Add(match.Groups[1].Value,
                                                 HttpUtility.UrlDecode(match.Groups[2].Value).Replace(' ', '+'));
                            //HACK: find out why + is coming in as " "
                        }
                    }
                }
            }

            return cookieCollection;
        }

        private Uri GetRequestUri(IRequest request)
        {
            var host = new Uri(string.Format("http://{0}", request.Headers[HttpRequestHeader.Host.ToString()]));

            if (request.Headers.AllKeys.Contains("X-REWRITE-URL"))
            {
                return new Uri(host, request.Headers["X-REWRITE-URL"]);
            }
            else
            {
                return new Uri(request.Uri, request.Url);
            }            
        }
    }
}