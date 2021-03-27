using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE
{
    class DetalleDocumento
    {
        // Campos para el xml (Detalle)
        public Int32 Linea { get; set; }
        public decimal Faccant { get; set; }
        public string Artcode { get; set; }
        public string Cabys { get; set; }
        public string Artdesc { get; set; }
        public Int32 TipoArticulo { get; set; }
        public string Unidad { get; set; }
        public decimal Artprec { get; set; }
        public decimal PorcIVA { get; set; }
        public decimal PorcICO { get; set; }    // Porcentaje del impuesto de consumo
        public decimal PorcDES { get; set; }
        public decimal TotalLinea { get; set; }
        public bool Exonerado { get; set; }
        public string TipoExoneracion { get; set; }
        public string NumerodeExoneracion { get; set; }
        public string InstitucionExoneracion { get; set; }
        public DateTime FechaExoneracion { get; set; }
        public Int32 BaseExoneracion { get; set; } //Base de Exoneración del 1 al 100
    } // end class
}
