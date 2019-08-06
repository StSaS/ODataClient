using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace TestConsoleApp
{
    class Program
    {

        private const string serverUri = "http://localhost:82/0/ServiceModel/EntityDataService.svc/";
        private const string authServiceUtri = "http://localhost:82/ServiceModel/AuthService.svc/Login";

        private const string Login = "Supervisor";
        private const string Password = "Supervisor";
        

        private static readonly XNamespace ds = "http://schemas.microsoft.com/ado/2007/08/dataservices";
        private static readonly XNamespace dsmd = "http://schemas.microsoft.com/ado/2007/08/dataservices/metadata";
        private static readonly XNamespace atom = "http://www.w3.org/2005/Atom";

        public static void GetOdataCollectionContactCommunication(string userName, string userPassword)
        {
            // Создание запроса на аутентификацию.
            var authRequest = HttpWebRequest.Create(authServiceUtri) as HttpWebRequest;
            authRequest.Method = "POST";
            authRequest.ContentType = "application/json";
            var bpmCookieContainer = new CookieContainer();
            // Включение использования cookie в запросе.
            authRequest.CookieContainer = bpmCookieContainer;
            // Получение потока, ассоциированного с запросом на аутентификацию.
            using (var requestStream = authRequest.GetRequestStream())
            {
                // Запись в поток запроса учетных данных пользователя BPMonline и дополнительных параметров запроса.
                using (var writer = new StreamWriter(requestStream))
                {
                    writer.Write(@"{
                                ""UserName"":""" + userName + @""",
                                ""UserPassword"":""" + userPassword + @""",
                                ""SolutionName"":""TSBpm"",
                                ""TimeZoneOffset"":-120,
                                ""Language"":""Ru-ru""
                                }");
                }
            }

            using (var response = (HttpWebResponse)authRequest.GetResponse())
            {
                // Создание запроса на получение данных от сервиса OData.
                var dataRequest = HttpWebRequest.Create(serverUri + "ContactCommunicationCollection?select=Id, Number")
                                        as HttpWebRequest;
                // Для получения данных используется HTTP-метод GET.
                dataRequest.Method = "GET";
                // Добавление полученных ранее аутентификационных cookie в запрос на получение данных.
                dataRequest.CookieContainer = bpmCookieContainer;
                // Получение ответа от сервера.
                using (var dataResponse = (HttpWebResponse)dataRequest.GetResponse())
                {
                    // Загрузка ответа сервера в xml-документ для дальнейшей обработки.
                    XDocument xmlDoc = XDocument.Load(dataResponse.GetResponseStream());
                    // Получение коллекции объектов контактов, соответствующих условию запроса.
                    var contacts = from entry in xmlDoc.Descendants(atom + "entry")
                                   select new
                                   {
                                       Id = new Guid(entry.Element(atom + "content")
                                                              .Element(dsmd + "properties")
                                                              .Element(ds + "Id").Value),
                                       Number = entry.Element(atom + "content")
                                                       .Element(dsmd + "properties")
                                                       .Element(ds + "Number").Value
                                   };
                    foreach (var contact in contacts)
                    {
                        // Выполнение действий с контактами.
                        Console.WriteLine(contact.ToString());
                    }
                }
            }
        }

        public static void CreateODataRecordContactCommunication()
        {
            // Создание сообщения xml, содержащего данные о создаваемом объекте.

            var content = new XElement(dsmd + "properties",
                          new XElement(ds + "CommunicationTypeId", "3DDDB3CC-53EE-49C4-A71F-E9E257F59E49"),
                          new XElement(ds + "Number", "66666-555555"),
                          new XElement(ds + "ContactId","46F565C1-5F30-431D-B48F-D8671984D848"));

            var entry = new XElement(atom + "entry",
                        new XElement(atom + "content",
                        new XAttribute("type", "application/xml"), content));
            Console.WriteLine(entry.ToString());
            // Создание запроса к сервису, который будет добавлять новый объект в коллекцию контактов.
            var request = (HttpWebRequest)HttpWebRequest.Create(serverUri + "ContactCommunicationCollection/");
            request.Credentials = new NetworkCredential(Login, Password);
            request.Method = "POST";
            request.Accept = "application/atom+xml";
            request.ContentType = "application/atom+xml;type=entry";
            // Запись xml-сообщения в поток запроса.
            using (var writer = XmlWriter.Create(request.GetRequestStream()))
            {
                entry.WriteTo(writer);
            }
            // Получение ответа от сервиса о результате выполнения операции.
            using (WebResponse response = request.GetResponse())
            {
                if (((HttpWebResponse)response).StatusCode == HttpStatusCode.Created)
                {
                    // Обработка результата выполнения операции.
                    Console.WriteLine("Created");
                }
            }
        }

        public static void DeleteRecordContactCommunication(string contactId)
        {
            // Id записи объекта, который необходимо удалить.
            //string contactId = "29076C61-D501-4C9E-B171-4A51AC0910E8";
            // Создание запроса к сервису, который будет удалять данные.
            var request = (HttpWebRequest)HttpWebRequest.Create(serverUri
                    + "ContactCommunicationCollection(guid'" + contactId + "')");
            request.Credentials = new NetworkCredential(Login, Password);
            request.Method = "DELETE";
            // Получение ответа от сервися о результате выполненя операции.
            using (WebResponse response = request.GetResponse())
            {
                // Обработка результата выполнения операции.
                Console.WriteLine("Delete Success");
            }
        }

        static void Main(string[] args)
        {



            GetOdataCollectionContactCommunication(Login, Password);
            CreateODataRecordContactCommunication();
            //DeleteRecordContactCommunication("29076C61-D501-4C9E-B171-4A51AC0910E8");
            GetOdataCollectionContactCommunication(Login, Password);



        }
    }
}
