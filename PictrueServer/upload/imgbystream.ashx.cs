using TraceLogs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Web;
using PictrueServer.server;

namespace PictrueServer.upload
{
    /// <summary>
    /// 已文件流的形式上传图片
    /// </summary>
    public class imgbystream : IHttpHandler
    {
        //图片的最大尺寸
        private static int _iMaxSize = Convert.ToInt32(ConfigurationManager.AppSettings["MaxPictureSize"]);

        private static ILogger logger = LoggerManager.Instance.GetSLogger("imgbystream");
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
                var sDirectorieName = context.Request["dir"];//存放图片的路径目录
                string timestamp = context.Request["timestamp"];//时间戳
                string sign = context.Request["sign"];//上传的签名
                if (string.IsNullOrEmpty(timestamp) || Math.Abs((DateTime.Parse(timestamp) - DateTime.Now).Minutes) > 2)
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
                var imgStream = context.Request.InputStream;//获取上传的文件流
                if (imgStream.Length > 0)
                {//判断流的长度
                     //判断文件大小
                    if (imgStream.Length > _iMaxSize * 1024 * 1024)
                    {
                        res.message = string.Format("文件超出大小限制{0}M!", _iMaxSize);
                        logger.Info("返回结果:" + res.json());
                        context.Response.Write(res.json());
                        return;
                    }
                    /*图片保存路径的根目录*/
                    string sPath = ConfigurationManager.AppSettings["root"];
                    string dDate = DateTime.Now.ToString("yyyy-MM");
                    //保存的图片文件名
                    string sFileName = DateTime.Now.ToString("yyyyMMddHHmmssfff") + ".jpg";
                    //图片的网络地址
                    string pictureUrl = string.Format("http://{0}:{1}/{2}{3}", context.Request.Url.Host, context.Request.Url.Port, sPath.Replace('\\', '/'), sFileName);
                    //检查目录是否存在
                    sPath = AppDomain.CurrentDomain.BaseDirectory + sPath;
                    if (!Directory.Exists(sPath))
                    {
                        Directory.CreateDirectory(sPath);
                    }
                    Image img = Bitmap.FromStream(imgStream);
                    img.Save(sPath + sFileName,ImageFormat.Jpeg);
                    res.error = 0;
                    res.url = sUrl;
                    res.message = "上传成功";
                }
                else
                {
                    res.message = "InputStream Length Is Zero";
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
}