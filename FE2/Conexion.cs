//using MySql.Data.MySqlClient;
using MySqlConnector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE2
{
    class Conexion
    {
        private string user;
        private string password;
        private string dataBase;
        private string connectionString;

        public Conexion(string user, string password, string dataBase, string server, int port)
        {
            this.user = user;
            this.password = password;
            this.dataBase = dataBase;
            //this.connectionString = "datasource=127.0.0.1;port=3307;username=" + user + ";password=" + password + ";database=" + dataBase + ";";
            this.connectionString = "datasource=" + server + ";" + "port=" + port + ";" + "username=" + user + ";password=" + password + ";database=" + dataBase + ";";
        } // end constructor


        // Devuelve un objeto nuevo de conexión cada vez que se invoca
        public MySqlConnection GetConnection()
        {
            //MySqlConnector conn = new MySqlConnector(connectionString);

            return new MySqlConnection(connectionString);
        }
    } // end class
} // end namespace
