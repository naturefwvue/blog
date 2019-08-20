using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Web;
using System.Web.SessionState;
using Nature.Common;
using Nature.Data;
using Nature.DebugWatch;


namespace BoleMange.blog
{
    /// <summary>
    /// blog de WebAPI
    /// </summary>
    public class blog : IHttpHandler, IRequiresSessionState
    {
        #region 简化代码的几个属性
        /// <summary>
        /// 保存 context ，不用作为参数传来传去了
        /// </summary>
        protected HttpContext Context;// { get; set; }

        /// <summary>
        /// 返回 context.Request 简化代码
        /// </summary>
        protected HttpRequest Request
        {
            get { return Context.Request; }
        }

        /// <summary>
        /// 返回 context.Response 简化代码
        /// </summary>
        protected HttpResponse Response
        {
            get { return Context.Response; }
        }

        /// <summary>
        /// 返回 context.Session 简化代码
        /// </summary>
        protected HttpSessionState Session
        {
            get { return Context.Session; }
        }
        #endregion

        #region 参数，处理跨域访问
        /// <summary>
        /// 回调函数的名称
        /// </summary>
        /// user:jyk
        /// time:2013/1/23 9:44
        public string CallBack { get; set; }

        /// <summary>
        /// 数据库ID
        /// </summary>
        /// user:jyk
        /// time:2013/1/23 9:44
        public string DataBaseID { get; set; }

        /// <summary>
        /// 表单跨域提交，回调的url地址
        /// </summary>
        /// user:jyk
        /// time:2013/6/29 15:02
        public string ResourceUrl { get; set; }

        /// <summary>
        /// 表单跨域提交的情况下，返回的消息
        /// </summary>
        /// user:jyk
        /// time:2013/6/29 15:02
        public string ResourceUrlMessage { get; set; }

        /// <summary>
        /// 表单跨域提交的情况下，新添加的记录ID
        /// </summary>
        /// user:jyk
        /// time:2013/8/21 15:02
        public string NewDataID { get; set; }
        #endregion

        /// <summary>
        /// 记录经历了哪些步骤、以及每个步骤的用时
        /// </summary>
        protected NatureDebug BaseDebug { get; set; }

        //protected delegate void ActionToFunction(HttpContext context);
        //protected static Dictionary<string, ActionToFunction> ActionList = new Dictionary<string, ActionToFunction>();


        /// <summary>
        /// 通过实现 <see cref="T:System.Web.IHttpHandler"/> 接口的自定义 HttpHandler 启用 HTTP Web 请求的处理。
        /// </summary>
        /// <param name="context"><see cref="T:System.Web.HttpContext"/> 对象，它提供对用于为 HTTP 请求提供服务的内部服务器对象（如 Request、Response、Session 和 Server）的引用。</param>
        /// user:jyk
        /// time:2012/10/18 17:41
        public void ProcessRequest(HttpContext context)
        {
            //记录一下，不用传参数了。
            Context = context;
            //允许跨域的设置
            #region 
            string webAppId = HttpContext.Current.Request.QueryString["webappid"];
            if (!string.IsNullOrEmpty(webAppId))
            {
                string url = HttpContext.Current.Request.Url.Host;

                switch (webAppId)
                {
                    case "1"://支撑平台
                        HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin", "http://nature.com");
                        break;
                    case "7"://芒果网站综合管理
                        HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin",
                                                                url.IndexOf("lc", System.StringComparison.Ordinal) >= 0
                                                                    ? "http://192.168.3.96:8108"
                                                                    : "http://naturemanage.517.cn/");
                        break;
                    case "4"://手机推广
                        HttpContext.Current.Response.AddHeader("Access-Control-Allow-Origin",
                                                               url.IndexOf("lc", System.StringComparison.Ordinal) >= 0
                                                                   ? "http://lcmobileplat.naturefw.com"
                                                                   : "http://mobileplat.naturefw.com");
                        break;
                }

                //"http://nature.com,http://192.168.3.96:8108"
            }
            HttpContext.Current.Response.AddHeader("Access-Control-Allow-Credentials", "true");
            HttpContext.Current.Response.AddHeader("Access-Control-Allow-Headers", "POST, GET,Origin, X-Requested-With, Content-Type, Accept");

            CallBack = context.Request.QueryString["callback"] ?? ""; //看看是不是跨域
            if (CallBack.Length == 0)
            {
                //不是跨域application/json
                context.Response.ContentType = "text/plain";
                //cors的方式跨域，运行访问的域名还需要处理

                context.Response.Write("{");
            }
            else
            {
                //跨域z!z\":0   
                context.Response.ContentType = "application/x-javascript";
                context.Response.Write(CallBack + "({");
            }
            #endregion

            //context.Response.ContentType = "application/json";

            //要访问的数据库ID 
            DataBaseID = Request["dbid"];

            if (!Functions.IsIDString(DataBaseID))
            {

                //context.Response.Write(string.IsNullOrEmpty(CallBack) ? re : string.Format("{0}({1})", CallBack, re));
                //context.Response.End();
                DataBaseID = null;
            }

            //看看是不是跨域表单post提交
            ResourceUrl = context.Request.QueryString["reurl"] ?? ""; //看看是不是跨域表单post提交

            //记录调试信息，经过了多少步骤，每个步骤用了多少毫秒
            BaseDebug = new NatureDebug { StartTime = DateTime.Now, DetailList = new List<NatureDebugInfo>() };

             
            DataAccessLibrary dal = DalFactory.CreateDal();
            //[Content],
            string sql = "SELECT  TOP (20) ArticleId, Category1Id, Title,  AgreeCount, OppositionCount, DiscussCount, UserHits, Hits, AddUserID, AddTime FROM blog_Article WHERE(IsDel = 0)";
            string str = dal.ManagerJson.ExecuteFillJsonByColName(sql);

            context.Response.Write(str);


            //模板模式，在子类里实现具体操作，便于流程控制
            Process();


            //流程完毕，输出json格式的debug信息
            ProcessEnd();


        }

        #region 访问结束
        /// <summary>
        /// 结束访问时，应该调用这个
        /// </summary>
        public virtual void ProcessEnd()
        {
            //停止计时
            BaseDebug.Stop();

            //计算输出json格式的debug信息用了多少毫秒。
            var st = new Stopwatch();
            st.Start();

            //输出debug信息
            var sb = new StringBuilder(6000);
            DebugToJson(sb);

            st.Stop();

            sb.Append(",\"debugToJsonTime\":\"");
            sb.Append(Functions.TimeSpantoFloat(st.Elapsed));
            sb.Append("\"} ");

            //判断是不是跨域
            sb.Append(CallBack.Length == 0 ? "}" : "})");

            //判断是不是跨域表单post提交

            if (ResourceUrl.Length == 0)
            {
                Response.Write(sb.ToString());
            }
            else
            {
                //标志
                string guid = Guid.NewGuid().ToString();
                //跨域表单post提交，回调网页，用cache保存debug信息（因为太长了，url参数传递不过去）
                string formId = Request["formId"];
                sb.Insert(0, "\"ajaxPost\":{\"formId\":\"" + formId + "\"");
                Context.Cache.Insert(guid, sb.ToString());

                Response.Redirect(ResourceUrl + "?err=" + ResourceUrlMessage + "&id=" + NewDataID + "&formId=" + formId + "&guid=" + guid);
            }
        }

        #endregion

        #region 输出debug信息
        /// <summary>
        /// 输出debug信息
        /// </summary>
        private void DebugToJson(StringBuilder sb)
        {
            sb.Append(",\"debug\":{\"Title\":\""); sb.Append(BaseDebug.Title);
            sb.Append("\", \"UserId\":\""); sb.Append(BaseDebug.UserId);
            sb.Append("\", \"StartTime\":\""); sb.Append(BaseDebug.StartTime.ToString("yy-MM-dd HH:mm:ss"));
            sb.Append("\", \"UseTime\":\""); sb.Append(BaseDebug.UseTime);
            sb.Append("\", \"Url\":\""); sb.Append(BaseDebug.Url);
            sb.Append("\", \"ErrorMessage\":\""); sb.Append(BaseDebug.ErrorMessage);
            sb.Append("\", \"Detail\": ");

            if (BaseDebug.DetailList == null || BaseDebug.DetailList.Count == 0)
            {
                //没有详细步骤
                sb.Append("[] }");
                return;
            }

            sb.Append("[");
            //输出详细步骤
            foreach (var dic in BaseDebug.DetailList)
            {
                sb.Append("{ "); DebugDetail(sb, dic); sb.Append("},");
            }

            if (sb[sb.Length - 1] == ',') sb[sb.Length - 1] = ']';
            else sb.Append("] ");

            //sb.Append("}"); //debug1 结束

        }

        /// <summary>
        /// 输出debug子步骤信息
        /// </summary>
        private void DebugDetail(StringBuilder sb, NatureDebugInfo info)
        {
            if (info == null)
            {
                //没有详细步骤
                return;
            }

            //输出详细步骤
            sb.Append("\"Title\":\""); sb.Append(info.Title);
            sb.Append("\", \"UseTime\":\""); sb.Append(info.UseTime);
            sb.Append("\", \"Remark\":\""); sb.Append(info.Remark);
            sb.Append("\", \"Detail\": ");

            if (info.DetailList == null || info.DetailList.Count == 0)
            {
                //没有详细步骤
                sb.Append("[] ");
                return;
            }

            sb.Append("[");
            //输出详细步骤
            foreach (var dic in info.DetailList)
            {
                sb.Append("{ "); DebugDetail(sb, dic); sb.Append("},");
            }

            if (sb[sb.Length - 1] == ',') sb[sb.Length - 1] = ']';
            else sb.Append("] ");

        }
        #endregion

        #region 钩子

        /// <summary>
        /// 子类里实现处理方法，代替原来的 ProcessRequest 。模板模式
        /// </summary>
        public virtual void Process()
        {

        }

        #endregion

        #region 实现接口 IsReusable
        /// <summary>
        /// 您将需要在您网站的 web.config 文件中配置此处理程序，
        /// 并向 IIS 注册此处理程序，然后才能进行使用。有关详细信息，
        /// 请参见下面的链接: http://go.microsoft.com/?linkid=8101007
        /// </summary>
        public bool IsReusable
        {
            // 如果无法为其他请求重用托管处理程序，则返回 false。
            // 如果按请求保留某些状态信息，则通常这将为 false。
            get { return true; }
        }
        #endregion
    }
}