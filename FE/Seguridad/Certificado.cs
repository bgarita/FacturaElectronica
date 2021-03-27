using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace FE.Testing
{
    class Certificado
    {
        public string Archivo { get; set; }
        public string Usuario { get; set; }
        public string Clave { get; set; }
        public string Pin { get; set; }

        private string Certificado64;

        public Certificado(string archivo, string usuario, string clave, string pin)
        {
            Archivo = archivo ?? throw new ArgumentNullException(nameof(archivo));
            Usuario = usuario ?? throw new ArgumentNullException(nameof(usuario));
            Clave = clave ?? throw new ArgumentNullException(nameof(clave));
            Pin = pin ?? throw new ArgumentNullException(nameof(pin));

            Console.WriteLine("Certificado digital: {0}", Archivo);
        }

        public string GetCertificadoBase64()
        {
            X509Certificate2 cert = new X509Certificate2(Archivo, Pin, X509KeyStorageFlags.Exportable);
            DateTime fechavencimiento = cert.NotAfter;
            if (fechavencimiento < DateTime.Now)
            {
                Console.WriteLine("[ERROR] El certificado está vencido.");
                return "";
            }

            byte[] certData = cert.Export(X509ContentType.Pkcs12, Pin);
            this.Certificado64 = Convert.ToBase64String(certData);

            return this.Certificado64;
        }
    } // end class
}
