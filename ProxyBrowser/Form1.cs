using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CefSharp;
using CefSharp.WinForms;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualBasic;
using System.Net;

namespace ProxyBrowser
{    
    public partial class BrowserForm : Form
    {
        public ChromiumWebBrowser browser = null;
        public ChromiumWebBrowser browser2 = null;
        public bool is_overloaded = false;
        private string[] keys = { "ig_did", "csrftoken", "mid", "rur", "ds_user_id", "sessionid" };
        private string[] keys2 = { "ig_did", "csrftoken", "mid" };
        string username = "mohammedibrahim.lj", password = "xxx";
        public string crftoken, ig_did, mid;
        public IList<CefSharp.Cookie> CookieList=new List<CefSharp.Cookie>();
        bool initialstatus = false;
        
        public BrowserForm(string[] args)
        {            
            InitializeComponent();
            try
            {
                string proxy = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\proxy.lst");
                string urls = File.ReadAllText(AppDomain.CurrentDomain.BaseDirectory + "\\url.lst");
                string[] t1 = Strings.Split(proxy, "&", -1, CompareMethod.Binary);
                string[] t2 = Strings.Split(urls, "|", -1, CompareMethod.Binary);
                if (t1.Length > 0)
                {
                    for (int i = 0; i < t1.Length; i++)
                    {
                        cboProxies.Items.Add(t1[i]);
                    }
                }
                if (t2.Length > 0)
                {
                    for (int i = 0; i < t2.Length; i++)
                    {
                        cboURL.Items.Add(t2[i]);
                    }
                }
            }
            catch(Exception e)
            {

            }
            if(args[0]!="NONE")
            {
                cboProxies.Text = args[0];
                cboURL.Text = args[1];
                is_overloaded = true;
            }
            else
            {
                cboProxies.Text = cboProxies.Items[0].ToString();
                cboURL.Text = cboURL.Items[0].ToString();            
            }            
        }

        private void BrowserForm_Load(object sender, EventArgs e)
        {
            if(is_overloaded)
            {
                InitBrowser();
            }
        }

        public  void InitBrowser()
        {            
            if(browser!=null)
            {
                Process process = new Process();
                process.StartInfo.Arguments = cboProxies.Text + "|" + cboURL.Text;
                process.StartInfo.FileName = Path.GetFileName(System.Reflection.Assembly.GetEntryAssembly().Location);
                process.Start();
                Environment.Exit(1);
            }            
            //CefSettings cfsetting = new CefSettings();
            //CefSharpSettings.Proxy = new ProxyOptions("196.17.171.237", "8000", "e5G14W", "7awpYL");
            //cfsetting.CefCommandLineArgs.Add("proxy-server", cboProxies.Text);
            //cfsetting.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.109 Safari/537.36";            
            //Cef.Initialize(cfsetting);
            browser = new ChromiumWebBrowser(cboURL.Text, new RequestContext());
            browser.RequestHandler = new MyRequestHandler();

            this.tableLayoutPanel1.Controls.Add(browser);
            browser.Dock = DockStyle.Fill;
            string str = cboProxies.Text;
            Cef.UIThreadTaskFactory.StartNew(delegate
            {
                var rc = browser.GetBrowser().GetHost().RequestContext;
                var dict = new Dictionary<string, object>();
                dict.Add("mode", "fixed_servers");
                string[] ttt = Strings.Split(str, ":", -1, CompareMethod.Binary);
                dict.Add("server", ttt[0] + ":" + ttt[1]);
                ProxyConfig.username = ttt[2];
                ProxyConfig.userpassword = ttt[3];
                string error;
                bool success = rc.SetPreference("proxy", dict, out error);
            }).GetAwaiter().GetResult();
            browser.FrameLoadEnd += OnFameLoadEvent;
            //BrowserTimer.Start();

        }

        private async void OnFameLoadEvent(object sender, FrameLoadEndEventArgs e)
        {
            if (initialstatus == false)
            {
                CookieList = await CefSharp.Cef.GetGlobalCookieManager().VisitAllCookiesAsync();
                initialstatus = true;
            }
            //throw new NotImplementedException();
        }

        private void BrowserForm_SizeChanged(object sender, EventArgs e)
        {
            this.tableLayoutPanel1.Width = this.Width - 50;
            this.tableLayoutPanel1.Height = this.Height - 100;
        }

        private void btnGo_Click(object sender, EventArgs e)
        {
            InitBrowser();
            MyTimer.Start();
        }

        private void btnAddProxy_Click(object sender, EventArgs e)
        {
            cboProxies.Items.Add(cboProxies.Text);
        }

        private void btnAddUrl_Click(object sender, EventArgs e)
        {
            cboURL.Items.Add(cboURL.Text);
        }

        private void BrowserForm_FormClosed(object sender, FormClosedEventArgs e)
        {
            File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\Proxy.lst");
            File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\url.lst");
            string proxies = "";
            for(int i=0;i<cboProxies.Items.Count;i++)
            {
                proxies = proxies + cboProxies.Items[i].ToString() + "&";
            }
            proxies = proxies.Substring(0, proxies.Length - 1);
            string urls = "";
            for (int i = 0; i < cboURL.Items.Count; i++)
            {
                urls = urls + cboURL.Items[i].ToString() + "|";
            }
            urls = urls.Substring(0, urls.Length - 1);
            using (var filestream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\Proxy.lst"))
            {
                filestream.Write(Encoding.ASCII.GetBytes(proxies),0,proxies.Length);
            }
            using (var filestream = File.Create(AppDomain.CurrentDomain.BaseDirectory + "\\url.lst"))
            {
                filestream.Write(Encoding.ASCII.GetBytes(urls), 0, urls.Length);
            }            
        }

        private void cboProxies_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void MyTimer_Tick(object sender, EventArgs e)
        {
            if (CookieList!=null && initialstatus)
            {
                InstagramLogin();
                MyTimer.Stop();
            }
        }

        #region HttpLoginToFetchCookie
        private bool InstagramLogin()
        {
            try
            {
                FetchKeyasync();
                if (crftoken != null)
                {
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create("https://www.instagram.com/accounts/login/ajax/?hl=en");
                        request.Headers.Add("X-CSRFToken", crftoken);
                        request.ContentType = "application/x-www-form-urlencoded";
                        request.Accept = "*/*";
                        request.Headers.Add("X-Requested-With", @"XMLHttpRequest");
                        request.Referer = "https://www.instagram.com/accounts/login/?hl=en&source=auth_switcher";
                        request.Headers.Set(HttpRequestHeader.AcceptLanguage, "en-US,en;q=0.5");
                        request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip, deflate");
                        request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64; Trident/7.0; rv:11.0) like Gecko";
                        request.Headers.Set(HttpRequestHeader.CacheControl, "no-cache");
                        request.Headers.Set(HttpRequestHeader.Cookie, @"ig_did=" + ig_did + "; csrftoken=" + crftoken + "; rur=FTW; mid=" + mid);
                        request.Method = "POST";
                        request.ServicePoint.Expect100Continue = false;
                        string body = @"username=" + username + "&password=" + password + "&queryParams=%7B%22hl%22%3A%22en%22%2C%22source%22%3A%22auth_switcher%22%7D&optIntoOneTap=false";
                        byte[] postBytes = System.Text.Encoding.UTF8.GetBytes(body);
                        request.ContentLength = postBytes.Length;
                        Stream stream = request.GetRequestStream();
                        stream.Write(postBytes, 0, postBytes.Length);
                        stream.Close();
                        var response = (HttpWebResponse)request.GetResponse();
                        Getcookie(response);
                        return true;
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            { }
            return false;
        }

        private void BrowserTimer_Tick(object sender, EventArgs e)
        {

        }

        private void Getcookie(HttpWebResponse response)
        {
            try
            {
                string cookievalue = response.Headers["Set-Cookie"].ToString();
                CefSharp.Cookie Rcookie;
                var mngr = Cef.GetGlobalCookieManager();
                foreach (string str in keys)
                {

                    try
                    {
                        int firstindex;
                        int secondindex;
                        string substrings = string.Empty;

                        try
                        {
                            firstindex = cookievalue.IndexOf(str);
                            if (firstindex != -1)
                            {
                                substrings = cookievalue.Substring(firstindex, cookievalue.Length - firstindex);
                                secondindex = substrings.IndexOf(";");
                                substrings = cookievalue.Substring(firstindex, secondindex);
                                Rcookie = new CefSharp.Cookie
                                {
                                    Name = str,
                                    Value = substrings.Replace(str + "=", "")
                                };
                                mngr.SetCookieAsync("https://www.instagram.com/", Rcookie);
                            }
                        }
                        catch { }

                    }
                    catch { }
                }

                browser.Load("https://www.instagram.com/");
            }
            catch (Exception ex)
            { }
        }
        public void FetchKeyasync()
        {
            //Task.Delay(10000);
            
            //var result = CefSharp.Cef.GetGlobalCookieManager().VisitAllCookiesAsync().GetAwaiter().GetResult();
            foreach (var cookie in CookieList)
            {
                if (cookie.Name == "ig_did")
                {
                    ig_did = cookie.Value;
                }
                else if (cookie.Name == "csrftoken")
                {
                    crftoken = cookie.Value;
                }
                else if (cookie.Name == "mid")
                {
                    mid = cookie.Value;
                }
            }
        }

        #endregion
    }

    public static class ProxyConfig
    {
        public static string username;
        public static string userpassword;        
    }
}
