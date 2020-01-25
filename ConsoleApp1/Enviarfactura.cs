using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace EnviarFactura
{
    class Enviarfactura
    {
        /*
         * Debe recibir cuatro valores dentro de args:
         * 1=Nombre del xml, 
         * 2=Secuencia (documento interno), 
         * 3=Acción (1=Enviar archivo, 2=Consultar estado, 3=Recibir xml, 4=Consultar estado proveedores)
         * 4=TipoDoc documento (FAC, NDB, NCR)
         * 
         * Nota1: Si la acción es 2 o 4, entonces el primer valor debe ser la referencia a consultar.
         * Para todos los efectos este valor es el que viene en el campo id_referencia
         * cuando se hace el registro de la factura. El valor de id_referencia queda guardado
         * en un archivo que tiene el mismo nombre del xml enviado pero con la extensión txt.
         * En el caso de las los envíos el nombre es la misma referencia.
         * 
         * Nota2: La secuenca se refiere al número de factura interna y auque puede ir vacío 
         * este programa no lo aceptará ya que se usará para crear el archivo que registra
         * el Estado_guardado del envío. Este archivo queda conformado con el nombre del xml enviado 
         * pero con la extensión .txt
         * 
         * Nota3: Si la acción es 3 (recibir archivo xml) entonces los valores en el array serán:
         * 1=Nombre del xml, 
         * 2=Tipo de cédula del emisor, 
         * 3=3
         * 4=N/A
         * 
         * Archivos que genera este programa:
         * 1.   Error.log 
         *      Este archivo contiene el último error que se haya producido en tiempo de corrida.
         *      Su contenido es la excepción atrapada por el try-catch en la mayoría de los casos.
         * 2.   nnnnn_Err.txt
         *      Este archivo contiene los errores que se generaron al enviar una factura o consultarla.
         *      Su contenido es específico, de tal manera que los errores serán exclusivos de una factura.
         *      El nombre de este archivo está conformado por la secuencia o número de factura interna
         *      más la palabra _Err.txt
         * 3.   nnnnn_Hac.txt
         *      Este archivo contiene el Estado_guardado del envío del xml cuando se hace la consulta.
         *      Su contenido es específico para cada documento xml enviado.
         *      Está conformado por el parámetro secuencia, que en este caso se refiere al número de
         *      referencia que se generó al hacer el envío (ese número queda en el archivo que sigue).
         * 4.   nnnnn.txt
         *      Este archivo contiene el Estado_guardado del proceso de envío al momento de realizar el envío.
         *      Su contenido es similar a esto:
         *      25-09-2018
         *      Estado del envío: 1
         *      Referencia: 5119
         *      Mensaje: Se registró el XML correctamente
         *      Su nombre es igual al xml pero con la extensión .txt
         * 5.   nnnnn.xml
         *      Este archivo es el xml firmado que se envió a Hacienda.
         *      Su nombre está conformado por el número consecutivo que queda en
         *      el archivo nnnnn_Hac.txt + la extensión .xml
         *  
         */
        static void Main(string[] args)
        {
            // DEBUG
            /*
            args = new string[4];
            args[0] = "00100001050000000017.xml";
            args[1] = "01";
            args[2] = "3";
            args[3] = "FAC"; // FCO para las facturas de compra

            //Consultar(args);
            RecibirFactura(args);

            return;
            */

            // El nonbre del archivo a enviar viene por parámetro.
            // Este programa debe estar en la misma carpeta donde
            // se encuentran todos los xml.
            DateTime Hoy = DateTime.Now;
            Console.WriteLine("Inicia proceso: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Validación de parámetros recibidos.");


            try
            {
                EscribirMensaje em = new EscribirMensaje();

                string Dir = "xmls\\";
                string Err = "errores\\";
                string ErrorFile;

                ErrorFile = Dir + Err + "Error.txt";

                if (args != null && args.Length > 0)
                {
                    Console.WriteLine("Argumentos recibidos");
                    for (int i = 0; i < args.Length; i++)
                    {
                        Console.WriteLine("Argumento " + i + " = " + args[i]);
                    }
                    
                    if (args[2] == "1") // Enviar documento xml
                    {
                        Enviar(args);           
                    } else if (args[2] == "2" || args[2] == "4") // Consultar documento
                    {
                        Consultar(args);        
                    } else if (args[2] == "3") // Receptor: Enviar respuesta de documento recibido
                    {
                        RecibirFactura(args);   
                    }
                }
                else
                {
                    Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": No se recibió el nombre del xml.");
                    em.SaveMessage(Hoy.ToString("dd-MM-yyyy") + " --> No se recibió el nombre del xml.", ErrorFile);
                    return;
                }

            } catch (Exception ex) {
                Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Error " + ex);
            }
        } // end Main




        private static XmlDocument Base64ToXML(string documento)
        {
            XmlDocument resultante = new XmlDocument();
            Byte[] myBase64ret = Convert.FromBase64String(documento);
            string myStr = System.Text.Encoding.UTF8.GetString(myBase64ret);
            resultante.LoadXml(myStr);

            return resultante;
        } // end Base64ToXML



        /*
         * Enviar un xml a Hacienda.
         * En el array debe venir: 1=Nombre del xml, 2=Secuencia, 3=1, 4=TipoDoc (FAC, NDB, NCR, FCO, N/A)
         */
        private static void Enviar(string[] args)
        {
            DateTime Hoy = DateTime.Now;
            Console.WriteLine("Inicia proceso: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            string Dir = "xmls\\";
            string Err = "errores\\";
            string XmlFile;
            string Secuencia;
            string ErrorFile;
            string Accion;
            string TipoDoc;
            int Facnume, Facnd;

            // Variable para distinguir si el xml es de compras o de ventas.
            string TipoXml; // C=Compras, V=Ventas

            ErrorFile = Dir + Err + "Error.txt";
            Secuencia = "1";
            EscribirMensaje Bitacora = new EscribirMensaje();

            Accion = ""; // 1=Enviar factura, 2=Consultar el Estado_guardado.

            FacturaElectronica.Factura wsFactura = new FacturaElectronica.Factura();
            wsFactura.UserDetailsValue = new FacturaElectronica.UserDetails();
            wsFactura.UserDetailsValue.UserName = "desisatec";
            wsFactura.UserDetailsValue.Password = "fgyGhxviz@X6uv^*5o";

            try
            {
                Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Validación de parámetros recibidos.");
                if (args != null && args.Length > 0)
                {
                    XmlFile = Dir + args[0]; // Nombre del xml
                    Secuencia = args[1];     // Número de factura interna
                    Accion = args[2];        // En este caso la acción solo puede ser "1"
                    TipoDoc = args[3];       // TipoDoc de documento
                }
                else
                {
                    Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": No se recibió el nombre del xml.");
                    Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy") + " --> No se recibió el nombre del xml.", ErrorFile);
                    return;
                }

                ErrorFile = Dir + Err + Secuencia + "_Err" + ".txt";


                // Si la acción no es 1...
                if (Accion != "1")
                {
                    Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> El código de acción para envío sólo puede ser 1.", ErrorFile);
                    Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Error. El tercer parámetro no es un valor aceptado [" + Accion + "]");
                    return;
                } // end if

                // DEBUG:
                Console.WriteLine("Factura recibida: " + Secuencia);

                Facnume = Int32.Parse(Secuencia);
                bool error = false;

                // Bosco modificado 25/06/2019.  Agrego el tipo de documento para facturas de compra (Regimen simplificado)
                switch (TipoDoc)
                {
                    case "FAC":
                        Facnd = 0;
                        TipoXml = "V";
                        break;
                    case "NCR":
                        Facnd = System.Math.Abs(Facnume);
                        TipoXml = "V";
                        break;
                    case "NDB":
                        Facnd = Facnume * -1;
                        TipoXml = "V";
                        break;
                    case "FCO":
                        Facnd = 0;
                        TipoXml = "C";
                        break;
                    default:
                        Facnd = -1;
                        error = true;
                        TipoXml = "";
                        break;
                } // end switch

                // Si no se puede identificar el TipoDoc de documento no se puede continuar.
                if (error)
                {
                    Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> El TipoDoc de documento no viene identificado, no se puede continuar.", ErrorFile);
                    Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Error. El TipoDoc de documento no viene identificado, no se puede continuar.");
                    return;
                }

                //XmlFile = "C:\\xmls\\452-.xml"; // Solo debug


                // Inicia proceso de envío del xml
                string txtFile = XmlFile.Substring(0, XmlFile.IndexOf(".")) + ".txt";


                /*
                 * DEBUG:
                 * Se comentan las siguientes 7 líneas de código y se descomentan las otras 3.
                 */
               
                string xmlText = System.IO.File.ReadAllText(XmlFile);
                Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Enviando archivo [" + XmlFile + "]");

                System.Xml.XmlNode respuesta = 
                    wsFactura.RegistraXMLFactura(xmlText, Secuencia).SelectSingleNode("Resultado");

                string CodigoRespuesta = respuesta["estado"].InnerText;
                string referencia = respuesta["id_referencia"].InnerText;
                string mensaje = respuesta["respuesta"].InnerText;
                
                /*
                string CodigoRespuesta = "1";
                string referencia = "6321212";
                string mensaje = "XML Registrado satisfactoriamente.";
                */
                

                Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") +
                        ": El estado del envío es: " + CodigoRespuesta + " " +
                        "y la referencia es: " + referencia);

                string explica_estado;
                if (CodigoRespuesta == "1")
                {
                    explica_estado = "Enviado satisfactoriamente.";
                }
                else
                {
                    explica_estado = "Error al enviar el XML.";
                }

                // Cuando la respuesta es negativa no existe la referencia
                if (CodigoRespuesta == "-1")
                {
                    referencia = CodigoRespuesta;
                }

                Bitacora.SaveMessage(
                    Hoy.ToString("dd-MM-yyyy") + "\n" +
                    "Estado del envío: " + CodigoRespuesta + "\n" +
                    "Referencia: " + referencia + "\n" +
                    "Mensaje: " + mensaje, txtFile);


                // Guardar en base de datos el resultado de la consulta.
                var conn = new Configuracion().GetConnection();
                conn.Open();
                string sqlSent =
                    "UPDATE faestadoDocElect " +
                    "   SET estado = @ESTADO, descrip = @DESCRIP, fecha = Now(), referencia = @REF " +
                    "Where Facnume = @FACNUME " + 
                    "and Facnd = @FACND " +
                    "and tipoxml = @TIPOXML";

                var command = new MySqlCommand(sqlSent, conn);
                command.CommandTimeout = 60;
                command.Parameters.AddWithValue("ESTADO", CodigoRespuesta);
                command.Parameters.AddWithValue("DESCRIP", explica_estado + " " + mensaje);
                command.Parameters.AddWithValue("REF", referencia);
                command.Parameters.AddWithValue("FACNUME", Facnume);
                command.Parameters.AddWithValue("FACND", Facnd);
                command.Parameters.AddWithValue("TIPOXML", TipoXml);
                command.ExecuteNonQuery();
                
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") +
                        ": Ocurrió un error [" + ex.Message + "]");
                Bitacora.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> " + ex.Message, ErrorFile);
            }

            Console.WriteLine("Finaliza proceso: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
        } // end Enviar



        /*
         * Consultar un documento electrónico.
         * En el array debe venir: 1=Factura, 2=Secuencia, 3=(2 o 4), 4=Tipo (FAC, NCR, NDB, FCO)
         */
        private static void Consultar(string[] args)
        {
            DateTime Hoy = DateTime.Now;
            // El nonbre del archivo viene por parámetro.

            string Dir = "xmls\\";
            string Err = "errores\\";
            string Firmados = "firmados\\";
            string Logs = "logs\\";
            string XmlFile;
            string TipoXml;
            string Secuencia;
            string ErrorFile;
            string Accion;


            ErrorFile = Dir + Err + "Error.txt";
            Secuencia = "1";
            EscribirMensaje em = new EscribirMensaje();

            try
            {
                Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Validación de parámetros recibidos.");
                if (args != null && args.Length > 0)
                {
                    XmlFile = Dir + args[0];
                    Secuencia = args[1];
                    Accion = args[2];
                    TipoXml = args[3];
                }
                else
                {
                    Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": No se recibió el nombre del xml.");
                    em.SaveMessage(Hoy.ToString("dd-MM-yyyy") + " --> No se recibió el nombre del xml.", ErrorFile);
                    return;
                }

                ErrorFile = Dir + Err + Secuencia + "_Err" + ".txt";

                // Si la acción no es 2 o 4...
                if (Accion != "2" && Accion != "4")
                {
                    em.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> El código de acción para consultas sólo puede ser 2 o 4.", ErrorFile);
                    Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Error. El tercer parámetro no es un valor aceptado [" + Accion + "]");
                    return;
                } // end if
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") +
                        ": Ocurrió un error en la validación de parámetros [" + ex.Message + "]");
                em.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> " + ex.Message, ErrorFile);
                return;
            } // end try-catch

            /*
             * Validar el Estado_guardado del envío en base de datos. 
             * Si el código del Estado_guardado es 4 ó 5 no hago la consulta ya que los estados no van a cambiar.
             * 
             * Aquí hay que ver qué se hace porque este método se usa tanto para facturas
             * de clientes como de proveedores, y por ahora solo los estados de facturas
             * de clientes se guardan en base de datos.  Más adelante se puede agregar un
             * campo a la tabla de manera que se pueda identificar cuando la factura es
             * de clientes o de proveedores y de esa forma se estandarice el proceso de
             * consulta.
             * Actualización: 29/12/2018 se agregó el campo referencia.  Ese campo es único en
             * Hacienda y pertenece a cualquier documento electrónico, ya sea factura ND, NC, Tiquete
             * electrónico, etc., tanto para clientes como para proveedores.
             * 
             */

            int Estado_guardado = 99;
            string sqlSent =
                "Select estado from faestadoDocElect " +
                "Where referencia = @REF";
            try
            {
                // Crear la conexión con la base de datos
                var conn = new Configuracion().GetConnection();
                conn.Open();
                var command = new MySqlCommand(sqlSent, conn);
                command.CommandTimeout = 60;
                command.Parameters.AddWithValue("REF", args[0]);

                MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    reader.Read();
                    Estado_guardado = reader.GetInt16("estado");
                } // end if
                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") +
                        ": Ocurrió un error [" + ex.Message + "]");
                em.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> " + ex.Message, ErrorFile);
                return;
            } // end try-catch


            // Si el Estado_guardado es 4 ó 5 no hago la consulta ya que ese valor no va a cambiar.
            if (Estado_guardado == 4 || Estado_guardado == 5)
            {
                Console.WriteLine("Finaliza proceso: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
                return;
            } // end if

            // Si el Estado_guardado es 99 es porque el documento no fue encontrado
            if (Estado_guardado == 99)
            {
                em.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> " + "La referencia " + args[0] + 
                        " no existe en base de datos (estado=99), aún así se hará la consulta al Ministerio de Hacienda.", 
                        ErrorFile);
            } // end if

            //Accion = ""; // 1=Enviar factura, 2=Consultar el estado.

            FacturaElectronica.Factura wsFactura = new FacturaElectronica.Factura();
            wsFactura.UserDetailsValue = new FacturaElectronica.UserDetails();
            wsFactura.UserDetailsValue.UserName = "desisatec";
            wsFactura.UserDetailsValue.Password = "fgyGhxviz@X6uv^*5o";

            try
            {
                Console.WriteLine(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Consultando referencia " + args[0]);
                int idFactura = int.Parse(args[0]);
                XmlNode xmlresultante = wsFactura.GetFacturaEstadoFull(idFactura).SelectSingleNode("Resultado");
                string estadoHacienda = xmlresultante["estado"].InnerText;
                string respuesta_ext = xmlresultante["respuesta_ext"].InnerText;
                string numero_consecutivo = xmlresultante["numero_consecutivo"].InnerText;
                string claveHacienda = xmlresultante["clave"].InnerText;

                string explica_estado;
                switch (estadoHacienda)
                {
                    case "0":
                        explica_estado = "PRE-REGISTRO"; break;
                    case "1":
                        explica_estado = "REGISTRADO"; break;
                    case "2":
                        explica_estado = "RECIBIDO"; break;
                    case "3":
                        explica_estado = "PROCESANDO"; break;
                    case "4":
                        explica_estado = "ACEPTADO"; break;
                    case "5":
                        explica_estado = "RECHAZADO"; break;
                    case "6":
                        explica_estado = "ERROR"; break;
                    default:
                        explica_estado = "DESCONOCIDO"; break;
                } // end switch

                Console.WriteLine(
                    Hoy.ToString("dd-MM-yyyy HH:mm:ss") +
                    ": El estado reportado por Hacienda es: " + estadoHacienda + " " + explica_estado);

                // Guardar el estado de la consulta
                string txtFile2 = Dir + Logs + Secuencia + "_Hac" + ".log";

                if (Accion == "4") { // Para los proveedores el nombre termina en P
                    txtFile2 = Dir + Logs + Secuencia + "_HacP" + ".log";
                } // end if

                // Si es una factura de compra se debe diferenciar el log
                if (TipoXml == "FCO")
                {
                    txtFile2 = Dir + Logs + Secuencia + "_HacCompras" + ".log";
                }

                Console.WriteLine("Log generado: " + txtFile2);

                em.SaveMessage(
                    "Estado: " + estadoHacienda + " " + explica_estado + "\n" +
                    "Respuesta_ext: " + respuesta_ext + "\n" +
                    "Numero_consecutivo: " + numero_consecutivo + "\n" +
                    "claveHacienda: " + claveHacienda, txtFile2);

                Console.WriteLine(
                    Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Guardando xml firmado y devuelto por Hacienda.");

                // Guardar los xml devueltos por Hacienda.
                string xmlBase64 = xmlresultante["xmlFacturaBase64"].InnerText; //Aquí viene el xml firmado, tal cual se envió a Hacienda
                string xmlBase64Respuesta = xmlresultante["xmlRespuestaBase64"].InnerText; //Aquí viene el xml que devuelve Hacienda

                string RutaArchivo = Dir + Firmados + numero_consecutivo + "_resp.xml";
                XmlDocument xmlDecodificado;

                if (string.IsNullOrEmpty(xmlBase64Respuesta))
                {
                    Console.WriteLine(
                    Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Hacienda aún no ha emitido respuesta firmada.");
                } else
                {
                    xmlDecodificado = Base64ToXML(xmlBase64Respuesta);
                    xmlDecodificado.Save(RutaArchivo);
                } // end if-else
                

                RutaArchivo = Dir + Firmados + numero_consecutivo + ".xml";

                if (string.IsNullOrEmpty(xmlBase64))
                {
                    Console.WriteLine(
                    Hoy.ToString("dd-MM-yyyy HH:mm:ss") + ": Hacienda aún no ha devuelto el xml firmado.");
                }
                else
                {
                    xmlDecodificado = Base64ToXML(xmlBase64);
                    xmlDecodificado.Save(RutaArchivo);
                } // end if-else
                


                // Guardar en base de datos el resultado de la consulta.
                // En la descripción del estado concatenar explica_estado + respuesta_ext
                // porque respuesta_ext tiene la explicación cuando se rechaza un documento.
                // Crear la conexión con la base de datos

                var conn = new Configuracion().GetConnection();
                conn = new Configuracion().GetConnection();
                conn.Open();
                sqlSent =
                    "UPDATE faestadoDocElect " +
                    "   SET estado = @ESTADO, descrip = @DESCRIP, fecha = Now(), " +
                    "       xmlfirmado = @XML " +
                    "Where referencia = @REF";

                var command = new MySqlCommand(sqlSent, conn);
                command.CommandTimeout = 60;
                command.Parameters.AddWithValue("ESTADO", estadoHacienda);
                command.Parameters.AddWithValue("DESCRIP", explica_estado + " " + respuesta_ext);
                command.Parameters.AddWithValue("XML", (string.IsNullOrEmpty(xmlBase64) ? " " : numero_consecutivo + ".xml")); // No se guarda la ruta, solo el nombre.
                command.Parameters.AddWithValue("REF", args[0]);
                command.ExecuteNonQuery();

                conn.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                        Hoy.ToString("dd-MM-yyyy HH:mm:ss") +
                        ": Ocurrió un error [" + ex.Message + "]");
                em.SaveMessage(Hoy.ToString("dd-MM-yyyy") +
                        " --> " + ex.Message, ErrorFile);
            }

            Console.WriteLine("Finaliza proceso: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
        } // end consultar






        /*
         * Este método envía la respuesta sobre un xml recibido.
         * Parámetros: args[0]=Este parámetro lleva nombre del xml con la respuesta hacia Hacienda
         *             args[1]=Este parámetro lleva el tipo de cédula del emisor
         *             args[2]=Este parámetro lleva el TipoDoc de respuesta a enviar
         */
        private static void RecibirFactura(string[] args)
        {
            /*
             * DEBUG
             * //String SystemHome = "C:\\Java Programs\\osais\\"; // En producción debe quedar vacío
             */

            String SystemHome = ""; // En producción debe quedar vacío
            DateTime Hoy = DateTime.Now;
            Console.WriteLine("Inicia proceso de envío de respuesta: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            string tipoCedulaEmisor = args[1];

            EscribirMensaje em = new EscribirMensaje();
            string Dir = SystemHome + "xmls\\";
            
            string XmlFile = Dir + "proveedores\\" + args[0];
            string xmlMensajeRespuesta = System.IO.File.ReadAllText(XmlFile);


            Console.WriteLine("Autenticando con servidor remoto: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
            FacturaElectronica.Factura wsFactura = new FacturaElectronica.Factura();
            wsFactura.UserDetailsValue = new FacturaElectronica.UserDetails();
            wsFactura.UserDetailsValue.UserName = "desisatec";
            wsFactura.UserDetailsValue.Password = "fgyGhxviz@X6uv^*5o";

            Console.WriteLine("Enviando respuesta: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
            XmlNode respuesta = wsFactura.RegistraMensajeReceptor(xmlMensajeRespuesta, tipoCedulaEmisor).SelectSingleNode("Resultado");

            Console.WriteLine("Generando bitácora: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));

            //string XMLResultado = respuesta["Estado_guardado"].InnerText;
            string XMLResultado = respuesta["estado"].InnerText;
            string XMLDescripcion = respuesta["estado_descripcion"].InnerText;
            string XMLIDReferencia = respuesta["id_referencia"].InnerText;
            string XMLRespuesta = respuesta["respuesta"].InnerText;
            DateTime XMLFechaI = DateTime.Parse(respuesta["fecha_proceso"].InnerText);
            string XMLFecha = XMLFechaI.ToString("yyyy-MM-dd HH:mm:ss");

            string logFile = Dir + "proveedores\\" + XMLIDReferencia + ".log";
            em.SaveMessage(Hoy.ToString("dd-MM-yyyy HH:mm:ss") + "\n" 
                + "Estado: " + XMLResultado + " " + XMLDescripcion + "\n" 
                + "Referencia: " + XMLIDReferencia + "\n" 
                + "Respuesta: " + XMLRespuesta, logFile);

            Console.WriteLine("Finaliza proceso de envío de respuesta: " + Hoy.ToString("dd-MM-yyyy HH:mm:ss"));
        } // end RecibirFactura
    } // end class
} // end namespace
