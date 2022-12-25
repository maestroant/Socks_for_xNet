using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using xNet;

namespace SocksClass
{
    /// <summary>
    /// Формат прокси для класса WebProxy
    /// ip:port
    /// ip:port:user:pass
    /// </summary>
    internal class FormatWebProxy
    {
        /// <summary>
        /// Выбирает из списка случайный прокси проверяет, если таймаут или страна не подходит - удаление из списка proxyList
        /// </summary>
        /// <param name="proxyList">Список прокси, должен передаваться по ссылке ref. Перед вызовом в потоке нужно заблокировать lock</param>
        /// <param name="timeout">Таймаут проверки по умолчанию 5 сек</param>
        /// <param name="checkCountry">Если этот парметер введен, то проверяет нужную страну. По умолчанию проверки нет</param>
        /// <returns></returns>
        public static WebProxy GetRandom(ref List<string> proxyList, int timeout = 5000, string checkCountry = "*")
        {
            WebProxy proxy = null;
            int index = 0;
            Random rand = new Random();

            try
            {

                while (proxyList.Count > 0)
                {
                    while ((index = rand.Next(proxyList.Count)) >= proxyList.Count) ;
                    proxy = Parse(proxyList[index]);

                    if (proxy != null)
                    {
                        if (checkCountry == "*")
                        {
                            if (Test(proxy, timeout))
                                return proxy;
                        }
                        else
                        {
                            if (CheckCountry(proxy, timeout) == checkCountry.ToUpper())
                                return proxy;
                        }

                        proxyList.RemoveAt(index);
                    }
                    else
                    {
                        proxyList.RemoveAt(index);
                    }

                }

                // список пуст !!!
                return null;

            }
            catch (Exception)
            {
                //
                return null;
            }
        }

        /// <summary>
        /// Парсит строку находит нужный формат
        /// </summary>
        private static WebProxy Parse(string textProxy)
        {
            WebProxy proxy = null;
            if (string.IsNullOrEmpty(textProxy))
                return null;

            try
            {
                string[] sub = textProxy.Split(':');
                switch (sub.Length)
                {
                    case 2:
                        proxy = new WebProxy(textProxy, true, null);
                        break;
                    case 4:
                        proxy = new WebProxy(textProxy, true);
                        proxy.Credentials = new NetworkCredential(sub[2], sub[3]);
                        break;                     

                }

            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        /// <summary>
        /// Тест на скорость к гуглу
        /// </summary>
        private static bool Test(WebProxy proxy, int timeout)
        {

            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create("http://google.com");
                req.Timeout = timeout;
                req.Proxy = proxy;
                var resp = req.GetResponse();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Получает инфформацию IP
        /// </summary>
        /// <returns>Возвращает страну. Например: US или DK </returns>
        private static string CheckCountry(WebProxy proxy, int timeout)
        {
            try
            {
                var req = (HttpWebRequest)HttpWebRequest.Create("http://ip-api.com/json");
                req.Timeout = timeout;
                req.Proxy = proxy;
                var resp = req.GetResponse();
                var json = new StreamReader(resp.GetResponseStream()).ReadToEnd();

                var myip = (string)JObject.Parse(json)["query"];
                return myip;
  
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
