using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Formatting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ConsoleApplication1
{
    class Program
    {
        private static HttpClient httpClient = null;
        private static string serverURL = "https://02.g3stiona.com/rest/";
        private static string addon = ADDON_CORRESPONDIENTE;
        private static Dictionary<string, string> recursosDictionary = new Dictionary<string, string>();
        private static string token = null;
        private static string accessToken = null;
        private static bool tokenAutorizado = false;
        
        static void Main(string[] args)
        {
            try
            {
                httpClient = new HttpClient();
                httpClient.BaseAddress = new Uri(serverURL);

                // Obtenemos los Bookmarks de los recursos de la API, para a partir
                // de ellos empezar a 'navegar' haciendo las peticiones.
                getRecursos();

                // Creamos un token de acceso, al crearlo estará en estado
                // 'pendiente de autorizar' a la espera de que nos logueemos con un
                // usuario y lo autoricemos
                if (token == null)
                    token = createToken();

                // Comprobamos el estado del token
                while (tokenAutorizado == false)
                    tokenAutorizado = comprobarToken(token);

                Console.WriteLine("============================== LOGIN CORRECTO ==============================");

                // Obtenemos la oficina de registro en la que queremos crear la
                // anotación
                OficinaRegistro or = getOficinaRegistro("RC");

                // Creamos el tercero y el solicitante a añadir en las anotaciones
                Tercero tercero = new Tercero();
                tercero.nif = "33333333T";
                tercero.name = "Tercero-CSharp-03";
                tercero.relation = "INVOLVED";
                tercero.address = "address";
                tercero.zone = "zone";
                tercero.country = "España";
                tercero.province = "Zaragoza";
                tercero.zipCode = "50009";
                tercero.notificationChannel = "PAPER";
                tercero.personType = "JURIDICAL";

                Tercero provider = new Tercero();
                provider.nif = "44444444P";
                provider.name = "Tercero-CSharp-04";
                provider.address = "address";
                provider.zone = "zone";
                provider.country = "España";
                provider.province = "Zaragoza";
                provider.zipCode = "50012";
                provider.notificationChannel = "PAPER";
                provider.personType = "JURIDICAL";

                for (int i = 0; i < 3; i++)
                {
                    // Creamos la anotación
                    Anotacion anotacion = new Anotacion();
                    anotacion.incomeType = "PRESENTIAL";
                    anotacion.shortDescription = "API prueba rendimiento";
                    anotacion.classification = "REQUERIMENT";
                    anotacion.longDescription = "Aqui van las observaciones de la anotacíon";
                    anotacion.originCode = "C0D-O4161N";
                    anotacion = crearAnotacion(or.links[1].href, anotacion);

                    // Añadirmos el solicitante y el tercero
                    addTercero(anotacion, provider, true);
                    addTercero(anotacion, tercero, false);

                    // Subimos un documento a la anotación
                    String upload = crearUpload();
                    bool subido = subirFichero(upload, "C:/Users/eedevadmin/Downloads/a.pdf");
                    addFileToAnotacion(anotacion, upload, "documentoPrueba");

                    // Finalizamos la anotación
                    finalizarAnotacion(anotacion);
                }

                Console.WriteLine("Pulse una tecla para continuar...");
                Console.ReadLine();
            }
            finally
            {
                if (httpClient != null)
                    httpClient = null;
            }
        }

        /**
         * Rellena el Dictionary 'recursosDictionary' con todos los bookmarks de la API
         */
        private static void getRecursos()
        {

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(serverURL),
                Method = HttpMethod.Get
            };

            // Cabecera con el addon-token
            request.Headers.Add("X-Gestiona-Addon-Token", addon);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new System.InvalidOperationException("Failed: HTTP error code: " + response.StatusCode);
            }
            else
            {
                Recursos recurso = response.Content.ReadAsAsync<Recursos>(new[] { new LinksFormatter() }).Result;

                foreach (Link link in recurso.links)
                {
                    Console.WriteLine("{0}\n\r{1}\n\r", link.rel, link.href);
                    recursosDictionary.Add(link.rel, link.href);
                }
            }
        }

        /**
         * Crea el token con el que tendremos que loguearnos para obtener la autorización
         */
        private static string createToken()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(recursosDictionary["vnd.gestiona.addon.authorizations"]),
                Method = HttpMethod.Post
            };

            // Cabecera con el addon-token
            request.Headers.Add("X-Gestiona-Addon-Token", addon);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                string location = response.Headers.Location.ToString();

                token = location.Substring(location.LastIndexOf('/') + 1);

                Console.WriteLine("::TOKEN ==> " + token);

                return token;
            }
            else if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new System.InvalidOperationException("Error al crear el accessToken, no se encuentra el addon " + addon);
            }
            else
            {
                throw new System.InvalidOperationException("Error al crear el accessToken: " + response.StatusCode);
            }
        }

        /**
         * Compueba que el token que se le pasa como parámetro esté en estado autorizado. En
         * caso de estar pendiente de autorización nos devuelve la URL en la que nos debemos
         * loguear con un usuario y contraseña para autorizar ese token
         */
        private static bool comprobarToken(string token)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(recursosDictionary["vnd.gestiona.addon.authorizations"] + "/" + token),
                Method = HttpMethod.Get
            };

            // Cabecera con el addon-token
            request.Headers.Add("X-Gestiona-Addon-Token", addon);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            // Si devuelve estado 200 es que ya está autorizado y devuelve los datos del
            // token y el access-token
            if (response.StatusCode == HttpStatusCode.OK)
            {
                AddonAuthorization addonAuthorization = response.Content.ReadAsAsync<AddonAuthorization>(new[] { new AddonAuthorizationFormatter() }).Result;

                accessToken = addonAuthorization.access_token;

                Console.WriteLine("token: " + token + " accessToken: " + accessToken);

                return true;
            }
            // Si devuelve estado 401, nos muestra la URL en la que el usuario se debe
            // loguear
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                string urlLogin = response.Headers.Location.ToString();

                Console.WriteLine("Entre en esta URL y logueese con su usuario y contraseña para validar el token: \n{0}\n [Pulse intro cuando ya lo haya actualizado]", urlLogin);

                Console.ReadLine();

                return false;
            }
            else
            {
                RestError restError = response.Content.ReadAsAsync<RestError>(new[] { new RestErrorFormatter() }).Result;

                throw new System.InvalidOperationException("Error al comprobar autorización: " + restError.description);
            }
        }

        /**
         * Buscar oficina de registro según el código de la oficina que se le pasa como
         * parámetro
         */
        private static OficinaRegistro getOficinaRegistro(string code)
        {
            RestRegistryOfficeFilter restRegistryOfficeFilter = new RestRegistryOfficeFilter();
            restRegistryOfficeFilter.code = code;

            string b64 = base64Encode(Newtonsoft.Json.JsonConvert.SerializeObject(restRegistryOfficeFilter));

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(recursosDictionary["vnd.gestiona.registry.offices"] + "?filter-view=" + b64),
                Method = HttpMethod.Get
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                OficinaRegistroContent oficinaRegistro = response.Content.ReadAsAsync<OficinaRegistroContent>(new[] { new OficinaRegistroFormatter() }).Result;

               return oficinaRegistro.content[0];
            }
            else
            {
                throw new System.InvalidOperationException("Error al obtener la oficina de registro: " + response.ReasonPhrase);
            }
        }

        /**
         * Harña el POST sobre la uri que le pasamos para crear la anotación con los datos
         * que le pasamos en el objeto Anotacion
         */
        private static Anotacion crearAnotacion(string uri, Anotacion anotacion)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Post
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            request.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(anotacion));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.gestiona.registry-annotation");
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                string location = response.Headers.Location.ToString();

                return getAnotacion(location);
            }
            else
            {
                throw new System.InvalidOperationException("Error al crear anotación: " + response.ReasonPhrase);
            }
        }

        /**
         * Dado el link de una anotación existente, hará la petición GET y nos devolverá los
         * datos de dicha anotación mapeados en el objeto Anotacion que nos hemos creado
         */
        private static Anotacion getAnotacion(string uri)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Get
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                Anotacion anotacion = response.Content.ReadAsAsync<Anotacion>(new[] { new AnotacionFormatter() }).Result;

                return anotacion;
            }
            else
            {
                throw new System.InvalidOperationException("Error al obtener anotación: " + response.StatusCode);
            }
        }

        /**
         * Crea un nuevo recurso upload sobre el cual tendremos que hacer la subida del
         * fichero posteriormente
         */
        private static string crearUpload()
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(recursosDictionary["vnd.gestiona.uploads"]),
                Method = HttpMethod.Post
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                string location = response.Headers.Location.ToString();

                return location;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new System.InvalidOperationException("Error al crear el upload, no tiene autorización");
            }
            else
            {
                throw new System.InvalidOperationException("Error al crear el upload: " + response.StatusCode);
            }
        }

        /**
         * Hace el PUT para subir el fichero
         */
        private static bool subirFichero(string uri, string pathfile)
        {
            FileStream file = File.Open(pathfile, FileMode.Open);
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(uri),
                Method = HttpMethod.Put
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            request.Headers.Add("Accept", "application/octet-stream");
            request.Headers.Add("Slug", "prueba.pdf");
            request.Content = new StreamContent(file);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
               return true;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new System.InvalidOperationException("Error al crear upload: no tiene autorización");
            }
            else
            {
                throw new System.InvalidOperationException("Error al crear upload: " + response.ReasonPhrase);
            }
        }

        /**
         * Añadir un documento a la anotación
         */
        private static bool addFileToAnotacion(Anotacion anotacion, string uri, string nombreDoc)
        {
            if (anotacion == null || uri == null || nombreDoc == null)
                return false;

            AnnotationDocument annotationDocument = new AnnotationDocument();
            annotationDocument.name = nombreDoc;
            annotationDocument.type = "DIGITAL";

            Link link = new Link();
            link.rel = "content";
            link.href = uri;
            Link[] arrayLinks = new Link[1] { link };
            annotationDocument.links = arrayLinks;

            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(anotacion.links[5].href),
                Method = HttpMethod.Post
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            request.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(annotationDocument));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.gestiona.annotation-document");
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /**
         * Finaliza la anotación
         */
        private static bool finalizarAnotacion(Anotacion anotacion)
        {
            var request = new HttpRequestMessage()
            {
                RequestUri = new Uri(anotacion.links[6].href),
                Method = HttpMethod.Post
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new System.InvalidOperationException("Error al finalizar anotación, no tiene autorización");
            }
            else
            {
                throw new System.InvalidOperationException("Error al finalizar anotación: " + response.ReasonPhrase);
            }
        }

        /**
         * Añade el tercero que se le pasa como parámetro a la anotación que también se le
         * pasa como parámetro
         */
        private static bool addTercero(Anotacion anotacion, Tercero tercero, bool isProvider)
        {
            Uri uri = null;
            if (isProvider)
            {
                uri = new Uri(anotacion.links[2].href);
            }
            else
            {
                uri = new Uri(anotacion.links[3].href);
            }

            var request = new HttpRequestMessage()
            {
                RequestUri = uri,
                Method = HttpMethod.Post
            };
            request.Headers.Add("X-Gestiona-Access-Token", accessToken);
            request.Content = new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(tercero));
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/vnd.gestiona.thirdparty");
            HttpResponseMessage response = httpClient.SendAsync(request).Result;

            if (response.StatusCode == HttpStatusCode.Created)
            {
                return true;
            }
            else if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                throw new System.InvalidOperationException("Error al añadir tercero: no tiene autorización " + response.ReasonPhrase);
            }
            else
            {
                throw new System.InvalidOperationException("Error al añadir tercero: " + response.StatusCode);
            }
        }

        /**
         * Realiza la codificación en base64, utilizado para los filtros de búsqueda
         */
        private static string base64Encode(string text)
        {
            var textBytes = System.Text.Encoding.UTF8.GetBytes(text);
            return System.Convert.ToBase64String(textBytes);
        }
    }
}
