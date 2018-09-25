using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace RdN
{
    //TODO: ajouter sauvegarde/chargement

    /// <summary>
    /// classe servant au reseau de reseau
    /// </summary>
    [Serializable]
    class ReseauPrivee
    {
        /// <summary>
        /// superviseur utilisant le RdN
        /// </summary>
        [NonSerialized]
        public Superviseur Super;
        /// <summary>
        /// true: le reseau utilise la matrice de depart pour ses calculs
        /// </summary>
        public bool UtiliseMatriceEntrees;
        /// <summary>
        /// index des reseaux de neurone utilisé en entrée
        /// </summary>
        public int[] ReseauxEntrees;

        /// <summary>
        /// derniere sortie calcule du reseau de neurone
        /// </summary>
        public double[] DerniereSortie { get; private set; }

        /// <summary>
        /// calcul la sortie du RdN
        /// </summary>
        /// <param name="entrees">matrice d'entrees</param>
        /// <returns>matrice de sortie</returns>
        public double[] Calcul(double[] entrees)
        {
            this.DerniereSortie = this.Super.Calculer(entrees);
            return this.DerniereSortie;
        }
    }

    /// <summary>
    /// classe associa
    [Serializable]
    public class Reseau_Reseaux
    {
        /// <summary>
        /// liste des RdN a assembler pour generer la matrice de sortie du reseau des RdN
        /// </summary>
        public int[] AssemblageSortie = new int[0];

        /// <summary>
        /// liste des reseaux de neurones utilisés dans le reseau de reseau
        /// </summary>
        private List<ReseauPrivee> ListRdN = new List<ReseauPrivee>();

        /// <summary>
        /// Creer un RdN utilisant seulement la matrice d'entree du reseau des reseaux
        /// </summary>
        /// <param name="nbrNeurone">nombre de neurones</param>
        /// <param name="tailleMatriceEntree">taille de la matrice d'entree</param>
        public void CreerReseau(int[] nbrNeurone, int tailleMatriceEntree)
        {
            Reseau RdN_temp = new Reseau(nbrNeurone, tailleMatriceEntree);
            Superviseur Super_temp = new Superviseur(RdN_temp);
            ListRdN.Add(new ReseauPrivee()
                {
                    Super = Super_temp,
                    UtiliseMatriceEntrees = true,
                    ReseauxEntrees = new int[0]
                });
        }

        /// <summary>
        /// sauvegarde dans un fichier le RdR
        /// </summary>
        /// <param name="reseau">réseau</param>
        /// <param name="path">chemin complet de la sauvegarde</param>
        public void SauvegarderReseau(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this);
            for (int i = 0; i < this.ListRdN.Count; i++)
            {
                this.ListRdN[i].Super.SauvegarderEchantillons("echantillon " + i + ".ech");
            }
            
            stream.Close();
        }

        /// <summary>
        /// charge un RdR préalablement chargé en binaire
        /// </summary>
        /// <param name="path">chemin complet du réseau</param>
        /// <returns>réseau chargé</returns>
        public static Reseau_Reseaux ChargerReseau(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            Reseau_Reseaux obj = (Reseau_Reseaux)formatter.Deserialize(stream);
            
            stream.Close();

            return obj;
        }

        /// <summary>
        /// supprime un echantillon servant à l'apprentissage
        /// </summary>
        /// <param name="index">index de l'echantillon a supprimer</param>
        public void SupprimerEchantillon(int index)
        {
            for (int i = 0; i < this.ListRdN.Count; i++)
            {
                this.ListRdN[i].Super.AjouteIndexSupprimer(index);
            }
        }

        /// <summary>
        /// Creer un RdN utilisant d'autres RdN en entree
        /// </summary>
        /// <param name="nbrNeurone">nombre de neurones du reseau</param>
        /// <param name="indexReseauxEntree">index des reseaux a utiliser en entree (les reseaux doivent deja etre cree)</param>
        /// <param name="tailleMatriceEntree">taille de la matrice d'entree du reseau des reseaux (si null, alors ce reseau n'utilisera pas la matrice d'entree)</param>
        public void CreerReseau(int[] nbrNeurone, int[] indexReseauxEntree, int tailleMatriceEntree)
        {
            int tailleEntree = tailleMatriceEntree;
            for (int i = 0; i < indexReseauxEntree.Length; i++)
            {
                try
                {
                    tailleEntree += this.ListRdN[indexReseauxEntree[i]].Super.GetReseau().NbrSorties;
                }
                catch(Exception)
                {
                    throw new Exception("l'index donnee en parametre pointe sur un reseau non cree");
                }
            }
            
            bool UtiliseEntree;
            if (tailleMatriceEntree == 0)
                UtiliseEntree = false;
            else
                UtiliseEntree = true;

            Reseau RdN_temp = new Reseau(nbrNeurone, tailleEntree);
            Superviseur Super_temp = new Superviseur(RdN_temp);
            ReseauPrivee temp = new ReseauPrivee()
                {
                    Super = Super_temp,
                    UtiliseMatriceEntrees = UtiliseEntree,
                    ReseauxEntrees = indexReseauxEntree,
                };
            this.ListRdN.Add(temp);
        }

        /// <summary>
        /// retourne le superviseur du numero de reseau associe
        /// </summary>
        /// <param name="nbrRdN">numero du RdN du reseau</param>
        /// <returns>superviseur</returns>
        public Superviseur GetSuperviseur(int nbrRdN)
        {
            return this.ListRdN[nbrRdN].Super;
        }

        /// <summary>
        /// Calcul la sortie du reseau
        /// </summary>
        /// <param name="entrees">matrice d'entrees</param>
        /// <returns>matrice de sortie</returns>
        public double[] Calcul(double[] entrees)
        {
            for (int i = 0; i < this.ListRdN.Count; i++)
            {
                this.ListRdN[i].Calcul(CreerMatriceEntree(i, entrees));
            }

            return this.CreerMatriceSortie();
        }

        /// <summary>
        /// genere la matrice de sortie du RdR
        /// </summary>
        /// <returns>matrice de sortie</returns>
        private double[] CreerMatriceSortie()
        {
            List<double> list = new List<double>();
            for (int i = 0; i < this.AssemblageSortie.Length; i++)
            {
                list.AddRange(this.ListRdN[this.AssemblageSortie[i]].DerniereSortie);
            }

            return list.ToArray();
        }

        /// <summary>
        /// creer la matrice d'entree du RdN demande
        /// </summary>
        /// <param name="nbrReseau">numero du RdN</param>
        /// <param name="matriceEntrees">matrice d'entree du RdR</param>
        /// <returns>matrice d'entree</returns>
        private double[] CreerMatriceEntree(int nbrReseau, double[] matriceEntrees)
        {
            List<double> list = new List<double>();
            list.AddRange(matriceEntrees.ToList());

            ReseauPrivee temp = this.ListRdN[nbrReseau];
            for (int i = 0; i < temp.ReseauxEntrees.Length; i++)
            {
                list.AddRange(this.ListRdN[temp.ReseauxEntrees[i]].DerniereSortie);
            }

            return list.ToArray();
        }

        /// <summary>
        /// ajoute un echantillon pour l'apprentissage au RdR
        /// </summary>
        /// <param name="nbrReseau">index du RdN</param>
        /// <param name="entrees">matrice d'entrees du RdR</param>
        /// <param name="sortie">liste des sorties souhaitée de chaque RdN</param>
        public void AjouterEchantillon(double[] entrees, double[][] sorties)
        {
            if (sorties.Length != this.ListRdN.Count)
                throw new Exception("nombre de matrices de sorties différente du nombre de RdN");

            for (int i = 0; i < sorties.Length; i++)
            {
                this.ListRdN[i].Super.AddEchantillons(this.CreerEchantillonEntrees(i, sorties, entrees), sorties[i]);
            }
        }

        /// <summary>
        /// arrete l'apprentissage du RdR
        /// </summary>
        public void ArreterApprentissage()
        {
            for (int i = 0; i < this.ListRdN.Count; i++)
            {
                this.ListRdN[i].Super.StopperApprentissage();
            }
        }

        /// <summary>
        /// retourne la liste des reseaux de neurones
        /// </summary>
        /// <returns>list des reseaux de neurone</returns>
        public List<Reseau> GetRdR()
        {
            List<Reseau> list = new List<Reseau>();
            for (int i = 0; i < this.ListRdN.Count; i++)
            {
                list.Add(this.ListRdN[i].Super.GetReseau());
            }

            return list;
        }

        /// <summary>
        /// cree la matrice d'entrees du RdN souhaite
        /// </summary>
        /// <param name="nbrRdN">index du RdN souhaite</param>
        /// <param name="sorties">liste des echantillons de sortie de tous les reseaux</param>
        /// <param name="entreesRdR">matrice d'entrees du RdR</param>
        /// <returns>matrice d'entrees du RdN</returns>
        private double[] CreerEchantillonEntrees(int nbrRdN, double[][] sorties, double[] entreesRdR)
        {
            List<double> Matrice = new List<double>();
            ReseauPrivee RdN = this.ListRdN[nbrRdN];

            if (RdN.UtiliseMatriceEntrees)
                Matrice.AddRange(entreesRdR);

            for (int i = 0; i < RdN.ReseauxEntrees.Length; i++)
            {
                Matrice.AddRange(sorties[RdN.ReseauxEntrees[i]]);
            }

            return Matrice.ToArray();
        }
    }
}
