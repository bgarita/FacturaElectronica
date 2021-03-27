using EnviarFactura;
using EnviarFactura.Testing;
using FE.Testing;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE
{
    class Fe
    {
        private static string CurrentDirectory; // Carpeta desde donde se llama a este programa
        private static string JsonDir;          // Carpeta que contiene el Json con la estructura completa de carpetas del sistema.
        private static DirectoryStructure ds;   // Clase que carga y devuelve una instancia de la estructura de carpetas del sistema.
        private static Dir DIR;                 // Clase que contiene toda la estructura que viene en el Json
        private static EscribirMensaje Bitacora;// Clase que escribe en una bitácora
        private static string UserConfig;       // Archivo (ruta completa) donde se guardan los datos de conexión a la base de datos.
        private static DocumentoElectronico DocumEl;
        private static Int32 Facnume;
        private static Int32 Facnd;
        private static string TipoDoc;          // 01=Factura, 02=NotaDebito, 03=NotaCredito, 04=Tiquete, 08=FacturaCompraSimplificada, 09=FacturaExportacion
        private static string Accion;           // 1=Enviar archivo, 2=Consultar estado, 3=Recibir xml, 4=Consultar estado proveedores


        static void Main(string[] args)
        {
            // Obtener el la carpeta desde donde se hizo la llamada a este programa.
            CurrentDirectory = Directory.GetCurrentDirectory();
            Console.WriteLine("Invoked from {0}", CurrentDirectory);

            if (args == null || args.Length == 0)
            {
                args = new string[5];
                args[0] = "10006315"; // Número de factura a procesar
                args[1] = "0";        // Facnd
                args[2] = "C:\\Java Programs\\osais\\laflor";         // Carpeta home de la compañía
                args[3] = "01";       // 01=Factura, 02=NotaDebito, 03=NotaCredito, 04=Tiquete, 08=FacturaCompraSimplificada, 09=FacturaExportacion
                args[4] = "1";        // 1=Enviar archivo, 2=Consultar estado, 3=Recibir xml, 4=Consultar estado proveedores
            }
            
            // Construir la ruta para el Json que contiene la estructura completa de la compañía actual.
            JsonDir = (args[2].Length > 0 ? args[2] : CurrentDirectory) + "\\";

            // Obtener la configuración para la conexión a base de datos y los datos para conectar con Hacienda
            UserConfig = (args[2].Length > 0 ? args[2] : CurrentDirectory) + "\\UserConfig.txt";

            Console.WriteLine("\nJson file location: {0}", JsonDir);
            Console.WriteLine("UserConfig file: {0}", UserConfig);

            ds = new DirectoryStructure(JsonDir + "DirectoyStructure.js");
            DIR = ds.Structure;
            Console.WriteLine("Company dir: {0}", DIR.Company_home);

            Console.WriteLine("\nEstableciendo conexión con la base de datos...");

            // Esta conexión solo se hace para validar que el ejecutable tenga acceso a la base de datos
            // antes de intentar hacer el envío a Hacienda.  Esto se hace para evitar que se haga un 
            // envío y no quede registrado en la base de datos.
            Configuracion Config = new Configuracion(UserConfig);
            if (!Config.Connected)
            {
                Console.WriteLine("Could not connected to server -- " + Config.ErrorMessage);
                return;
            }

            if (Config.Connected)
            {
                Console.WriteLine("Connected to server " + Config.Server);
            }

            DateTime Hoy = DateTime.Now;
            Console.WriteLine("\nInicia proceso: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            Console.WriteLine("\n" + Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Parámetros recibidos.");

            for(int i = 0; i < args.Length; i++)
            {
                Console.WriteLine("Paramter {0} = {1}", i, args[i]);
            }

            Bitacora = new EscribirMensaje();

            try
            {
                Facnume = Int32.Parse(args[0]);
                Facnd = Int32.Parse(args[1]);
                TipoDoc = args[3];
                Accion = args[4];

                string Dir = DIR.Xmls + "\\";
                string Err = DIR.Errores_envio + "\\";

                string ErrorFile;

                ErrorFile = Err + "ErrorMessage.txt";

                Console.WriteLine("\nEstructura de carpetas");
                Console.WriteLine("Xmls: {0}", Dir);
                Console.WriteLine("Errores: {0}", Err);
                Console.WriteLine("Xmls firmados: {0} ", DIR.Xmls_firmados + "\\");

                if (args == null || args.Length == 0)
                {
                    Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": No se recibió el número de factura del sistema local.");
                    Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy") + " --> No se recibió el número de factura del sistema local.", ErrorFile);
                    return;
                }

                Console.WriteLine("\nProcesando datos del certificado digital");

                Certificado cert = new Certificado(Config.ArchivoCertificado, Config.UsuarioCertificado, Config.ClaveCertificado, Config.PinCertificado);

                DocumEl = new DocumentoElectronico(Config, Facnume, Facnd, Accion, TipoDoc, cert, DIR);
                DocumEl.EnviarFactura();


                // Cerrar la conexión a base de datos
                Config.GetConnection().Close();
            }
            catch (Exception ex)
            {

                Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": ErrorMessage " + ex);
            }

        }
    }
}
