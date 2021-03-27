using MySql.Data.MySqlClient;
using System;

namespace EnviarFactura
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
        public bool Connected { get; set; }
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
            Connected = false;
            try
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(UserConfig);
                while ((textLine = sr.ReadLine()) != null)
                {
                    switch (line)
                    {
                        case 1: User = textLine; break;     // Usuario
                        case 2: Passw = textLine; break;    // Clave
                        case 3: Server = textLine; break;   // Server
                        case 4: Port = Int16.Parse(textLine); break; // Puerto
                        case 5: DataBase = textLine; break; // Base de datos
                        default: break;
                    }
                    line++;
                } // end while

                sr.Close();
                Conexion = new Conexion(User, Passw, DataBase, Server, Port);
                Connected = true;
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
