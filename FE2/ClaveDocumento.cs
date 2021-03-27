using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE2
{
    class ClaveDocumento
    {
        private readonly string Clave;
        public string Cedula { get; set; }
        public string Consecutivo { get; set; }

        public ClaveDocumento(string clave)
        {
            Clave = clave ?? throw new ArgumentNullException(nameof(clave));
            SplitValues();
        }

        private void SplitValues()
        {
            Cedula = Clave.Substring(9, 12);
            Consecutivo = Clave.Substring(21, 20);
        }
    } // end class
}
