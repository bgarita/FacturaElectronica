using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviarFactura
{
    class Configuracion
    {
        // Variables para la conexión con la base de datos
        private Conexion conexion;
        private string user;
        private string passw;
        private string Server;
        private int port;
        private string DataBase;

        public Configuracion()
        {
            GetConfiguration();
        }


        private void GetConfiguration()
        {
            string file = "UserConfig.txt";
            string textLine;
            int line = 1;
            try
            {
                System.IO.StreamReader sr = new System.IO.StreamReader(file);
                while ((textLine = sr.ReadLine()) != null)
                {
                    switch (line)
                    {
                        case 1: user = textLine; break;     // Usuario
                        case 2: passw = textLine; break;    // Clave
                        case 3: Server = textLine; break;   // Server
                        case 4: port = Int16.Parse(textLine); break; // Puerto
                        case 5: DataBase = textLine; break; // Base de datos
                        default: break;
                    }
                    line++;
                } // end while

                sr.Close();
                conexion = new Conexion(user, passw, DataBase, Server, port);
            }
            catch (Exception ex)
            {
                Console.WriteLine("<Configuracion.GetConfiguration()> " + ex.Message);
            }

        } // end GetConfiguration


        // Retorna un objeto de conexión listo para usar
        public MySqlConnection GetConnection()
        {
            return conexion.GetConnection();
        } // end getConnection
    } // end Class
} // end namespace
