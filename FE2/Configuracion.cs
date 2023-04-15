using Google.Protobuf.WellKnownTypes;
using MySqlConnector;
//using MySql.Data.MySqlClient;
using System;

namespace FE2
{
    class Configuracion
    {
        // Variables para la conexión con la base de datos
        private Conexion Conexion;
        public string User { get; set; }
        public string Passw { get; set; }
        public string Server { get; set; }
        public int Port { get; set; }
        public string DataBase { get; set; }
        public bool ConnectedToDatabase { get; set; }

        public string UsuarioCertificado { get; set; }
        public string ClaveCertificado { get; set; }
        public string PinCertificado { get; set; }
        public string ArchivoCertificado { get; set; }

        public Int32 EsPrueba { get; set; }     // Se refiere al ambiente de Hacienda. 1=Prueba, 0=Producción

        public string ErrorMessage { get; set; }
        private readonly string UserConfig;   // Archivo (ruta completa) donde se guardan los datos de conexión a la base de datos.

        public Configuracion(string UserConfig)
        {
            this.UserConfig = UserConfig;
            GetConfiguration();
        }


        private void GetConfiguration()
        {
            // string file = "UserConfig.txt";
            string textLine;
            int line = 1;
            ConnectedToDatabase = false;
            EsPrueba = 1;   // Se asume el ambiente el de pruebas como el default.  De esa forma si no existe la línea 10 se garantiza el ambiente de pruebas.
            try
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(UserConfig);
                while ((textLine = sr.ReadLine()) != null)
                {
                    switch (line)
                    {
                        case 1: User = textLine; break;                 // Usuario
                        case 2: Passw = textLine; break;                // Clave
                        case 3: Server = textLine; break;               // Server
                        //case 4: Port = Int16.Parse(textLine); break;    // Puerto
                        case 4: Port = Convert.ToInt32(textLine); break;  // Puerto
                        case 5: DataBase = textLine; break;             // Base de datos
                        case 6: UsuarioCertificado = textLine; break;   // Usuario del certificado
                        case 7: ClaveCertificado = textLine; break;     // Clave del certificado
                        case 8: PinCertificado = textLine; break;       // Pin del certificado
                        case 9: ArchivoCertificado = textLine; break;   // Archivo que contiene el certificado
                        //case 10: EsPrueba = Int16.Parse(textLine); break;            // Ambiente de pruebas o producción
                        case 10: EsPrueba = Convert.ToInt32(textLine); break;            // Ambiente de pruebas o producción
                        default: break;
                    }
                    line++;
                } // end while

                sr.Close();
                Conexion = new Conexion(User, Passw, DataBase, Server, Port);
                ConnectedToDatabase = true;
            }
            catch (Exception ex)
            {
                ErrorMessage = "<Configuracion.GetConfiguration()> " + ex.Message;
                //Console.WriteLine(ErrorMessage);
            }

        } // end GetConfiguration


        // Retorna un objeto de conexión listo para usar
        public MySqlConnection GetConnection()
        {
            return Conexion.GetConnection();
        } // end getConnection
    } // end Class
} // end namespace
