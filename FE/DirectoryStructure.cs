using System.IO;
using Newtonsoft.Json;

/*
 * Carga la clase Dir con el contenido de un archivo JSon cuyo contenido es el árbol
 * de carpetas usadas por Osais y compartidas por este ejecutable.
 */
namespace EnviarFactura.Testing
{
    class DirectoryStructure
    {
        public Dir Structure { get; set; }
        private readonly string JsonFile;

        // Recibe el nombre (ruta completa) del archivo JSon que será cargado.
        public DirectoryStructure(string JsonFile)
        {
            this.JsonFile = JsonFile;
            LoadJsonFile();
        }

        private void LoadJsonFile()
        {
            Structure = JsonConvert.DeserializeObject<Dir>(File.ReadAllText(@JsonFile));
        } // end LoadJsonFile
    } // end class
} // end namespace
