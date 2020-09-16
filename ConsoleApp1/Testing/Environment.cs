using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EnviarFactura.Testing
{
    class Environment
    {
        public static Int16 PRUEBAS = 1;
        public static Int16 PRODUCCION = 2;

        private Int16 ambiente;
        private string usuario;
        private string clave;

        public Environment(short ambiente)
        {
            this.ambiente = ambiente;
            LoadConfiguration();
        }

        public Int16 GetAmbiente() => this.ambiente;

        public void SetAmbiente(Int16 ambiente) => this.ambiente = ambiente;

        public string GetUsuario() => this.usuario;

        public void SetUsuario(string usuario) => this.usuario = usuario;

        public string GetClave() => this.clave;

        public void SetClave(string clave) => this.clave = clave;


        private void LoadConfiguration()
        {
            string ConfigFile = "Environment.txt";

            // Read each line of the file into a string array. Each element
            // of the array is one line of the file.
            string[] lines = System.IO.File.ReadAllLines(@ConfigFile);

            // Find user and password by using a foreach loop.
            foreach (string line in lines)
            {
                if (line.Contains("usuario:"))
                {
                    string[] record = line.Split(':');
                    this.usuario = record[1].Trim();
                    continue;
                } // end if

                if (line.Contains("clave:"))
                {
                    string[] record = line.Split(':');
                    this.clave = record[1].Trim();
                    if (this.ambiente == PRUEBAS)
                    {
                        break;
                    }
                } // end if
            } // end for
        } // end LoadConfiguration
    } // end class
} // end namespace
