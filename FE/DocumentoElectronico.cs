using EnviarFactura;
using EnviarFactura.Testing;
using FE.Seguridad;
using FE.Testing;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace FE
{
    class DocumentoElectronico
    {
        private readonly string Accion;   // 1=Enviar archivo, 2=Consultar estado, 3=Recibir xml, 4=Consultar estado proveedores
        private readonly string TipoDoc;
        private readonly Configuracion Config;
        private readonly Documento Doc;
        private readonly Int32 Facnume;
        private readonly Int32 Facnd;
        private readonly bool Produccion = false; // Esto debe parametrizarse para tome el dato del archivo de configuración
        private readonly Certificado Cert;
        private readonly Dir DIR;


        public DocumentoElectronico(Configuracion config, Int32 facnume, Int32 facnd, string accion, string tipoDoc, Certificado cert, Dir dir)
        {
            this.Config = config;
            this.Facnume = facnume;
            this.Facnd = facnd;
            this.Accion = accion;
            this.TipoDoc = tipoDoc ?? throw new ArgumentNullException(nameof(tipoDoc));
            this.Cert = cert;
            this.Doc = new Documento(Config, Facnume, Facnd, cert);
            this.DIR = dir;
        }

        /*
         * IMPORTANTE: (Pendiente resolver)
         * Hay que analizar si es necesario el parámetro Accion porque la clase Fe.cs es la que va a utilizar
         * los métodos de esta clase y por lo tanto ahí mismo puede decidir la acción.
         * También es posible que el método EnviarFactura se cambie a EnviarDocumento ya que es este mismo
         * método el que envía facturas, tiquetes, notas de crédito, etc.
         */
        public void EnviarFactura()
        {
            // El archivo donde se guarda el error del envío tiene el mismo nombre de la factura interna + _Err.txt
            string ErrorFile = DIR.Errores_envio + "\\" + Facnume + "_Err.txt";
            string XmlFile = DIR.Xmls + "\\" + Facnume + ".xml";                    // xml interno, no debe enviarse al cliente porque lleva datos sensibles
            string RespHac = DIR.Xmls_firmados + "\\" + Facnume + "_resp.xml";      // xml con la respuesta de Hacienda
            string TxtFile = DIR.Xmls + "\\" + Facnume + ".txt";                    // Guarda solo el estado del envío
            string XmlSignedFile = DIR.Xmls_firmados + "\\" + Facnume + ".xml";     // xml firmado
            EscribirMensaje Bitacora = new EscribirMensaje();

            try
            {
                ApiJano.Servicios Serv = new ApiJano.Servicios();
                Serv.AutenticacionValue = new ApiJano.Autenticacion();
                Serv.AutenticacionValue.Usuario = "olde";
                Serv.AutenticacionValue.Clave = "olde";

                Console.WriteLine("\nGenerando el XML...");

                MemoryStream memoryStream = new MemoryStream();
                XmlTextWriter xmlTextWriter = new XmlTextWriter((Stream)memoryStream, Encoding.UTF8);
                XmlDocument xmlDocument = new XmlDocument();

                xmlTextWriter.WriteStartDocument();
                xmlTextWriter.WriteStartElement("DocumentoElectronico");
                xmlTextWriter.WriteAttributeString("xmlns:xsd", "http://www.w3.org/2001/XMLSchema");
                xmlTextWriter.WriteAttributeString("xmlns:xsi", "http://www.w3.org/2001/XMLSchema-instance");

                // Los datos se toman del objeto Doc
                xmlTextWriter.WriteElementString("EsProduccion", Produccion.ToString());
                xmlTextWriter.WriteElementString("Clave", Doc.Clave);
                xmlTextWriter.WriteElementString("CodigoActividad", Doc.CodigoActividad);
                xmlTextWriter.WriteElementString("NombreEmisor", Doc.NombreEmisor);
                xmlTextWriter.WriteElementString("TipoIdentificacionEmisor", Doc.TipoCedulaEmisor);
                xmlTextWriter.WriteElementString("Numero", Doc.CedulaEmisor);
                xmlTextWriter.WriteElementString("ProvinciaEmisor", Doc.ProvinciaEmisor);
                xmlTextWriter.WriteElementString("CantonEmisor", Doc.CantonEmisor);
                xmlTextWriter.WriteElementString("DistritoEmisor", Doc.DistritoEmisor);
                xmlTextWriter.WriteElementString("BarrioEmisor", Doc.BarrioEmisor);
                xmlTextWriter.WriteElementString("OtraSenasEmisor", Doc.OtrasSeñas);
                xmlTextWriter.WriteElementString("TelefonoEmisor", Doc.Telefono);
                xmlTextWriter.WriteElementString("FaxEmisor", Doc.Fax);
                xmlTextWriter.WriteElementString("CorreoEmisor", Doc.CorreoEmisor);
                xmlTextWriter.WriteElementString("PinHaciendaEmisor", Base64.Encode(Doc.PinHacienda));
                //xmlTextWriter.WriteElementString("PinHaciendaEmisor", Base64.Encode("2019"));

                xmlTextWriter.WriteElementString("CertificadoHaciendaEmisor", Doc.CertificadoHacienda); // Viene en Base 64
                //xmlTextWriter.WriteElementString("CertificadoHaciendaEmisor", "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"); // Viene en Base 64

                xmlTextWriter.WriteElementString("UsuarioHaciendaEmisor", Base64.Encode(Doc.UsuarioHacienda));
                //xmlTextWriter.WriteElementString("UsuarioHaciendaEmisor", Base64.Encode("cpf-01-1132-0693@stag.comprobanteselectronicos.go.cr"));

                xmlTextWriter.WriteElementString("ClaveHaciendaEmisor", Base64.Encode(Doc.ClaveHacienda));
                //xmlTextWriter.WriteElementString("ClaveHaciendaEmisor", Base64.Encode("(p/@9_!+_ad26}h;Clh)"));

                xmlTextWriter.WriteElementString("EsReceptor", Doc.ClienteEsReceptor.ToString());
                xmlTextWriter.WriteElementString("EsExtranjero", Doc.ClienteEsExtranjero.ToString());
                xmlTextWriter.WriteElementString("CedulaExtranjero", Doc.CedulaExtranjero);
                xmlTextWriter.WriteElementString("NombreReceptor", Doc.NombreReceptor);
                xmlTextWriter.WriteElementString("TipoIdentificacionReceptor", Doc.TipoCedulaReceptor);
                xmlTextWriter.WriteElementString("IdentificacionReceptor", Doc.CedulaReceptor);
                xmlTextWriter.WriteElementString("CorreoReceptor", Doc.CorreoReceptor);
                xmlTextWriter.WriteElementString("CantidadDecimales", Doc.DecimalesXML.ToString());
                xmlTextWriter.WriteElementString("FechaFactura", Doc.FechaFacutraXML.ToString("dd/MM/yyyy hh:mm:ss"));
                xmlTextWriter.WriteElementString("CondicionFactura", Doc.CondicionFactura);
                xmlTextWriter.WriteElementString("FormaPago1", Doc.FormaPago1);
                xmlTextWriter.WriteElementString("FormaPago2", Doc.FormaPago2);
                xmlTextWriter.WriteElementString("FormaPago3", Doc.FormaPago3);
                xmlTextWriter.WriteElementString("FormaPago4", Doc.FormaPago4);
                xmlTextWriter.WriteElementString("FormaPago5", Doc.FormaPago5);
                xmlTextWriter.WriteElementString("Plazo", Doc.PlazoCredito.ToString());
                xmlTextWriter.WriteElementString("Moneda", Doc.CodigoMoneda);
                xmlTextWriter.WriteElementString("TipoCambio", Doc.TipoCambio.ToString());
                xmlTextWriter.WriteElementString("NumeroFacturaContingencia", Doc.NumeroFacturaContingecia);
                xmlTextWriter.WriteElementString("FechaFacturaContingencia", Doc.FechaFacturaContingenica.ToString());
                xmlTextWriter.WriteElementString("ClaveDocumentoReferenciaNota", Doc.ClaveReferenciaNota);
                xmlTextWriter.WriteElementString("FechaDocumentoReferenciaNota", Doc.FechaDocumentoReferenciaNota.ToString("dd/MM/yyyy hh:mm:ss"));
                xmlTextWriter.WriteElementString("AplicaTotalDocumentoReferencia", Doc.AplicaTotalDocumento.ToString());
                //xmlTextWriter.WriteAttributeString("OtroTexto", Doc.WMNumeroVendedor);

                xmlTextWriter.WriteStartElement("Detalle");

                foreach (var item in Doc.detalle)
                {
                    xmlTextWriter.WriteStartElement("LineaDetalle");
                    xmlTextWriter.WriteElementString("NLinea", item.Linea.ToString());
                    xmlTextWriter.WriteElementString("Cantidad", item.Faccant.ToString());
                    xmlTextWriter.WriteElementString("CodigoArticulo", item.Artcode);
                    xmlTextWriter.WriteElementString("CABYS", item.Cabys);
                    xmlTextWriter.WriteElementString("DescripcionArticulo", item.Artdesc);
                    xmlTextWriter.WriteElementString("TipoArticulo", item.TipoArticulo.ToString());
                    xmlTextWriter.WriteElementString("Unidad", item.Unidad);
                    xmlTextWriter.WriteElementString("Precio", item.Artprec.ToString());
                    xmlTextWriter.WriteElementString("PorcentajeIva", item.PorcIVA.ToString());
                    xmlTextWriter.WriteElementString("PorcentajeConsumo", item.PorcICO.ToString());
                    xmlTextWriter.WriteElementString("PorcentajeDescuento", item.PorcDES.ToString());
                    xmlTextWriter.WriteElementString("TotalLinea", item.TotalLinea.ToString());
                    xmlTextWriter.WriteElementString("EsExonerado", item.Exonerado.ToString());
                    xmlTextWriter.WriteElementString("TipoExoneracion", item.TipoExoneracion);
                    xmlTextWriter.WriteElementString("NumerodeExoneracion", item.NumerodeExoneracion);
                    xmlTextWriter.WriteElementString("InstitucionExoneracion", item.InstitucionExoneracion);
                    xmlTextWriter.WriteElementString("FechaExoneracion", item.FechaExoneracion.ToString());
                    xmlTextWriter.WriteElementString("BaseExoneracion", item.BaseExoneracion.ToString());
                    xmlTextWriter.WriteEndElement();
                }

                xmlTextWriter.WriteEndElement();
                xmlTextWriter.WriteEndElement();
                xmlTextWriter.WriteEndDocument();
                xmlTextWriter.Flush();
                memoryStream.Seek(0L, SeekOrigin.Begin);
                xmlDocument.Load((Stream)memoryStream);
                xmlTextWriter.Close();

                Console.WriteLine("Guardando el XML interno [{0}]", XmlFile);
                System.IO.File.WriteAllText(XmlFile, xmlDocument.InnerXml, Encoding.UTF8);

                Console.WriteLine("\nEnviando XML...");

                //Obtención de resultados
                XmlNode Resultado;
                Resultado = Serv.EnviarDocumento(xmlDocument);

                XmlDocument Xmlbase64;
                if (!string.IsNullOrEmpty(Resultado["XMLFirmado"].InnerText))
                {
                    Xmlbase64 = DecodeBase64ToXML(Resultado["XMLFirmado"].InnerText);
                }

                Console.WriteLine("Guardando el XML firmado [{0}]", XmlSignedFile);
                System.IO.File.WriteAllText(XmlSignedFile, Base64.Decode(Resultado["XMLFirmado"].InnerText), Encoding.UTF8);

                string EstadoMovimiento = Resultado["EstadoMovimiento"].InnerText;
                string FechaRespuestaHacienda = Resultado["FechaRespuestaHacienda"].InnerText;

                string JsonEnviado = Resultado["JsonEnviado"].InnerText;
                string JsonRespuestaHacienda = Resultado["JsonRespuestaHacienda"].InnerText;

                XmlDocument XMLRespuestaHacienda;

                if (!string.IsNullOrEmpty(Resultado["XMLRespuestaHacienda"].InnerText))
                {
                    XMLRespuestaHacienda = DecodeBase64ToXML(Resultado["XMLRespuestaHacienda"].InnerText);
                    Console.WriteLine("Guardando respuesta de Hacienda [{0}]", RespHac);
                    System.IO.File.WriteAllText(RespHac, Base64.Decode(Resultado["XMLRespuestaHacienda"].InnerText), Encoding.UTF8);
                }

                string MensajeRespuesta = Resultado["JsonEnviado"].InnerText;
                string MensajeRespuestaHacienda = Resultado["MensajeRespuestaHacienda"].InnerText;
                string CausaError = Resultado["CausaError"].InnerText;
                string EstadoRespuetas = Resultado["EstadoRespuesta"].InnerText;

                Console.WriteLine("Estado respuesta: {0}", EstadoRespuetas);
                Console.WriteLine("Fecha Respuesta Hacienda: {0}", FechaRespuestaHacienda);
                Console.WriteLine("Mensaje respuesta de Hacienda: {0}", MensajeRespuestaHacienda);
                Console.WriteLine("Causa del error: {0}", CausaError);

                DateTime Hoy = DateTime.Now;

                /**
                 * Los estados de Hacienda son los siguientes: 
                 * 0=PRE-REGISTRO
                 * 1=REGISTRADO
                 * 2=RECIBIDO
                 * 3=PROCESANDO
                 * 4=ACEPTADO
                 * 5=RECHAZADO
                 * 6=ERROR
                 * 10=DESCONOCIDO
                 */
                string CodigoRespuesta = "10"; // Vamos a usar este código para DESCONOCIDO
                if (EstadoRespuetas == "PRE-REGISTRO")
                {
                    CodigoRespuesta = "0";
                }
                else if (EstadoRespuetas == "REGISTRADO")
                {
                    CodigoRespuesta = "1";
                }
                else if (EstadoRespuetas == "RECIBIDO")
                {
                    CodigoRespuesta = "2";
                }
                else if (EstadoRespuetas == "PROCESANDO")
                {
                    CodigoRespuesta = "3";
                }
                else if (EstadoRespuetas == "ACEPTADO")
                {
                    CodigoRespuesta = "4";
                }
                else if (EstadoRespuetas == "RECHAZADO")
                {
                    CodigoRespuesta = "5";
                }
                else if (EstadoRespuetas == "ERROR")
                {
                    CodigoRespuesta = "6";
                }

                if (CodigoRespuesta == "10")
                {
                    EstadoRespuetas = "DESCONOCIDO";
                }

                string referencia = "0"; // Esta versión no maneja referencia

                Console.WriteLine("\nGuardando estado del envío {0}", TxtFile);

                Bitacora.SaveMessage(
                        Hoy.ToString("dd/MM/yyyy hh:mm:ss") + "\n" +
                        "Estado del envío: " + CodigoRespuesta + " " + EstadoRespuetas + "\n" +
                        "Referencia: " + referencia + "\n" +
                        "Mensaje: " + MensajeRespuestaHacienda + "\n" + CausaError, TxtFile);

                // Guardar en base de datos el resultado de la consulta.
                // Este método solo hace el UPDATE, el insert lo hace Osais ya que aquí no se cuenta con todos los parámetros.
                var conn = Config.GetConnection();
                conn.Open();
                string sqlSent =
                    "UPDATE faestadoDocElect " +
                    "   SET estado = @ESTADO, descrip = @DESCRIP, fecha = Now(), referencia = @REF , xmlFirmado =  @XMLF " +
                    "Where Facnume = @FACNUME " +
                    "and Facnd = @FACND " +
                    "and tipoxml = @TIPOXML";

                var command = new MySqlCommand(sqlSent, conn);
                command.CommandTimeout = 60;
                command.Parameters.AddWithValue("ESTADO", CodigoRespuesta);
                command.Parameters.AddWithValue("DESCRIP", EstadoRespuetas + ":\n " + MensajeRespuestaHacienda + "\n" + CausaError);
                command.Parameters.AddWithValue("REF", referencia);
                command.Parameters.AddWithValue("FACNUME", Facnume);
                command.Parameters.AddWithValue("FACND", Facnd);
                command.Parameters.AddWithValue("TIPOXML", 'V');
                command.Parameters.AddWithValue("XMLF", Facnume + ".xml"); // El xml interno y el firmado se llaman igual pero quedan en distintas carpetas.
                command.ExecuteNonQuery();
            } catch (Exception ex)
            {
                Console.WriteLine(
                        DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") +
                        ": Ocurrió un error [" + ex.Message + " " + ex.InnerException.Message + "]");
                Bitacora.SaveMessage(DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss") +
                        " --> " + ex.Message + " " + ex.InnerException.Message, ErrorFile);
            }

            Console.WriteLine("\nFinaliza proceso: " + DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss"));
        } // EnviarFactura

        private static XmlDocument DecodeBase64ToXML(string valor)
        {
            string xml = Encoding.UTF8.GetString(Convert.FromBase64String(valor));
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(xml);
            return xmlDocument;
        }


    } // end class
}
