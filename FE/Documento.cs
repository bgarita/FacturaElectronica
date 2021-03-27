using EnviarFactura;
using FE.Testing;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FE
{
    // Los datos cargados y procesados en esta clase serán enviados por la clase DocumentoElectronico.cs
    class Documento
    {
        private Int32 Facnume;
        private Int32 Facnd;
        private Configuracion Config;
        private Certificado Cert;

        // Campos para la autenticación
        public string PinHacienda { get; set; }
        public string CertificadoHacienda { get; set; } // base 64
        public string UsuarioHacienda { get; set; }
        public string ClaveHacienda { get; set; }

        // Campos para el xml (Encabezado)
        public string CodigoActividad { get; set; }
        public string Clave { get; set; }
        public DateTime FechaFacutraXML { get; set; }
        public string NombreEmisor { get; set; }
        public string CedulaEmisor { get; set; }
        public string TipoCedulaEmisor { get; set; }
        // Acá iría el nombre comercial del emisor

        public string ProvinciaEmisor { get; set; }
        public string CantonEmisor { get; set; }
        public string DistritoEmisor { get; set; }
        public string BarrioEmisor { get; set; }
        public string OtrasSeñas { get; set; }
        public string CodigoPaisTelefono { get; set; }
        public string Telefono { get; set; }
        public string Fax { get; set; }
        public string CorreoEmisor { get; set; }

        public string NombreReceptor { get; set; }
        public string TipoCedulaReceptor { get; set; }
        public string CedulaReceptor { get; set; }
        public string CorreoReceptor { get; set; }
        public bool ClienteEsReceptor { get; set; }

        public bool ClienteEsExtranjero { get; set; }
        public string CedulaExtranjero { get; set; }
       
        public string NombreComercialReceptor { get; set; }
        
        public Int16 DecimalesXML { get; set; }

        public string CondicionFactura { get; set; }
        public Int32 PlazoCredito { get; set; }
        public string FormaPago1 { get; set; }
        public string FormaPago2 { get; set; }
        public string FormaPago3 { get; set; }
        public string FormaPago4 { get; set; }
        public string FormaPago5 { get; set; }

        
        public string CodigoMoneda { get; set; }
        public decimal TipoCambio { get; set; }

        public string NumeroFacturaContingecia { get; set; }
        public DateTime FechaFacturaContingenica { get; set; }
        public string ClaveReferenciaNota { get; set; }
        public DateTime FechaDocumentoReferenciaNota { get; set; }
        public bool AplicaTotalDocumento { get; set; } //Se aplica el total del documento con la nota?

        // Campos para Walmart
        public string WMNumeroVendedor { get; set; }
        public string WMNumeroOrden { get; set; }
        public string WMEnviarGLN { get; set; }
        public string WMNumeroReclamo { get; set; }
        public string WMFechaReclamo { get; set; }

        // Campos para el xml (Detalle)
        public List<DetalleDocumento> detalle;


        // Constructor
        public Documento(Configuracion config, Int32 facnume, Int32 facnd, Certificado cert)
        {
            this.Config = config;
            this.Facnume = facnume;
            this.Facnd = facnd;
            this.Cert = cert;
            this.detalle = new List<DetalleDocumento>();

            LoadData();
        }

        /**
         * Usar la conexión con la base de datos y cargar todas las variables de esta clase.
         */
        private void LoadData()
        {
            string TipoDoc;
            if (Facnd == 0)
            {
                TipoDoc = "FAC";
            } else if (Facnd > 0)
            {
                TipoDoc = "NCR";
            } else {
                TipoDoc = "NDB";
            }

            Console.WriteLine("Procesando documento {0}, tipo {1}", Facnume, TipoDoc);

            // Conectando con base de datos
            var conn = Config.GetConnection();
            conn.Open();

            // Obteniendo datos para el encabezado del XML
            string sqlSent =
                "SELECT faencabe.*,  " +
                "    (SELECT Empresa FROM config) AS Empresa, " +
                "    (SELECT codigoAtividadEconomica FROM config) AS codigoAtividadEconomica, " +
                "    (SELECT replace(cedulajur, '-', '') FROM config) AS cedulajur, " +
                "    (SELECT lpad(tipoID, 2, '0') FROM config)  AS tipoID, " +
                "    (SELECT provincia FROM config) as provincia,  " +
                "    (SELECT lpad(canton, 2, '0') FROM config)  AS canton, " +
                "    (SELECT lpad(distrito, 2, '0') FROM config)  AS distrito, " +
                "    (SELECT lpad(barrio, 2, '0') FROM config)  AS barrio, " +
                "    (SELECT direccion FROM config) AS direccion, " +
                "    (SELECT correoE FROM config) AS correoE, " +
                "    (SELECT replace(telefono1, '-', '') FROM config)  AS telefono1, " +
                "    inclient.idcliente,     " +
                "    inclient.cligenerico,   " +
                "    lpad(inclient.idtipo, 2, '0') AS idtipo, " +
                "    inclient.clinaci,   " +
                "    inclient.clidesc,   " +
                "    inclient.cliemail,  " +
                "    faencabe.facplazo,  " +
                "    IF(faencabe.facplazo = 0, '01', '02') as tipoVenta,  " +
                "    Case faencabe.factipo  " +
                "          When 0 THEN '01' " + // Desconocido (Efectivo para hacienda)
                "          When 1 THEN '01' " + // Efectivo
                "          When 2 THEN '03' " + // Cheque
                "          When 3 THEN '02' " + // Tarjeta
                "          When 4 THEN '04' " + // Transferencia
                "    End as tipoPago,  " +
                "    monedas.codigoHacienda,  " +
                "    monedas.descrip,  " +
                "    faencabe.tipoca,  " +
                "    ifNull(faotros.WMNumeroVendedor, '') AS WMNumeroVendedor,   " +
                "    ifNull(faotros.WMNumeroOrden, '') AS WMNumeroOrden,         " +
                "    ifNull(faotros.WMEnviarGLN, '') AS WMEnviarGLN,             " +
                "    ifNull(faotros.WMNumeroReclamo, '') AS WMNumeroReclamo,     " +
                "    ifNull(faotros.WMFechaReclamo, '') AS  WMFechaReclamo       " +
                " FROM faencabe" +
                " INNER JOIN inclient ON faencabe.clicode = inclient.clicode " +
                " INNER JOIN monedas ON faencabe.codigoTC = monedas.codigo   " +
                " LEFT JOIN faotros ON faencabe.facnume = faotros.facnume AND faencabe.facnd = faotros.facnd " +
                " Where faencabe.Facnume = @FACNUME " +
                " and faencabe.Facnd = @FACND ";

            var command = new MySqlCommand(sqlSent, conn);
            command.CommandTimeout = 60;
            command.Parameters.AddWithValue("FACNUME", Facnume);
            command.Parameters.AddWithValue("FACND", Facnd);

            MySqlDataReader DataReader = command.ExecuteReader();

            if (!DataReader.HasRows)
            {
                Console.WriteLine("No hay datos para el documento {0}, tipo {1}.", Facnume, Facnd);
                return;
            }

            DataReader.Read();

            // Cargar las variables
            this.Clave = DataReader.GetString("claveHacienda");
            this.CodigoActividad = DataReader.GetString("codigoAtividadEconomica"); // "741402"; // Laflor="155403";        // Esto se debe incluir en la tabla config
            this.NombreEmisor = DataReader.GetString("Empresa");
            this.CedulaEmisor = DataReader.GetString("cedulajur");
            this.TipoCedulaEmisor = DataReader.GetString("tipoID");
            this.ProvinciaEmisor = DataReader.GetString("provincia");
            this.CantonEmisor = DataReader.GetString("canton");
            this.DistritoEmisor = DataReader.GetString("distrito");
            this.BarrioEmisor = DataReader.GetString("barrio");
            this.OtrasSeñas = DataReader.GetString("direccion");
            this.CodigoPaisTelefono = "506";
            this.Telefono = DataReader.GetString("telefono1");
            this.Fax = DataReader.GetString("telefono1");
            this.CorreoEmisor = DataReader.GetString("correoE");
            this.PinHacienda = Cert.Pin;                        // Viene del archivo UserConfig.txt
            this.CertificadoHacienda = Cert.GetCertificadoBase64();
            this.UsuarioHacienda = Cert.Usuario;
            this.ClaveHacienda = Cert.Clave;
            this.ClienteEsReceptor = DataReader.GetInt32("cligenerico") == 0;
            this.ClienteEsExtranjero = DataReader.GetInt32("clinaci") == 0;
            this.CedulaExtranjero = "";                     // Hay que incluir esto en la tabla inclient y modificar los proceso de mantenimiento
            this.NombreReceptor = DataReader.GetString("clidesc");
            this.NombreComercialReceptor = "";              // No existe en la base de datos
            this.TipoCedulaReceptor = DataReader.GetString("idtipo");
            this.CedulaReceptor = DataReader.GetString("idcliente");
            this.CorreoReceptor = DataReader.GetString("cliemail");
            this.DecimalesXML = 5;
            this.FechaFacutraXML = DateTime.Now;
            this.CondicionFactura = DataReader.GetString("tipoVenta");
            this.FormaPago1 = DataReader.GetString("tipoPago");
            this.FormaPago2 = "";
            this.FormaPago3 = "";
            this.FormaPago4 = "";
            this.FormaPago5 = "";
            this.PlazoCredito = DataReader.GetInt32("facplazo");
            this.CodigoMoneda = DataReader.GetString("codigoHacienda");
            this.TipoCambio = DataReader.GetDecimal("tipoca");
            this.NumeroFacturaContingecia = ""; // No está implementado en osais
            this.FechaFacturaContingenica = DateTime.Now;
            this.ClaveReferenciaNota = "";      // Va vacío en las facturas
            this.FechaDocumentoReferenciaNota = DateTime.Now;
            this.AplicaTotalDocumento = TipoDoc.Equals("NCR");   //Se aplica el total del documento con la nota? false = si no es NCR

            this.WMEnviarGLN      = DataReader.GetString("WMEnviarGLN");
            this.WMFechaReclamo   = DataReader.GetString("WMFechaReclamo");
            this.WMNumeroOrden    = DataReader.GetString("WMNumeroOrden");
            this.WMNumeroReclamo  = DataReader.GetString("WMNumeroReclamo");
            this.WMNumeroVendedor = DataReader.GetString("WMNumeroVendedor");
            DataReader.Close();

            // Si es una nota de crédito se debe obtener la información de referencia.
            if (TipoDoc.Equals("NCR"))
            {
                // Hacienda permite hasta un máximo de 10 referencias pero por ahora solo se incluirá una.
                sqlSent
                    = "Select  "
                    + "	notasd.facnume, "
                    + "	notasd.facnd, "
                    + "	faencabe.facfech, "
                    + "	faencabe.claveHacienda "
                    + "from notasd  "
                    + "Inner join faencabe on notasd.facnume = faencabe.facnume and notasd.facnd = faencabe.facnd "
                    + "where notasd.Notanume = @FACNUME "
                    + "LIMIT 1";

                command = new MySqlCommand(sqlSent, conn);
                command.CommandTimeout = 60;
                command.Parameters.AddWithValue("FACNUME", Facnume);

                DataReader = command.ExecuteReader();

                if (DataReader.HasRows)
                {
                    DataReader.Read();
                    this.ClaveReferenciaNota = DataReader.GetString("claveHacienda");
                    this.FechaDocumentoReferenciaNota = DataReader.GetDateTime("facfech");
                }

                DataReader.Close();
            } // end if (TipoDoc.Equals("NCR"))


            // Obteniendo datos para el detalle del XML
            sqlSent
                = "Select  "
                + " if((Select artcode from inservice where artcode = fadetall.artcode) is null, 'N','S') as EsServicio,    "
                + "	fadetall.codigoCabys, " // Ver nota 17 del archivo Anexos y estructuras_V4.3.pdf
                + "	'04' as tipoCod, "      // Código interno
                + " If(IfNull(inservice.artcode, 0) = 0, 0, 1) AS TipoArticulo, "
                + "	fadetall.artcode, "
                + "	fadetall.faccant, "
                + "	'Unid' as unidadMedida, "
                + "	inarticu.artdesc, "
                + "	fadetall.artprec, "
                + "	fadetall.facdesc, "
                + " if (fadetall.facdesc > 0, fadetall.facdesc / fadetall.facmont * 100, 0) AS porcDES, "
                + "	fadetall.facmont, "
                + "	If(fadetall.facdesc > 0,'Buen cliente','') as NatDescuento, "
                + "	(fadetall.facmont - fadetall.facdesc) as subtotal, "
                + "	'01' as codImpuesto, "   // 01=IVA, 02=Selectivo de consumo... ver nota 8 del archivo Anexos y estructuras_V4.3.pdf
                + "	fadetall.codigoTarifa, " // 01=Excento, 08=Tarifa general 13%... ver nota 8.1 del archivo Anexos y estructuras_V4.3.pdf
                + "	fadetall.facpive, "
                + "	fadetall.facimve, "
                + "	0.00 as FactorIVA, " // Esperar forma de cálculo (Julio 2019)
                + "	(fadetall.facmont - fadetall.facdesc + fadetall.facimve) as MontoTotalLinea "
                + "from fadetall  "
                + "Inner join inarticu on fadetall.artcode = inarticu.artcode  "
                + "LEFT JOIN inservice ON fadetall.artcode = inservice.artcode "
                + "where facnume = @FACNUME  "
                + "and facnd = @FACND";

            DataReader.Close();

            command = new MySqlCommand(sqlSent, conn);
            command.CommandTimeout = 60;
            command.Parameters.AddWithValue("FACNUME", Facnume);
            command.Parameters.AddWithValue("FACND", Facnd);
            DataReader = command.ExecuteReader();

            if (!DataReader.HasRows)
            {
                Console.WriteLine("No hay detalle para el documento {0}, tipo {1}.", Facnume, Facnd);
                return;
            }

            int Linea = 0;
            DetalleDocumento de;
            while (DataReader.Read())
            {
                de = new DetalleDocumento();
                Linea++;
                de.Linea = Linea;
                de.Faccant = DataReader.GetDecimal("faccant");
                de.Artcode = DataReader.GetString("artcode");
                de.Cabys = DataReader.GetString("codigoCabys");
                de.Artdesc = DataReader.GetString("artdesc");
                de.Artprec = DataReader.GetDecimal("artprec");
                de.TipoArticulo = DataReader.GetInt32("TipoArticulo"); // 0=Producto, 1=Servicio
                de.Unidad = DataReader.GetString("unidadMedida");
                de.PorcIVA = DataReader.GetDecimal("facpive");
                de.PorcICO = 0;
                de.PorcDES = DataReader.GetDecimal("porcDES");
                de.TotalLinea = DataReader.GetDecimal("MontoTotalLinea");
                de.Exonerado = false;               // No está implementado
                de.TipoExoneracion = "";            // No está implementado
                de.NumerodeExoneracion = "";        // No está implementado
                de.InstitucionExoneracion = "";     // No está implementado
                de.FechaExoneracion = DateTime.Now;
                de.BaseExoneracion = 0;             //Base de Exoneración del 1 al 100 

                this.detalle.Add(de);
            } // end while

            DataReader.Close();

        } // end LoadData

    } // end class
}
