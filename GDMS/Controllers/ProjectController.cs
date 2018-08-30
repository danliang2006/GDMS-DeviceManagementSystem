﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;
using GDMS.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GDMS.Controllers
{
    [RequestAuthorize]
    public class ProjectController : ApiController
    {

        //POST对象 (通过POST只能获取1个对象，因此POST多个数据需要使用类)
        public class ProjectAjax
        {
            public string userId { get; set; }
            public string systemId { get; set; }
            public int page { get; set; }
            public int limit { get; set; }
            public string keyword { get; set; }
        }
        //返回对象
        private class Response
        {
            public int code { get; set; }
            public string count { get; set; }
            public string msg { get; set; }
            public object data { get; set; }
        }

        //获取项目列表
        [ActionName("list")]
        public HttpResponseMessage ProjectList([FromBody] ProjectAjax projectajax)
        {
            Db db = new Db();
            string where = "";
            if (projectajax.systemId != null) { where = where + " AND A.SYSTEM_ID = '" + projectajax.systemId + "'"; }
            if (projectajax.keyword != null && projectajax.keyword.Length != 0) {
                where = where + "AND ( UPPER(A.DETAIL) LIKE '%" + projectajax.keyword.ToUpper() + "%' or UPPER(A.NAME) LIKE '%" + projectajax.keyword.ToUpper() + "%')";
            }
            string sqlnp = @"
                SELECT
                A.ID AS PROJECT_ID,
                A.NAME AS PROJECT_NAME,
                A.DETAIL,
                A.YEAR,
                A.USER_ID,
                A.EDIT_DATE,
                B.ID AS SYSTEM_ID,
                B.NAME AS SYSTEM_NAME
                FROM
                GDMS_PROJECT A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.SYSTEM_ID IN (SELECT SYSTEM_ID FROM GDMS_USER_SYSTEM WHERE USER_ID = '" + projectajax.userId + "') " + where;

            int limit1 = (projectajax.page - 1) * projectajax.limit + 1;
            int limit2 = projectajax.page * projectajax.limit;
            string sql = "SELECT * FROM(SELECT p1.*,ROWNUM rn FROM(" + sqlnp + ")p1)WHERE rn BETWEEN " + limit1 + " AND " + limit2;
            var ds = db.QueryT(sql);
            Response res = new Response();
            ArrayList data = new ArrayList();
            foreach (DataRow col in ds.Rows)
            {
                Dictionary<string, string> dict = new Dictionary<string, string>
                {
                    { "PROJECT_ID", col["PROJECT_ID"].ToString() },
                    { "PROJECT_NAME", col["PROJECT_NAME"].ToString() },
                    { "DETAIL", col["DETAIL"].ToString() },
                    { "YEAR", col["YEAR"].ToString() },
                    { "USER_ID", col["USER_ID"].ToString() },
                    { "EDIT_DATE", col["EDIT_DATE"].ToString() },
                    { "SYSTEM_ID", col["SYSTEM_ID"].ToString() },
                    { "SYSTEM_NAME", col["SYSTEM_NAME"].ToString() },
                };

                data.Add(dict);
            }

            string sql2 = @"
                SELECT
                COUNT(*) AS COUNT
                FROM
                GDMS_PROJECT A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.SYSTEM_ID IN (SELECT SYSTEM_ID FROM GDMS_USER_SYSTEM WHERE USER_ID = '" + projectajax.userId + "') " + where;
            var ds2 = db.QueryT(sql2);
            foreach (DataRow col in ds2.Rows)
            {
                res.count = col["count"].ToString();
            }

            res.code = 0;
            res.msg = "";
            res.data = data;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //获取select
        [ActionName("select")]
        public HttpResponseMessage ProjectSelect([FromBody] ProjectAjax projectajax)
        {
            Db db = new Db();
            Response res = new Response();
            Dictionary<string, object> data = new Dictionary<string, object>();

            //查询系统select
            string sql1 = @"
                SELECT
                A.SYSTEM_ID AS SYSTEM_ID,
                B.NAME AS SYSTEM_NAME
                FROM
                GDMS_USER_SYSTEM A
                LEFT JOIN GDMS_SYSTEM B ON A.SYSTEM_ID = B.ID
                WHERE A.USER_ID = '" + projectajax.userId + "'";
            var ds1 = db.QueryT(sql1);
            Dictionary<string, string> dict1 = new Dictionary<string, string>();
            foreach (DataRow col in ds1.Rows)
            {
                dict1.Add(col["SYSTEM_ID"].ToString(), col["SYSTEM_NAME"].ToString());
            }
            data.Add("system", dict1);

            res.code = 0;
            res.msg = "";
            res.data = data;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //删除项目
        [ActionName("del")]
        public HttpResponseMessage ProjectDel([FromBody] String ajaxData)
        {
            Db db = new Db();
            JArray idArr = (JArray)JsonConvert.DeserializeObject(ajaxData);
            string sqlin = "";
            foreach (var devId in idArr)
            {
                sqlin = sqlin + devId + ",";
            }
            sqlin = sqlin.Substring(0, sqlin.Length - 1);
            string sql = "DELETE FROM GDMS_PROJECT WHERE ID IN (" + sqlin + ")";

            var rows = db.ExecuteSql(sql);
            Response res = new Response();

            res.code = 0;
            res.msg = "操作成功，删除了" + rows + "个项目";
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //添加项目
        [ActionName("add")]
        public HttpResponseMessage ProjectAdd([FromBody] String ajaxData)
        {
            JObject formData = (JObject)JsonConvert.DeserializeObject(ajaxData);
            Db db = new Db();
            string sql = @"INSERT INTO GDMS_PROJECT(ID,NAME,DETAIL,YEAR,SYSTEM_ID,USER_ID,EDIT_DATE) VALUES (
                GDMS_PROJECT_SEQ.nextVal, 
                '" + (String)formData["name"] + @"', 
                '" + (String)formData["detail"] + @"',
                '" + (String)formData["year"] + @"', 
                '" + (String)formData["system"] + @"',
                '" + (String)formData["userId"] + @"',
                SYSDATE
                )";
            var rows = db.ExecuteSql(sql);


            Response res = new Response();
            res.code = 0;
            res.msg = "添加成功"+rows;
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }

        //修改项目
        [ActionName("edit")]
        public HttpResponseMessage ProjectEdit([FromBody] String ajaxData)
        {
            JObject formData = (JObject)JsonConvert.DeserializeObject(ajaxData);
            Db db = new Db();
            string sql = @"UPDATE GDMS_PROJECT SET 
                SYSTEM_ID = '" + (String)formData["system"] + @"',
                NAME = '" + (String)formData["name"] + @"',
                DETAIL = '" + (String)formData["detail"] + @"',
                YEAR = '" + (String)formData["year"] + @"',
                USER_ID = '" + (String)formData["userId"] + @"',
                EDIT_DATE = SYSDATE
                WHERE ID = '" + (String)formData["projectId"] + "'";

            var rows = db.ExecuteSql(sql);

            Response res = new Response();
            res.code = 0;
            res.msg = "更新成功"+ rows;
            res.data = null;

            var resJsonStr = JsonConvert.SerializeObject(res);
            HttpResponseMessage resJson = new HttpResponseMessage
            {
                Content = new StringContent(resJsonStr, Encoding.GetEncoding("UTF-8"), "application/json")
            };
            return resJson;
        }
    }
}
