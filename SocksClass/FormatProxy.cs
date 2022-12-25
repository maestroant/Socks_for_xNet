using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using xNet;

namespace SocksClass
{
    /// <summary>
    /// Формат прокси из текста в ProxyClient для xNet https://github.com/X-rus/xNet/releases
    /// Форматы текстовых строк прокси. Если нет префикса используется http
    /// 1. ip:port
    /// 2. ip:port:user:pass
    /// 3. http://user:pass@ip:port
    /// 4. https://user:pass@ip:port
    /// 5. socks4://user:pass@ip:port
    /// 5. socks5://user:pass@ip:port
    /// </summary>
    internal class FormatProxy
    {
        /// <summary>
        /// Выбирает из списка случайный прокси проверяет, если таймаут или страна не подходит - удаление из списка proxyList
        /// </summary>
        /// <param name="proxyList">Список прокси, должен передаваться по ссылке ref. Перед вызовом в потоке нужно заблокировать lock</param>
        /// <param name="timeout">Таймаут проверки по умолчанию 5 сек</param>
        /// <param name="checkCountry">Если этот парметер введен, то проверяет нужную страну. По умолчанию проверки нет</param>
        /// <returns></returns>
        public static ProxyClient GetRandom(ref List<string> proxyList, int timeout = 5000, string checkCountry = "*") 
        {
            ProxyClient proxy = null;
            int index = 0;
            Random rand = new Random();

            try
            {

                while (proxyList.Count > 0)
                {
                    while ((index = rand.Next(proxyList.Count)) >= proxyList.Count);
                    proxy = Parse(proxyList[index]);
                    proxy.ConnectTimeout = timeout;

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
        private static ProxyClient Parse(string textProxy)
        {
            ProxyClient proxy = null;
            if (string.IsNullOrEmpty(textProxy))
                return null;

            try
            {
                if (textProxy.IndexOf("http://") == 0)
                {
                    string[] sub = textProxy.Remove(0, 9).Split('@');
                    if (ProxyClient.TryParse(ProxyType.Http, $"{sub[1]}:{sub[0]}", out proxy))
                        return proxy;
                }

                if (textProxy.IndexOf("socks4://") == 0)
                {
                    string[] sub = textProxy.Remove(0, 9).Split('@');
                    if (ProxyClient.TryParse(ProxyType.Socks4, $"{sub[1]}:{sub[0]}", out proxy))
                        return proxy;
                }

                if (textProxy.IndexOf("socks5://") == 0)
                {
                    string[] sub = textProxy.Remove(0, 9).Split('@');
                    if (ProxyClient.TryParse(ProxyType.Socks5, $"{sub[1]}:{sub[0]}", out proxy))
                        return proxy;
                }

                if (ProxyClient.TryParse(ProxyType.Http, textProxy, out proxy))
                    return proxy;

                return null;

            }
            catch (Exception)
            {
                return null;
            }
        }

         /// <summary>
         /// Тест на скорость к гуглу
         /// </summary>
        private static bool Test(ProxyClient proxy, int timeout)
        {

            try
            {
                using (var request = new HttpRequest())
                {
                    System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                    request.Proxy = proxy;
                    request.ConnectTimeout = timeout;
                    request.UserAgent = Http.ChromeUserAgent();
                    request.ConnectTimeout = timeout;
                    string content = request.Get("https://google.com").ToString();
                    return true;
                }
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
        private static string CheckCountry(ProxyClient proxy, int timeout)
        {
            try
            {
                using (var request = new HttpRequest())
                {
                    request.Proxy = proxy;
                    request.ConnectTimeout = timeout;
                    string content = request.Get("http://ip-api.com/json").ToString();
                    return (string)JObject.Parse(content)["countryCode"];
                }
            }
            catch (Exception)
            {
                return null;
            }
        }
        
    }
}
