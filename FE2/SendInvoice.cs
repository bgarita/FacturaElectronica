using MySqlConnector;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace FE2
{
    class SendInvoice
    {
        private static string CurrentDirectory; // Carpeta desde donde se llama a este programa
        private static string JsonDir;          // Carpeta que contiene el Json con la estructura completa de carpetas del sistema.
        private static DirectoryStructure ds;   // Clase que carga y devuelve una instancia de la estructura de carpetas del sistema.
        private static Dir DIR;                 // Clase que contiene toda la estructura que viene en el Json
        private static EscribirMensaje Bitacora;// Clase que escribe en una bitácora
        private static string UserConfig;       // Archivo (ruta completa) donde se guardan los datos de conexión a la base de datos.
        private static Configuracion Config;
        private static Int32 EsPrueba;          // 0=Producción, 1=Prueba
        private static Certificado Cert;        // Certificado digital

        private static string Facnume;          // Documento en osais
        private static string Facnd;            // Identificador del documento en osais
        private static string XmlFile;          // Nombre (sin ruta) del archivo xml
        private static string Accion;           // (1=Enviar archivo, 2=Consultar estado, 3=Recibir xml)
        private static string TipoDoc;          // Tipo de documento electrónico
        private static string ClaveDoc;         // Clave del documento
        private static string CompanyHome;      // Carpeta home de la compañía
        private static string CedulaReceptor;   // Nuestra cédula
        private static string TipoCedulaReceptor; // Nuestro tipo de cédula
        private static string CedulaProveedor;
        private static string TipoCedulaProveedor; // Tipo de cédula Proveedor
        private static string ConsecutivoProveedor;




        static void Main(string[] args)
        {
            if (args[0] == "Test")
            {
                CurrentDirectory = Directory.GetCurrentDirectory();
                Console.WriteLine("Línea 1 - Carpeta desde donde se hizo la llamada {0} ", CurrentDirectory);
                Console.WriteLine("Línea 2 - Número de argumentos recibidos {0} ", args.Length);
                return;
            }

            /*
             * Habilitar este código cuando solo se desee probar la conexión
            if (DoTest())
            {
                return;
            }
            */
            // Obtener el la carpeta desde donde se hizo la llamada a este programa.
            CurrentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine(CurrentDirectory);

            
            // DEBUG - xml ventas
            // --------------------------------------------------------------------------------
            /*
            if (args == null || args.Length == 0)
            {
                args = new string[8];
                args[0] = "10006342.xml";   // Nombre del XML
                args[1] = "10006342";       // Facnume 
                args[2] = "2";              // Acción (1=Enviar archivo, 2=Consultar estado, 3=Recibir xml, 4=Consultar estado proveedores)
                args[3] = "FAC";            // TipoDoc documento (FAC, NDB, NCR, FCO)
                args[4] = "C:\\Java Programs\\osais\\laflor";         // Carpeta home de la compañía
                args[5] = "50606032100310117850600100001010000000049100782415";   // Clave del documento
                args[6] = "02"; // Tipo de cédula del receptor.  Solo se usa para confirmar xmls
                args[7] = "02"; // Tipo de cédula del proveedor.  Solo se usa para confirmar xmls
            }
            */
            // --------------------------------------------------------------------------------
            /*
            // DEBUG - xml compras
            if (args == null || args.Length == 0)
            {
                args = new string[8];
                args[0] = "00100001050000000006.xml";   // Nombre del XML
                args[1] = "3101026903";      // Cédula del proveedor
                args[2] = "3";              // Acción (1=Enviar archivo, 2=Consultar estado, 3=Recibir xml, 4=Consultar estado proveedores)
                args[3] = "003101178506";   // Cédula del receptor (nosotros)
                args[4] = "C:\\Java Programs\\osais\\laflor";         // Carpeta home de la compañía
                args[5] = "50607012000310102690300100001010000003632199999999";   // Clave del documento
                args[6] = "02"; // Tipo de cédula del receptor.  Solo se usa para confirmar xmls
                args[7] = "02"; // Tipo de cédula del proveedor.  Solo se usa para confirmar xmls
            }
            // --------------------------------------------------------------------------------
            */
            Console.WriteLine("Parámetros recibidos:");
            for (int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("Argumento " + i + " = " + args[i]);
            }

            if (args == null || args.Length < 8)
            {
                Console.WriteLine("Error en el número de parámetros recibidos");
                return;
            }

            // Trasladar los argumentos a las variables de la clase.
            XmlFile     = args[0];
            Facnume     = args[1];
            Accion      = args[2];
            TipoDoc     = args[3];
            CompanyHome = args[4];
            ClaveDoc    = args[5];
            TipoCedulaReceptor = args[6];
            TipoCedulaProveedor = args[7];

            ConsecutivoProveedor = new ClaveDocumento(ClaveDoc).Consecutivo;
            CedulaReceptor = new ClaveDocumento(ClaveDoc).Cedula;

            // Falta definir de dónde se toma el valor de la cédula del proveedor

            if (TipoDoc.Equals("NCR"))
            {
                Facnd = Math.Abs(Int32.Parse(Facnume)) + "";
            }
            else if (TipoDoc.Equals("NDB"))
            {
                Facnd = "-" + Facnume;
            }
            else
            {
                Facnd = "0";
            }



            // Construir la ruta para el Json que contiene la estructura completa de la compañía actual.
            JsonDir = (CompanyHome.Length > 0 ? CompanyHome : CurrentDirectory) + "\\";

            // Obtener la configuración para la conexión a base de datos y los datos para conectar con Hacienda
            UserConfig = (CompanyHome.Length > 0 ? CompanyHome : CurrentDirectory) + "\\UserConfig.txt";

            Console.WriteLine("\nJson file location: {0}", JsonDir);
            Console.WriteLine("UserConfig file: {0}", UserConfig);


            // Esta conexión solo se hace para validar que el ejecutable tenga acceso a la base de datos
            // antes de intentar hacer el envío a Hacienda.  Esto se hace para evitar que se haga un 
            // envío y no quede registrado en la base de datos.
            Config = new Configuracion(UserConfig);
            if (!Config.ConnectedToDatabase)
            {
                Console.WriteLine("Could not connect to server -- " + Config.ErrorMessage);
            }

            if (Config.ConnectedToDatabase)
            {
                Console.WriteLine("Connected to server " + Config.Server);
            }

            EsPrueba = Config.EsPrueba;

            Console.WriteLine("\nTrabajando en ambiente de {0}\n", (EsPrueba == 1 ? "PRUEBAS" : "PRODUCCION"));

            // El archivo del certificado digital debe estar en la misma ruta que el JsonDir
            // Es importante que en el archivo UserConfig.txt solo esté el nombre, no la ruta.
            Config.ArchivoCertificado = JsonDir + Config.ArchivoCertificado;

            if (!File.Exists(Config.ArchivoCertificado))
            {
                Console.WriteLine("\nEl certificado digital no existe [{0}]\n", Config.ArchivoCertificado);
                return;
            }

            Console.WriteLine("\nProcesando certificado digital [{0}]\n", Config.ArchivoCertificado);

            Cert = new Certificado(Config.ArchivoCertificado, Config.UsuarioCertificado, Config.ClaveCertificado, Config.PinCertificado);

            ds = new DirectoryStructure(JsonDir + "DirectoyStructure.js");
            DIR = ds.Structure;

            Console.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ": Validación de parámetros recibidos.");

            Bitacora = new EscribirMensaje();

            try
            {
                string Dir = DIR.Xmls + "\\";
                string Err = DIR.Errores_envio + "\\";

                string ErrorFile = Err + "ErrorMessage.txt";

                Console.WriteLine("Estructura de carpetas");
                Console.WriteLine("XMLs: {0}", Dir);
                Console.WriteLine("Errores {0}", Err);

                Console.WriteLine("\nProcesando datos del certificado digital");
                Certificado Cert = new Certificado(Config.ArchivoCertificado, Config.UsuarioCertificado, Config.ClaveCertificado, Config.PinCertificado);

                if (Accion.Equals("1")) // Enviar documento xml
                {
                    string respuesta = Enviar();

                    string RespHac = DIR.Xmls_firmados + "\\" + Facnume + "_resp.xml";      // xml con la respuesta de Hacienda

                    // Si ya existe el archivo con la respuesta de Hacienda y el código de respuesta es 4 (Aceptado) o 5 (Rechazado) no hace falta continuar
                    if (File.Exists(RespHac) && (respuesta == "4" || respuesta == "5"))
                    {
                        return;
                    }

                    // Si el proceso de envío no pudo obtener la respuesta de Hacienda, se ejecuta la consulta.
                    Accion = "2";
                    Thread.Sleep(3000); // Se establece un delay de 3 segundos para que Hacienda termine de procesar.

                    Console.WriteLine("\nEjecutando consulta post-envío...");
                    Consultar();
                }
                else if ((Accion.Equals("2")) || (Accion.Equals("4"))) // Consultar documento
                {
                    Consultar();
                }
                else if ((Accion.Equals("3"))) // Confirmar XMLs
                {
                    CedulaProveedor = args[1];
                    CedulaReceptor = args[3];
                    RecibirFactura();
                }
            }
            catch (Exception ex)
            {

                Console.WriteLine(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ": ErrorMessage " + ex);
            }

            Console.WriteLine("------------- Fin del proceso -------------");
            //Console.ReadKey();
        }

        private static bool DoTest()
        {
            /*
             * Cambié el conector. Ahora hay que probarlo en la flor porque localmente funciona bien.
             * Copiar los archivos de MsqlConnector a la flor y también FE.exe
             * Ejecutar FE.exe y ver resultados
             */
            // Obtener el la carpeta desde donde se hizo la llamada a este programa.
            CurrentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine(CurrentDirectory);
            //CompanyHome = "C:\\Java Programs\\osais\\laflor"; // Desarrollo local
            CompanyHome = "C:\\InfoT\\OSAIS\\System\\sai2"; // Producción La Flor

            // Obtener la configuración para la conexión a base de datos y los datos para conectar con Hacienda
            UserConfig = (CompanyHome.Length > 0 ? CompanyHome : CurrentDirectory) + "\\UserConfig.txt";
            Config = new Configuracion(UserConfig);
            if (!Config.ConnectedToDatabase)
            {
                Console.WriteLine("Could not connect to server -- " + Config.ErrorMessage);
            }

            if (Config.ConnectedToDatabase)
            {
                Console.WriteLine("Connected to server " + Config.Server);
            }

            EsPrueba = Config.EsPrueba;

            Console.WriteLine("\nTrabajando en ambiente de {0}\n", (EsPrueba == 1 ? "PRUEBAS" : "PRODUCCION"));
            Console.WriteLine("\nCreando conexión con la base de datos...");
            MySqlConnection conn = Config.GetConnection();
            if (conn == null)
            {
                Console.WriteLine("\nFalló la conexión a base de datos.");
            }
            try
            {
                conn.Open(); // acá se está cayendo
            }
            catch (Exception ex)
            {
                Console.WriteLine("\nNo se pudo abrir la conexión. " + ex);
            }

            Console.ReadKey();

            Console.WriteLine("\nConexión abierta satisfactoriamente.");

            string sqlSent =
                    "SELECT estado from faestadoDocElect " +
                    "Where Facnume = @FACNUME " +
                    "and Facnd = @FACND ";

            Console.WriteLine("\nEstableciendo parámetros SQL...");
            Console.ReadKey();
            var command = new MySqlCommand(sqlSent, conn);
            command.CommandTimeout = 60;
            command.Parameters.AddWithValue("FACNUME", 1252);
            command.Parameters.AddWithValue("FACND", 0);

            Console.WriteLine("\nEnviando consulta al servidor de base de datos...");
            Console.ReadKey();
            MySqlDataReader dr = command.ExecuteReader(); // Esta instrucción es la que está fallando

            if (!dr.HasRows)
            {
                return false;
            }

            Console.WriteLine("\nDeterminando estado del documento electrónico...");
            dr.Read();

            int Estado = dr.GetInt32("estado");
            Console.WriteLine("\nEl estado de esta factura es {0}", Estado);

            dr.Close();

            conn.Close();

            Console.WriteLine("\nProceso de consulta exitoso.");
            Console.ReadKey();

            return true;
        }

        private static void Consultar()
        {
            Console.WriteLine("\n" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Inicia proceso de consulta a Hacienda...");
            string Firmados = DIR.Xmls_firmados + "\\";
            string Logs = DIR.Logs + "\\";

            string TipoXml = "V";

            // Log con la respuesta de Hacienda.
            string LogRespuesaHacienda = Logs + Facnume + "_Hac" + ".log";

            Console.WriteLine("\n" + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + " Validando estado en base de datos...");
            // Consultar la base de datos para determinar si el xml ya fue aceptado o rechazado.
            if (AceptadoORechazado())
            {
                Console.WriteLine("Este documento fue consultado anteriormente y la respuesta de Hacienda no va a cambiar.");
                return;
            }

            ApiJano.Servicios Serv = new ApiJano.Servicios();
            Serv.AutenticacionValue = new ApiJano.Autenticacion();
            Serv.AutenticacionValue.Usuario = "olde";
            Serv.AutenticacionValue.Clave = "olde";

            XmlNode Resultado = Serv.ConsultarDocumento(ClaveDoc, EsPrueba, Config.UsuarioCertificado, Config.ClaveCertificado);
            string EstadoMovimiento = Resultado["EstadoMovimiento"].InnerText;

            // Tal parece que esta parte no trae toda la información como si lo hace el proceso de envío
            string MensajeHacienda = Resultado["MensajeRespuesta"].InnerText;

            /**
            * Los estados de Hacienda son los siguientes: 
            * 0=PRE-REGISTRO
            * 1=REGISTRADO
            * 2=RECIBIDO
            * 3=PROCESANDO
            * 4=ACEPTADO
            * 5=RECHAZADO
            * 6=ERROR
            * 10=DESCONOCIDO
            */
            string CodigoRespuesta = "10"; // Vamos a usar este código para DESCONOCIDO
            if (EstadoMovimiento == "PRE-REGISTRO")
            {
                CodigoRespuesta = "0";
            }
            else if (EstadoMovimiento == "REGISTRADO")
            {
                CodigoRespuesta = "1";
            }
            else if (EstadoMovimiento == "RECIBIDO")
            {
                CodigoRespuesta = "2";
            }
            else if (EstadoMovimiento == "PROCESANDO")
            {
                CodigoRespuesta = "3";
            }
            else if (EstadoMovimiento == "ACEPTADO")
            {
                CodigoRespuesta = "4";
            }
            else if (EstadoMovimiento == "RECHAZADO")
            {
                CodigoRespuesta = "5";
            }
            else if (EstadoMovimiento == "ERROR")
            {
                CodigoRespuesta = "6";
            }

            if (CodigoRespuesta == "10")
            {
                EstadoMovimiento = "DESCONOCIDO";
            }

            Console.WriteLine(
                    DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") +
                    ": El estado reportado por Hacienda es: " + EstadoMovimiento + " " + MensajeHacienda);

            // Guardar el estado de la consulta
            // Para los proveedores el nombre termina en P
            if (Accion == "4")
            { 
                LogRespuesaHacienda = Logs + Facnume + "_HacP" + ".log";
            } // end if

            // Si es una factura de compra se debe diferenciar el log
            if (TipoDoc == "FCO")
            {
                LogRespuesaHacienda = Logs + Facnume + "_HacCompras" + ".log";
                TipoXml = "C";
            }

            Console.WriteLine("Log generado: " + LogRespuesaHacienda);

            Bitacora.SaveMessage(
                "Estado: " + EstadoMovimiento + " \n" +
                "Respuesta_ext: " + MensajeHacienda + " \n" +
                "claveHacienda: " + ClaveDoc + " \n" +
                "Codigo respuesta: " + CodigoRespuesta, LogRespuesaHacienda);

            Console.WriteLine(
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ": Guardando xml firmado y devuelto por Hacienda.");

            // Guardar los xml devueltos por Hacienda.
            string xmlBase64Respuesta = Resultado["XMLRespuestaHacienda"].InnerText; //Aquí viene el xml que devuelve Hacienda
            string RutaArchivo = Firmados + Facnume + "_resp.xml";
            XmlDocument xmlDecodificado;

            if (string.IsNullOrEmpty(xmlBase64Respuesta))
            {
                Console.WriteLine(
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") + ": Hacienda aún no ha emitido respuesta firmada.");
            }
            else
            {
                xmlDecodificado = Base64ToXML(xmlBase64Respuesta);
                xmlDecodificado.Save(RutaArchivo);
            } // end if-else

            // Guardar en base de datos el resultado de la consulta.
            // En la descripción del estado concatenar explica_estado + respuesta_ext
            // porque respuesta_ext tiene la explicación cuando se rechaza un documento.
            // Crear la conexión con la base de datos

            var conn = Config.GetConnection();
            conn.Open();
            string sqlSent =
                    "UPDATE faestadoDocElect " +
                    "   SET estado = @ESTADO, descrip = @DESCRIP, fecha = Now(), referencia = @REF, xmlFirmado =  @XMLF " +
                    "Where Facnume = @FACNUME " +
                    "and Facnd = @FACND " +
                    "and tipoxml = @TIPOXML";

            var command = new MySqlCommand(sqlSent, conn);
            command.CommandTimeout = 60;
            command.Parameters.AddWithValue("ESTADO", CodigoRespuesta);
            command.Parameters.AddWithValue("DESCRIP", EstadoMovimiento + ":\n " + MensajeHacienda + " \nclaveHacienda: " + ClaveDoc);
            command.Parameters.AddWithValue("REF", 0);
            command.Parameters.AddWithValue("FACNUME", Facnume);
            command.Parameters.AddWithValue("FACND", Facnd);
            command.Parameters.AddWithValue("TIPOXML", TipoXml);
            command.Parameters.AddWithValue("XMLF", Facnume + ".xml"); // El xml interno y el firmado se llaman igual pero quedan en distintas carpetas.
            Int32 records = command.ExecuteNonQuery();
            

            conn.Close();

            Console.WriteLine("\nBase de datos actualizada exitosamente. ({0} registros)", records);
        } // end Consultar


        /**
         * Determina si el documento tiene estado de ACEPTADO o RECHAZADO
         */
        private static bool AceptadoORechazado()
        {
            string TipoXml = "V";
            if (TipoDoc == "FCO")
            {
                TipoXml = "C";
            }

            Console.WriteLine("\nCreando conexión con la base de datos...");
            MySqlConnection conn = Config.GetConnection();
            if (conn == null)
            {
                Console.WriteLine("\nFalló la conexión a base de datos.");
            }
            try
            {
                conn.Open();
            } catch(Exception ex)
            {
                Console.WriteLine("\nNo se pudo abrir la conexión. " + ex);
            }
            
            Console.WriteLine("\nConexión abierta satisfactoriamente.");

            string sqlSent =
                    "SELECT estado from faestadoDocElect " +
                    "Where Facnume = @FACNUME " +
                    "and Facnd = @FACND " +
                    "and tipoxml = @TIPOXML";

            Console.WriteLine("\nEstableciendo parámetros SQL...");
            var command = new MySqlCommand(sqlSent, conn);
            command.CommandTimeout = 60;
            command.Parameters.AddWithValue("FACNUME", Facnume);
            command.Parameters.AddWithValue("FACND", Facnd);
            command.Parameters.AddWithValue("TIPOXML", TipoXml);

            Console.WriteLine("\nEnviando consulta al servidor de base de datos...");
            MySqlDataReader dr = command.ExecuteReader(); // Esta instrucción es la que está fallando

            if (!dr.HasRows)
            {
                return false;
            }

            Console.WriteLine("\nDeterminando estado del documento electrónico...");
            dr.Read();

            int Estado = dr.GetInt32("estado");
            bool AcepRecha = (Estado == 4 || Estado == 5);
            dr.Close();

            conn.Close();

            Console.WriteLine("\nProceso de consulta exitoso.");

            return AcepRecha;
        }


        // Se puede enviar todas las veces que sea necesario.
        private static string Enviar()
        {
            DateTime Hoy = DateTime.Now;
            Console.WriteLine("\nInicia proceso de envío: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            string Err = DIR.Errores_envio + "\\";

            string ErrorFile = Err + "ErrorMessage.txt";
            string CodigoRespuesta = "";

            EscribirMensaje Bitacora = new EscribirMensaje();

            Console.WriteLine("\nConectando con el servicio...");

            ApiJano.Servicios Serv = new ApiJano.Servicios();
            Serv.AutenticacionValue = new ApiJano.Autenticacion();
            Serv.AutenticacionValue.Usuario = "olde";
            Serv.AutenticacionValue.Clave = "olde";

            try
            {
                ErrorFile = Err + Facnume + "_Err" + ".txt";
                string RespHac = DIR.Xmls_firmados + "\\" + Facnume + "_resp.xml";      // xml con la respuesta de Hacienda
                string TxtFile = DIR.Xmls + "\\" + Facnume + ".txt";                    // Guarda solo el estado del envío

                Console.WriteLine("Procesando documento: {0}", Facnume);

                // Inicia proceso de envío del xml
                string txtFile = XmlFile.Substring(0, XmlFile.IndexOf(".")) + ".txt";
                string xmlText = System.IO.File.ReadAllText(DIR.Xmls + "\\" + XmlFile);
                XmlDocument DocumentoXML = new XmlDocument();
                DocumentoXML.LoadXml(xmlText);

                Console.WriteLine("\n" + Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Enviando archivo [" + XmlFile + "]");

                XmlNode Resultado = Serv.EnviarDocumento(DocumentoXML, EsPrueba, Cert.GetCertificadoBase64(), Config.PinCertificado, Config.UsuarioCertificado, Config.ClaveCertificado);

                XmlDocument Xmlbase64;
                if (!string.IsNullOrEmpty(Resultado["XMLFirmado"].InnerText))
                {
                    Xmlbase64 = DecodeBase64ToXML(Resultado["XMLFirmado"].InnerText);
                }

                string XmlSignedFile = DIR.Xmls_firmados + "\\" + Facnume + ".xml";     // xml firmado
                Console.WriteLine("Guardando el XML firmado [{0}]", XmlSignedFile);
                System.IO.File.WriteAllText(XmlSignedFile, Base64.Decode(Resultado["XMLFirmado"].InnerText), Encoding.UTF8);

                string EstadoMovimiento = Resultado["EstadoMovimiento"].InnerText;
                string JsonEnviado = Resultado["JsonEnviado"].InnerText;

                XmlDocument XMLRespuestaHacienda;

                if (!string.IsNullOrEmpty(Resultado["XMLRespuestaHacienda"].InnerText))
                {
                    XMLRespuestaHacienda = DecodeBase64ToXML(Resultado["XMLRespuestaHacienda"].InnerText);
                    Console.WriteLine("Guardando respuesta de Hacienda [{0}]", RespHac);
                    System.IO.File.WriteAllText(RespHac, Base64.Decode(Resultado["XMLRespuestaHacienda"].InnerText), Encoding.UTF8);
                }

                string MensajeRespuesta = Resultado["JsonEnviado"].InnerText;
                string MensajeRespuestaHacienda = Resultado["MensajeRespuesta"].InnerText;

                Console.WriteLine("Estado movimiento: {0} ", EstadoMovimiento);
                Console.WriteLine("Mensaje respuesta de Hacienda: {0} ", MensajeRespuestaHacienda);

                /**
                * Los estados de Hacienda son los siguientes: 
                * 0=PRE-REGISTRO
                * 1=REGISTRADO
                * 2=RECIBIDO
                * 3=PROCESANDO
                * 4=ACEPTADO
                * 5=RECHAZADO
                * 6=ERROR
                * 10=DESCONOCIDO
                */
                CodigoRespuesta = "10"; // Vamos a usar este código para DESCONOCIDO

                if (EstadoMovimiento == "PRE-REGISTRO")
                {
                    CodigoRespuesta = "0";
                }
                else if (EstadoMovimiento == "REGISTRADO")
                {
                    CodigoRespuesta = "1";
                }
                else if (EstadoMovimiento == "RECIBIDO")
                {
                    CodigoRespuesta = "2";
                }
                else if (EstadoMovimiento == "PROCESANDO")
                {
                    CodigoRespuesta = "3";
                }
                else if (EstadoMovimiento == "ACEPTADO")
                {
                    CodigoRespuesta = "4";
                }
                else if (EstadoMovimiento == "RECHAZADO")
                {
                    CodigoRespuesta = "5";
                }
                else if (EstadoMovimiento == "ERROR")
                {
                    CodigoRespuesta = "6";
                }

                if (CodigoRespuesta == "10")
                {
                    EstadoMovimiento = "DESCONOCIDO";
                }

                string referencia = "0"; // Esta versión no maneja referencia
                Console.WriteLine("\nGuardando estado del envío {0} ", TxtFile);

                Bitacora.SaveMessage(
                        Hoy.ToString("dd/MM/yyyy hh:mm:ss") + "\n" +
                        "Documento: " + Facnume + "\n" +
                        "Tipo:      " + TipoDoc + "\n" +
                        "Identificador: " + Facnd + "\n" +
                        "Estado del envío: " + CodigoRespuesta + " " + "\n" + EstadoMovimiento + "\n" +
                        "Referencia: " + referencia + "\n", TxtFile);

                Console.WriteLine("\nCreando conexión con la base de datos...");
                var conn = Config.GetConnection();
                Console.WriteLine("\nAbriendo conexión...");
                conn.Open();
                string sqlSent =
                        "UPDATE faestadoDocElect " +
                        "   SET estado = @ESTADO, descrip = @DESCRIP, fecha = Now(), referencia = @REF , xmlFirmado =  @XMLF " +
                        "Where Facnume = @FACNUME " +
                        "and Facnd = @FACND " +
                        "and tipoxml = @TIPOXML";

                Console.WriteLine("\nSeteando paraámetros SQL...");
                var command = new MySqlCommand(sqlSent, conn);
                command.CommandTimeout = 60;
                command.Parameters.AddWithValue("ESTADO", CodigoRespuesta);
                command.Parameters.AddWithValue("DESCRIP", EstadoMovimiento + ":\n " + MensajeRespuestaHacienda);
                command.Parameters.AddWithValue("REF", 0);
                command.Parameters.AddWithValue("FACNUME", Facnume);
                command.Parameters.AddWithValue("FACND", Facnd);
                command.Parameters.AddWithValue("TIPOXML", "V");
                command.Parameters.AddWithValue("XMLF", Facnume + ".xml"); // El xml interno y el firmado se llaman igual pero quedan en distintas carpetas.
                Console.WriteLine("\nEnviando petición al servidor SQL...");
                Int32 records = command.ExecuteNonQuery();

                conn.Close();
                Console.WriteLine("\nBase de datos actualizada exitosamente. ({0} registros)", records);
            }
            catch (Exception ex)
            {
                Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Ocurrió un error [" + ex.Message + "]");
                Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy") + " --> " + ex.Message, ErrorFile);
            }

            return CodigoRespuesta;
        } // end Enviar

        private static XmlDocument DecodeBase64ToXML(string valor)
        {
            string xml = Encoding.UTF8.GetString(Convert.FromBase64String(valor));
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            return xmlDocument;
        }

        private static XmlDocument Base64ToXML(string documento)
        {
            XmlDocument resultante = new XmlDocument();
            Byte[] myBase64ret = Convert.FromBase64String(documento);
            string myStr = System.Text.Encoding.UTF8.GetString(myBase64ret);
            resultante.LoadXml(myStr);

            return resultante;
        } // end Base64ToXML


        
        private static void RecibirFactura()
        {

            DateTime Hoy = DateTime.Now;
            Console.WriteLine("Inicia proceso de envío de respuesta: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            string xmlText = System.IO.File.ReadAllText(DIR.Xmls_proveedores + "\\" + XmlFile);

            XmlDocument DocumentoXML = new XmlDocument();
            DocumentoXML.LoadXml(xmlText);

            Console.WriteLine("Autenticando con servidor remoto: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            ApiJano.Servicios Serv = new ApiJano.Servicios();
            Serv.AutenticacionValue = new ApiJano.Autenticacion();
            Serv.AutenticacionValue.Usuario = "olde";
            Serv.AutenticacionValue.Clave = "olde";

            Console.WriteLine("Enviando respuesta: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
            string certif = Cert.GetCertificadoBase64();
            XmlNode Resultado = Serv.EnviarMensajeReceptor(
                DocumentoXML,
                ClaveDoc,
                ConsecutivoProveedor,
                CedulaProveedor,        // Cédula del proveedor
                TipoCedulaProveedor,    // Tipo de cédula del proveedor
                CedulaReceptor,         // Nuestra cédula jurídica
                TipoCedulaReceptor,     // Nuestro tipo de cédula
                Cert.GetCertificadoBase64(),
                Config.PinCertificado,
                Config.UsuarioCertificado,
                Config.ClaveCertificado,
                EsPrueba);

            Console.WriteLine("Generando bitácora: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            string XMLResultado = Resultado["estado"].InnerText;
            string XMLDescripcion = Resultado["estado_descripcion"].InnerText;
            string XMLIDReferencia = Resultado["id_referencia"].InnerText;
            string XMLRespuesta = Resultado["respuesta"].InnerText;
            DateTime XMLFechaI = DateTime.Parse(Resultado["fecha_proceso"].InnerText);
            string XMLFecha = XMLFechaI.ToString("yyyy-MM-dd HH:mm:ss");

            string logFile = DIR.Xmls_proveedores + "\\" + XMLIDReferencia + ".log";
            Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + "\n"
                + "Estado: " + XMLResultado + " " + XMLDescripcion + "\n"
                + "Referencia: " + XMLIDReferencia + "\n"
                + "Respuesta: " + XMLRespuesta, logFile);

            Console.WriteLine("Finaliza proceso de envío de respuesta: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
            
        } // end RecibirFactura
    } // end class
}

