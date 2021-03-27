using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace FE2
{
    class EscribirMensaje
    {
        
        public void SaveMessage(string mensaje, string archivo)
        {
            try
            {
                StreamWriter sw = new StreamWriter(archivo);
                sw.WriteLine(mensaje);
                sw.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
                Console.ReadKey();
            }
        }
    }
}
