using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace RdN
{
    /// <summary>
    /// classe gérant l'aspect UI du réseau de neurone
    /// </summary>
    static public class Parser
    {
        /// <summary>
        /// sauvegarde dans un fichier le réseau de neurone
        /// </summary>
        /// <param name="reseau">réseau</param>
        /// <param name="path">chemin complet de la sauvegarde</param>
        static public void SauvegarderReseau(Reseau reseau, string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, reseau);
            stream.Close();
        }

        /// <summary>
        /// charge un réseau de neurone préalablement chargé en binaire
        /// </summary>
        /// <param name="path">chemin complet du réseau</param>
        /// <returns>réseau chargé</returns>
        static public Reseau ChargerReseau(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Reseau obj = (Reseau)formatter.Deserialize(stream);
            stream.Close();

            return obj;
        }
    }
}
