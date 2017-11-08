using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Collections.Specialized;
using System.Collections;
using TraceLogs;
using System.Configuration;

namespace PictrueServer.server
{
    public class Helper
    {

        private static string up_key=ConfigurationManager.AppSettings["up_key"];

        /// <summary>
        /// 验证签名
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="logger"></param>
        public static bool CheckParamSign(NameValueCollection obj, ILogger logger)
        {
            StringBuilder SignText = new StringBuilder();
            var Keys = new ArrayList(obj.Keys);
            Keys.Sort();//字典排序
            foreach (string key in Keys)
            {
                if (!string.IsNullOrEmpty(obj[key]))
                {
                    SignText.Append(key + "=" + obj[key] + "&");
                }
            }
            SignText.Append("key=" + up_key);
            string Sign =MD5(SignText.ToString());         
            if (!string.IsNullOrEmpty(obj["sign"])&&Sign == obj["sign"].ToUpper())
                return true;
            else
            {
                logger.Info("签名原串:"+ SignText.ToString());
                logger.Info("签名:" + Sign);
                return false;
            }
        }


        /// <summary>
        /// Md5大写32加密
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string MD5(string str)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] data = Encoding.UTF8.GetBytes(str);
            byte[] md5data = md5.ComputeHash(data);
            md5.Clear();
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < md5data.Length; i++)
            {
                sBuilder.Append(md5data[i].ToString("X2"));
                //X代表十六进制
                //2:代表每个数字2位
            }
            return sBuilder.ToString();
        }
    }
}