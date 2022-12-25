using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using xNet;

namespace SocksClass
{
    internal class Program
    {
        const string proxyFile = "proxy.txt";

        private static List<string> ProxyList = new List<string>();

        static void Main(string[] args)
        {
            using (StreamReader reader = File.OpenText("proxy.txt"))
            {
                string line = "";
                while ((line = reader.ReadLine()) != null)
                {
                    if ((string.IsNullOrEmpty(line)) || (line.Length < 5)) continue;
                    ProxyList.Add(line);
                }
            }

            ProxyClient wp;

            lock (ProxyList)
                wp = FormatProxy.GetRandom(ref ProxyList, 5000);



            try
            {
                using (var request = new HttpRequest())
                {
                    request.Proxy = wp;
                    request.Get("habrahabr.ru");
                }
            }
            catch (HttpException ex)
            {
                Console.WriteLine("Произошла ошибка при работе с HTTP-сервером: {0}", ex.Message);

                switch (ex.Status)
                {
                    case HttpExceptionStatus.Other:
                        Console.WriteLine("Неизвестная ошибка");
                        break;

                    case HttpExceptionStatus.ProtocolError:
                        Console.WriteLine("Код состояния: {0}", (int)ex.HttpStatusCode);
                        break;

                    case HttpExceptionStatus.ConnectFailure:
                        Console.WriteLine("Не удалось соединиться с HTTP-сервером.");
                        break;

                    case HttpExceptionStatus.SendFailure:
                        Console.WriteLine("Не удалось отправить запрос HTTP-серверу.");
                        break;

                    case HttpExceptionStatus.ReceiveFailure:
                        Console.WriteLine("Не удалось загрузить ответ от HTTP-сервера.");
                        break;
                }
            }



            Console.ReadLine(); 
        }
    }
}
