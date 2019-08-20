using System;
using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using Nature.Data;
using Nature.Common;

namespace BoleMange.Blog
{
    /// <summary>
    /// BlogService 博客的后端接口
    /// </summary>
    public class BlogService : IHttpHandler, IRequiresSessionState
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

        protected DataAccessLibrary dal = DalFactory.CreateDal();

        public void ProcessRequest(HttpContext context)
        {
            //记录一下，不用传参数了。
            Context = context;

            Response.ContentType = "text/plain";

           
            Response.Write("{");

            string action = Request.QueryString["action"];
            switch (action)
            {
                case "blog": //博文分类、网站统计
                    Blog();
                    break;
                case "articleList": 
                    ArticleList();
                    break;
                case "discussList":
                    DiscussList();
                    break;
                case "addArticle":
                    AddArticle();
                    break;
                case "addDiscuss":
                    AddDiscuss();
                    break;
            }

            Response.Write("}");

            dal.Dispose();

        }

        #region 发表博文
        private string AddArticle()
        {
            string[] col = new[] { "Category1Id", "Category2Id", "Title", "Content", "addUserId" };
            string[] value = new string[5];
            value[0] = Request.Form["Category1Id"].Trim().Replace("'", "");
            value[1] = "0";//"Request.Form["Category2Id"].Trim().Replace("'", "");
            value[2] = Request.Form["Title"].Trim().Replace("'", "");
            value[3] = Request.Form["Content"].Trim().Replace("'", "");
            value[4] = "1";

            string newId = dal.ModifyData.InsertData("blog_Article", col, value);

            string sql = " select ArticleId, Category1Id,Title,Content,addUserId from blog_Article where ArticleId = {0}";
            string re = dal.ManagerJson.ExecuteFillJsonByColName(string.Format(sql, newId));
            Response.Write("\"status\":1,");
            Response.Write(re);

            return "";
        }
        #endregion


        #region 发表讨论
        private string AddDiscuss()
        {

            string[] col = new[] { "ArticleId", "ParentID", "ParentPath","Chapter", "Content", "addUserId", "NickName" };
            string[] value = new string[7];
            value[0] = Request.Form["ArticleId"].Replace("'","");
            value[1] = Request.Form["ParentID"].Trim().Replace("'", "");
            value[2] = value[1];//Request.Form["ParentPath"].Trim().Replace("'", "");
            value[3] = Request.Form["Chapter"].Trim().Replace("'", "");
            value[4] = Request.Form["Content"].Trim().Replace("'", "");
            value[5] = "1";
            value[6] = Request.Form["NickName"].Trim().Replace("'", "");

            string newId = dal.ModifyData.InsertData("blog_Discuss", col,value);

            //获取讨论记录
            string sql = " select DiscussID, ArticleId,NickName,ParentPath,Chapter,Content,addUserId,AddTime from blog_Discuss where DiscussID = {0}";
            string re = dal.ManagerJson.ExecuteFillJsonByColName(string.Format(sql,newId));
            Response.Write("\"status\":1,");
            Response.Write(re);

            return "";
        }
        #endregion
      

        #region 博主列表、分类和统计
        private string Blog()
        {
            string re = "";

            dal.ManagerJson.JsonName = "categoryList";
            string sql = "SELECT   Category1Id, Title,[Content] FROM blog_Category1 WHERE (IsDel = 0) order by Category1Id";
            string str = dal.ManagerJson.ExecuteFillJsonByColName(sql );
            //Response.Write(str.Replace("\"data\"", "\"categoryList\""));
            Response.Write(str );
            Response.Write(",");

            dal.ManagerJson.JsonName = "bloggerList";
            sql = "SELECT BloggerId,BloggerName,Nick,ArticleCount,DiscussCount  FROM blog_Blogger  WHERE (IsDel = 0) order by BloggerId";
            str = dal.ManagerJson.ExecuteFillJsonByColNameKey(sql );
            //Response.Write(str.Replace("\"data\"", "\"bloggerList\""));
            Response.Write(str);
            Response.Write(",");

            dal.ManagerJson.JsonName = "statistics";
            sql = "SELECT BloggerCount,ArticleCount,DiscussCount  FROM blog_Statistics ";
            str = dal.ManagerJson.ExecuteFillJsonByColName(sql );
            //Response.Write(str.Replace("\"data\"", "\"statistics\""));
            Response.Write(str);

            return re;
        }
        #endregion

        #region 博文列表
        private string ArticleList()
        {
            string re = "";

            string sql = "SELECT  TOP (20) ArticleId, Category1Id,[Content], Title,  AgreeCount, OppositionCount, DiscussCount, UserHits, Hits, AddUserID, AddTime FROM blog_Article WHERE (IsDel = 0) {query} order by ArticleId desc";
            string query = "";

            //判断查询条件
            string category1Id = Request.Form["Category1Id"];
            if (Functions.IsInt(category1Id))
                query += " and Category1Id=" + category1Id;

            string addUserId = Request.Form["bloggerId"];
            if (Functions.IsInt(addUserId))
                query += " and AddUserID=" + addUserId;

            string title = Request.Form["title"];
            if (!string.IsNullOrEmpty(title) )
                query += " and title like '%" + title.Trim().Replace("'","") + "%'";
            string content = Request.Form["Content"];
            if (!string.IsNullOrEmpty(content))
                query += " and [Content] like '%" + content.Trim().Replace("'", "") + "%'";

            string str = dal.ManagerJson.ExecuteFillJsonByColName(sql.Replace("{query}", query));

            Response.Write(str);

            return re;
        }
        #endregion

        #region 讨论列表
        private string DiscussList()
        {
            string re = "";

            string sql = "SELECT  DiscussID, ArticleId, ParentID, NickName,ParentPath, Chapter, [Content], AgreeCount, OppositionCount, DiscussCount, Flag, AddUserID, AddTime FROM      blog_Discuss WHERE(IsDel = 0) AND(ArticleId = {id}) ORDER BY DiscussID";
            string id = Request.QueryString["id"];
            if (Functions.IsInt(id))
            {
                string str = dal.ManagerJson.ExecuteFillJsonByColName(sql.Replace("{id}", id));
                Response.Write(str);
            }
            else
            {
                re = "id参数不正确";
                Response.Write("err:'" + re + "'");
            }

            return re;
        }
        #endregion

        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }
}