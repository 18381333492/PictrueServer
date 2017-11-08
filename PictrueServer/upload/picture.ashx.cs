using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using Newtonsoft.Json;
using System.Drawing;
using TraceLogs;
using PictrueServer.server;

namespace PictrueServer.upload
{
    /// <summary>
    /// 图片上传一般处理程序
    /// </summary>
    public class picture : IHttpHandler
    {
        //图片的最大尺寸
        private static int _iMaxSize = Convert.ToInt32(ConfigurationManager.AppSettings["MaxPictureSize"]);

        private static ILogger logger = LoggerManager.Instance.GetSLogger("picture");

        public void ProcessRequest(HttpContext context)
        {
            var res = new result();
            context.Response.Clear();
            context.Response.ContentType = "text/plain";
            context.Response.AppendHeader("Access-Control-Allow-Origin", "*");//允许跨域
            try
            {
                logger.Info("-----------------上传图片开始-------------------");
                logger.Info("url:" + context.Request.Url.ToString());
                var sUrl = new List<string>();//存储图片地址
                string sDirectorieName = context.Request["dir"];//存放图片的路径目录
                string timestamp = context.Request["timestamp"];//时间戳
                string sign = context.Request["sign"];//上传的签名
                if (string.IsNullOrEmpty(timestamp)|| Math.Abs((DateTime.Parse(timestamp)-DateTime.Now).Minutes)>2)
                {
                    res.message = "时间戳错误!";
                    logger.Info("返回结果:" + res.json());
                    context.Response.Write(res.json());
                    return;
                }
                //验证签名
                if (!Helper.CheckParamSign(context.Request.QueryString, logger))
                {
                    res.message = "验证签名失败!";
                    logger.Info("返回结果:" + res.json());
                    context.Response.Write(res.json());
                    return;
                }
                HttpFileCollection ImgList = context.Request.Files;
                for (var i = 0; i < ImgList.Count; i++)
                {
                    HttpPostedFile picture = ImgList[i];
                    if (picture != null && picture.ContentLength > 0)
                    {//文件正常
                        var extension_list = ConfigurationManager.AppSettings["extension"].Split(',');
                        //获取文件后缀名
                        string format = System.IO.Path.GetExtension(picture.FileName);
                        //判断图片后缀名
                        if (!extension_list.Contains(format.ToLower()))
                        {
                            res.message = "上传的图片文件格式错误!";
                            logger.Info("返回结果:" + res.json());
                            context.Response.Write(res.json());
                            return;
                        }
                        //判断文件大小
                        if (picture.ContentLength > _iMaxSize * 1024 * 1024)
                        {
                            res.message = string.Format("文件超出大小限制{0}M!", _iMaxSize);
                            logger.Info("返回结果:" + res.json());
                            context.Response.Write(res.json());
                            return;
                        }
                        /*图片保存路径的根目录*/
                        string sPath = ConfigurationManager.AppSettings["root"];
                        string dDate = DateTime.Now.ToString("yyyy-MM");
                        if (!string.IsNullOrEmpty(sDirectorieName))
                            sPath = sPath + sDirectorieName + "\\" + dDate + "\\";
                        else
                            sPath = sPath + "others" + "\\" + dDate + "\\";
                        //保存的图片名称
                        string sFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + format;
                        //图片的网络地址
                        string pictureUrl = string.Format("http://{0}:{1}/{2}{3}", context.Request.Url.Host, context.Request.Url.Port, sPath.Replace('\\', '/'), sFileName);
                        //检查目录是否存在
                        sPath = AppDomain.CurrentDomain.BaseDirectory + sPath;
                        if (!Directory.Exists(sPath))
                        {
                            Directory.CreateDirectory(sPath);
                        }
                        /*保存图片到本地*/
                        Image img = Bitmap.FromStream(picture.InputStream);
                        img.Save(sPath + sFileName);
                        sUrl.Add(pictureUrl);
                    }
                }
                if (sUrl.Count > 0)
                {
                    res.error = 0;
                    res.url = sUrl;
                    res.message = "上传成功";
                }
                else
                { 
                    res.message = "no picture file!";
                }
                logger.Info("返回结果:" + res.json());
                context.Response.Write(res.json());
            }
            catch (Exception e)
            {
                logger.Info(e.Message);
                logger.Fatal(e);
                res.message = "Server Error!";
                logger.Info("返回结果:" + res.json());
                context.Response.Write(res.json());
            }
        }

      
        public bool IsReusable
        {
            get
            {
                return false;
            }
        }
    }


    public class result
    {
        public int error=1;
        public string message;
        public object url;

        public string json()
        {
            return  JsonConvert.SerializeObject(this);
        }
    }
}