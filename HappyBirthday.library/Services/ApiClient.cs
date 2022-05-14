using Newtonsoft.Json;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace HappyBirthday.Services
{
    public class ApiClient
    {
        public static readonly HttpClient Client = new HttpClient();

        /// <summary>
        /// Send http request and return T object as response
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="endpoint"></param>
        /// <param name="method"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public async Task<T> SendRequest<T>(string endpoint, HttpMethod method, string data) where T : class
        {
            var request = new HttpRequestMessage(method, endpoint);
            request.Content = new StringContent(data, Encoding.UTF8, "application/json");

            T result;
            using (HttpResponseMessage response = await Client.SendAsync(request))
            {
                string content = await response.Content.ReadAsStringAsync();
                result = JsonConvert.DeserializeObject<T>(content);
            }

            return result;
        }
    }
}
