using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RdN
{

    public enum ModificationType
    {
        NONE,
        AJOUT_COUCHE
    }

    /// <summary>
    /// classe servant à modifier le réseau
    /// </summary>
    [Serializable]
    public class ModificationReseau
    {
        /// <summary>
        /// type de modification du réseau
        /// </summary>
        public ModificationType Modification { get; set; }
    }

    /// <summary>
    /// structure pour les échantillons d'apprentissage
    /// </summary>
    [Serializable]
    public struct Echantillon
    {
        /// <summary>
        /// entrées pour l'échantillon
        /// </summary>
        public double[] Entrees;
        /// <summary>
        /// sortie de l'échantillon
        /// </summary>
        public double[] Sortie;
    }

    /// <summary>
    /// superviseur pour le réseau neuronal
    /// </summary>
    [Serializable]
    public class Superviseur
    {
        /// <summary>
        /// list des indices à supprimer dans les échantillons d'apprentissage
        /// </summary>
        private List<int> IndexASupprimer { get; set; }

        /// <summary>
        /// mutex utilise pour le boolean autorisant la reconstruction par la génétique
        /// </summary>
        private Mutex MutexAutoriseReconstruction { get; set; }

        /// <summary>
        /// true: la modification du RdN par reconstruction génétique est autorisé
        /// </summary>
        private bool AutoriseReconstruction { get; set; }

        /// <summary>
        /// mutex pour la liste des indexes à supprimer
        /// </summary>
        private Mutex MutexListSuppression { get; set; }

        /// <summary>
        /// réseau neuronal
        /// </summary>
        private Reseau ReseauNeuronal { get; set; }

        /// <summary>
        /// thread faisant l'apprentissage en background
        /// </summary>
        private Thread SystemeApprentissage { get; set; }

        /// <summary>
        /// true: l'apprentissage est effectué en background
        /// </summary>
        private bool ContinuerApprentissage { get; set; }

        /// <summary>
        /// erreur max à atteindre dans l'apprentissage
        /// </summary>
        public double ErreurMax = .0000001f;

        /// <summary>
        /// liste des échantillons à apprendre
        /// </summary>
        private List<Echantillon> ListEchantillons { get; set; }

        /// <summary>
        /// list des échantillons en attente pour l'apprentissage
        /// </summary>
        private List<Echantillon> ListEnAttente { get; set; }

        /// <summary>
        /// mutex sur la liste des échantillons en attente à être copié
        /// </summary>
        private Mutex MutexListEchantillon { get; set; }

        /// <summary>
        /// mutex sur le réseau de neurone
        /// </summary>
        private Mutex MutexReseau { get; set; }

        /// <summary>
        /// mutex pour si une demande de modification du reseau à lieu
        /// </summary>
        private Mutex MutexModificationReseau { get; set; }

        /// <summary>
        /// commande de modification du réseau
        /// </summary>
        private ModificationReseau Modification { get; set; }

        /// <summary>
        /// autorise au non la reconstruction du RdN par la genetique
        /// </summary>
        /// <param name="Autorisation"></param>
        public void AutorisationReconstruction(bool Autorisation)
        {
            this.MutexAutoriseReconstruction.WaitOne();
            this.AutoriseReconstruction = Autorisation;
            this.MutexAutoriseReconstruction.ReleaseMutex();
        }

        /// <summary>
        /// crée un superviseur de réseau neuronal
        /// </summary>
        /// <param name="reseau">réseau neuronal à superviser</param>
        public Superviseur(Reseau reseau)
        {
            this.ReseauNeuronal = reseau;
            ContinuerApprentissage = true;
            this.ListEchantillons = new List<Echantillon>();
            this.ListEnAttente = new List<Echantillon>();
            MutexListEchantillon = new Mutex();
            this.MutexReseau = new Mutex();
            this.MutexListSuppression = new Mutex();
            this.IndexASupprimer = new List<int>();
            this.MutexAutoriseReconstruction = new Mutex();
            this.AutoriseReconstruction = true;
            this.MutexModificationReseau = new Mutex();
            this.Modification = new ModificationReseau();
            this.Modification.Modification = ModificationType.NONE;
        }

        /// <summary>
        /// ajoute un index à la liste des indexes à supprimer
        /// </summary>
        /// <param name="index">numéro de l'index</param>
        public void AjouteIndexSupprimer(int index)
        {
            this.MutexListSuppression.WaitOne();
            this.IndexASupprimer.Add(index);
            this.MutexListSuppression.ReleaseMutex();
        }

        /// <summary>
        /// réalise une modification au réseau de neurone
        /// </summary>
        /// <param name="modif">modification</param>
        public void AjoutModification(ModificationType modif)
        {
            this.MutexModificationReseau.WaitOne();
            this.Modification.Modification = modif;
            this.MutexModificationReseau.ReleaseMutex();
            //On relance l'apprentissage depuis le début
            this.StopperApprentissage();
            this.LancerApprentissage();
        }

        /// <summary>
        /// réalise la modification demandé par l'utilisateur
        /// </summary>
        private void RealiserModification()
        {
            ModificationReseau modif_ = new ModificationReseau();
            this.MutexModificationReseau.WaitOne();
            modif_.Modification = this.Modification.Modification;
            this.Modification.Modification = ModificationType.NONE;
            this.MutexModificationReseau.ReleaseMutex();

            switch (modif_.Modification)
            {
                case ModificationType.NONE:
                    break;
                case ModificationType.AJOUT_COUCHE:
                    this.MutexReseau.WaitOne();
                    this.ReseauNeuronal.AjoutCouche();
                    
                    this.MutexReseau.ReleaseMutex();
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// supprime tous les échantillons en cours et stop l'apprentissage
        /// </summary>
        public void EcraseToutEchantillon()
        {
            this.StopperApprentissage();
            while (this.SystemeApprentissage.IsAlive); //on attend tant que le thread d'apprentissage n'est pas mort

            this.IndexASupprimer = new List<int>();
            this.ListEnAttente = new List<Echantillon>();
            this.ListEchantillons = new List<Echantillon>();
        }

        /// <summary>
        /// trie le les index par ordre décroissant
        /// </summary>
        /// <param name="x">x</param>
        /// <param name="y">y</param>
        /// <returns>le meilleur</returns>
        static private int SortIndex(int x, int y)
        {
            if (x < y)
                return 1;
            else if (x > y)
                return -1;
            else return 0;
        }

        /// <summary>
        /// supprime des échantillons à la liste des échantillons
        /// </summary>
        private void SuppressionEchantillonBackground()
        {
            this.MutexListSuppression.WaitOne();
            this.IndexASupprimer.Sort(SortIndex);
            for (int i = this.IndexASupprimer.Count - 1; i >= 0; i--)
            {
                if (this.IndexASupprimer[i] < this.ListEchantillons.Count)
                {
                    this.ListEchantillons.RemoveAt(this.IndexASupprimer[i]);
                }
            }
            this.IndexASupprimer.Clear();
            this.MutexListSuppression.ReleaseMutex();
        }

        /// <summary>
        /// sauvegarde les échantillons dans un fichier
        /// </summary>
        /// <param name="path">chemin complet des échantillons</param>
        public void SauvegarderEchantillons(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this.ListEchantillons);
            stream.Close();
        }

        /// <summary>
        /// charge les échantillons depuis un fichier
        /// </summary>
        /// <param name="path">chemin complet des échantillons</param>
        public void ChargerEchantillons(string path)
        {
            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.ListEchantillons = (List<Echantillon>)formatter.Deserialize(stream);
            stream.Close();
        }

        /// <summary>
        /// Ajoute les echantillons à la liste d'apprentissage pour le thread
        /// </summary>
        /// <param name="copieReseau">reseau servant à l'apprentissage en background</param>
        private void AjoutEchantillonBackGround(ref Reseau copieReseau)
        {
            double[][] entrees, sorties;
            this.MutexListEchantillon.WaitOne();
            if (this.ListEnAttente.Count > 0)
            {
                this.ListEchantillons.AddRange(this.ListEnAttente);
                this.ListEnAttente.Clear();
                entrees = this.GetEchantillonsEntrees();
                sorties = this.GetEchantillonsSorties();
                Apprentissage.LearnRetropropagation(ref copieReseau, 1, this.ErreurMax, entrees, sorties);

                this.MutexReseau.WaitOne();
                //si le nouveau est mieux, autant le copier
                if (this.ReseauNeuronal.ErreurQuadratiqueMoyenne > copieReseau.ErreurQuadratiqueMoyenne)
                {
                    this.ReseauNeuronal = copieReseau.Clone();
                }
                else
                    this.ReseauNeuronal.ErreurQuadratiqueMoyenne = copieReseau.ErreurQuadratiqueMoyenne;
                this.MutexReseau.ReleaseMutex();

            }
            this.MutexListEchantillon.ReleaseMutex();
        }

        /// <summary>
        /// Compare le reseau apprenant au réseau utilisé et le recopie si il est meilleur
        /// </summary>
        /// <param name="copie">réseau apprenant</param>
        private void CompareEtCopie(ref Reseau copie)
        {
            this.MutexReseau.WaitOne();
            if (this.ReseauNeuronal.ErreurQuadratiqueMoyenne > copie.ErreurQuadratiqueMoyenne)
            {
                this.ReseauNeuronal = copie.Clone();
            }
            this.MutexReseau.ReleaseMutex();
        }

        /// <summary>
        /// routine du thread de background
        /// </summary>
        private void RoutineBackground()
        {
            try
            {
                if (this.ListEchantillons.Count == 0 && this.ListEnAttente.Count == 0)
                    return; //pas d'échantillons pour l'apprentissage

                //on copie le réseau de neurone
                this.MutexReseau.WaitOne();
                this.MutexModificationReseau.WaitOne();
                if (this.Modification.Modification != ModificationType.NONE)
                {
                    this.RealiserModification();
                    this.ReseauNeuronal.ErreurQuadratiqueMoyenne = 1; //on met "1" pour supprimer l'ancienne erreur
                }
                this.MutexModificationReseau.ReleaseMutex();
                Reseau copieReseau = this.ReseauNeuronal.Clone();
                this.MutexReseau.ReleaseMutex();

                int nbrEssai = 0;
                bool suppressionGenetique;

                this.MutexAutoriseReconstruction.WaitOne();
                bool construction = this.AutoriseReconstruction;
                this.MutexAutoriseReconstruction.ReleaseMutex();

                do
                {
    
                    //on ajoute les échantillons en attente
                    AjoutEchantillonBackGround(ref copieReseau);
                    SuppressionEchantillonBackground();

                    nbrEssai++;
                    if (nbrEssai % 10 == 0) //tous les 10 essais on voit si ça vaut le coup de supprimer qq neurones
                        suppressionGenetique = true;
                    else
                        suppressionGenetique = false;

                    try
                    {
                        if (construction)
                        {
                            Apprentissage.LearnConstruction(ref copieReseau, this.ErreurMax, (this.ListEchantillons.Count),
                                                            this.GetEchantillonsEntrees(), this.GetEchantillonsSorties(), suppressionGenetique);
                        }
                        else
                        {
                            Apprentissage.LearnRetropropagation(ref copieReseau, this.ListEchantillons.Count,
                                                                this.ErreurMax, this.GetEchantillonsEntrees(), this.GetEchantillonsSorties());
                        }
                    }
                    catch (ThreadAbortException)
                    {
                    }

                    CompareEtCopie(ref copieReseau);
                    
                    if(copieReseau.ErreurQuadratiqueMoyenne < (this.ErreurMax * 1.1))
                    {
                        this.AutorisationReconstruction(false);
                    }
                    else
                    {
                        this.AutorisationReconstruction(true);
                    }

                } while (this.ContinuerApprentissage && this.ErreurMax < copieReseau.ErreurQuadratiqueMoyenne);
                this.ContinuerApprentissage = false;
            }
            catch(ThreadAbortException)
            {
            }
        }

        /// <summary>
        /// lance l'apprentissage en background
        /// </summary>
        public void LancerApprentissage()
        {
            //si pas en vie on lance, sinon, on lance pas :)
            if (this.SystemeApprentissage == null ||
                this.SystemeApprentissage.IsAlive == false)
            {
                this.ContinuerApprentissage = true;
                this.SystemeApprentissage = new Thread(RoutineBackground);
                this.SystemeApprentissage.Start();
            }
        }

        /// <summary>
        /// stop l'apprentissage en background au prochain cycle d'apprentissage
        /// </summary>
        public void StopperApprentissage()
        {
            if (this.SystemeApprentissage == null || 
                this.SystemeApprentissage.IsAlive == true)
            {
                this.MutexListEchantillon.WaitOne();
                this.MutexListSuppression.WaitOne();
                this.MutexReseau.WaitOne();

                this.SystemeApprentissage.Abort();
                this.ContinuerApprentissage = false;
                
                this.MutexReseau.ReleaseMutex();
                this.MutexListSuppression.ReleaseMutex();
                this.MutexListEchantillon.ReleaseMutex();
            }
        }

        /// <summary>
        /// lance le calcul pour le réseau neuronal
        /// </summary>
        /// <param name="entrees">matrice d'entrée</param>
        public double[] Calculer(double[] entrees)
        {
            double[] sortie;
            this.MutexReseau.WaitOne();
            sortie = this.ReseauNeuronal.Calcul(entrees);
            this.MutexReseau.ReleaseMutex();

            return sortie;
        }

        /// <summary>
        /// ajoute des échantillons pour l'apprentissage
        /// </summary>
        /// <param name="entrees">entrees</param>
        /// <param name="sortie">sorties</param>
        public void AddEchantillons(double[][] entrees, double[][] sortie)
        {
            if (entrees.Length != sortie.Length)
                throw new Exception("nombre d'entrées différent du nombre de sortie");

            double[] copieEntree;
            double[] copieSortie;
            Echantillon tmp;
            List<Echantillon> list = new List<Echantillon>();
            for (int i = 0; i < entrees.Length; i++)
            {
                copieEntree = new double[entrees[i].Length];
                copieSortie = new double[sortie[i].Length];

                for (int j = 0; j < copieEntree.Length; j++)
                {
                    copieEntree[j] = entrees[i][j];
                }
                for (int j = 0; j < copieSortie.Length; j++)
                {
                    copieSortie[j] = sortie[i][j];
                }
                tmp = new Echantillon();
                tmp.Entrees = copieEntree;
                tmp.Sortie = copieSortie;

                list.Add(tmp);
            }

            this.MutexListEchantillon.WaitOne();
            this.ListEnAttente.AddRange(list);
            this.MutexListEchantillon.ReleaseMutex();

            this.LancerApprentissage();
        }

        /// <summary>
        /// retourne le réseau de neuronal optimal
        /// </summary>
        /// <returns>réseau optimal</returns>
        public Reseau GetReseau()
        {
            Reseau tmp;
            this.MutexReseau.WaitOne();
            tmp = this.ReseauNeuronal.Clone();
            this.MutexReseau.ReleaseMutex();
            return tmp;
        }

        /// <summary>
        /// ajoute un échantillon à la liste des échantillons d'apprentissage
        /// </summary>
        /// <param name="entrees">entrées</param>
        /// <param name="sortie">sorties</param>
        public void AddEchantillons(double[] entrees, double[] sortie)
        {
            Echantillon tmp;
            tmp = new Echantillon();
            tmp.Entrees = entrees;
            tmp.Sortie = sortie;
            this.MutexListEchantillon.WaitOne();
            this.ListEnAttente.Add(tmp);
            this.MutexListEchantillon.ReleaseMutex();
            
            this.LancerApprentissage();
        }

        /// <summary>
        /// ajoute des echantillons à la liste des echantillons d'apprentissage et lance l'apprentissage
        /// </summary>
        /// <param name="echantillons">list d'échantillons</param>
        public void AddEchantillons(List<Echantillon> echantillons)
        {
            this.MutexListEchantillon.WaitOne();
            this.ListEnAttente.AddRange(echantillons);
            this.MutexListEchantillon.ReleaseMutex();

            this.LancerApprentissage();
        }

        /// <summary>
        /// retourne toutes les entrées des échantillons enregistrées
        /// </summary>
        /// <returns>matrice d'entrées des échantillons</returns>
        private double[][] GetEchantillonsEntrees()
        {
            double[][] echantillons = new double[this.ListEchantillons.Count][];
            for (int i = 0; i < echantillons.Length; i++)
            {
                echantillons[i] = this.ListEchantillons[i].Entrees;
            }
            return echantillons;
        }

        /// <summary>
        /// retourne toutes les sorties des échantillons enregistrées
        /// </summary>
        /// <returns>matrice des sorties des échantillons</returns>
        private double[][] GetEchantillonsSorties()
        {
            double[][] echantillons = new double[this.ListEchantillons.Count][];
            for (int i = 0; i < echantillons.Length; i++)
            {
                echantillons[i] = this.ListEchantillons[i].Sortie;
            }
            return echantillons;
        }

        /// <summary>
        /// retourne l'erreur quadratique du réseau de neurone
        /// </summary>
        /// <returns>erreur quadratique</returns>
        public double GetErreurQuadratique()
        {
            double erreur;
            this.MutexReseau.WaitOne();
            erreur = this.ReseauNeuronal.ErreurQuadratiqueMoyenne;
            this.MutexReseau.ReleaseMutex();
            return erreur;
        }

        /// <summary>
        /// retourne le nombre de neurones du réseau
        /// </summary>
        /// <returns>nombre de neurones</returns>
        public int GetNbrNeurones()
        {
            int nbr;
            this.MutexReseau.WaitOne();
            nbr = this.ReseauNeuronal.nbrNeurone();
            this.MutexReseau.ReleaseMutex();
            return nbr;
        }

        /// <summary>
        /// retourne le nombre de couches dans le réseau
        /// </summary>
        /// <returns>nombre de couche</returns>
        public int GetNbrCouches()
        {
            return this.ReseauNeuronal.ReseauNeurones.Length;
        }
    }
}
