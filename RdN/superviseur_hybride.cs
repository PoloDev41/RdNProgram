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
    /// <summary>
    /// superviseur pour apprentissage hybride
    /// </summary>
    /// <remarks>ce superviseur modifie les sorties du réseau. Il est utilisé pour faire du classement alors qu'on ne connait le nombre de classe</remarks>
    public class Superviseur_hybride
    {
        /// <summary>
        /// coefficient pour signifier qu'une valeur de la matrice de sortie est correcte
        /// </summary>
        public double CoeffCorrect = 0.95f;

        /// <summary>
        /// coefficient pour signifier qu'une valeur peut être utilise pour l'apprentissage
        /// </summary>
        public double CoeffApprenti = 0.99f;

        private Superviseur ApprentissageSuper;

        private List<Echantillon> ListEchantillons;

        /// <summary>
        /// création d'un superviseur hybride
        /// </summary>
        /// <param name="reseau">reseau à modifier</param>
        public Superviseur_hybride(Reseau reseau)
        {
            this.ApprentissageSuper = new Superviseur(reseau);
            this.ListEchantillons = new List<Echantillon>();

            this.AjoutEchantillonsBase(reseau);
        }

        /// <summary>
        /// ajoute les echantillons de base à un reseau
        /// </summary>
        /// <param name="reseau">reseau</param>
        private void AjoutEchantillonsBase(Reseau reseau)
        {
            double[] Entrees = new double[reseau.NbrEntrees];
            for (int i = 0; i < Entrees.Length; i++)
            {
                Entrees[i] = 1;
            }
            double[] Sortie = new double[1];
            Sortie[0] = 1;

            this.ApprentissageSuper.AddEchantillons(Entrees, Sortie); //on ajoute un echantillon
            this.ListEchantillons.Add(new Echantillon()
            {
                Entrees = Entrees,
                Sortie = Sortie
            });
        }

        /// <summary>
        /// calcule la matrice de sortie du réseau
        /// </summary>
        /// <param name="entrees">matrice d'entrées</param>
        /// <returns>matrice d'entrées</returns>
        public double[] Calculer(double[] entrees)
        {
            double[] Sortie = this.ApprentissageSuper.Calculer(entrees);

            this.InjectionResultat(Sortie, entrees);

            return Sortie;
        }

        /// <summary>
        /// ajoute une categorie avec l'entrees données
        /// </summary>
        /// <param name="entrees">matrice d'entrées</param>
        public void AjouterCategorie(double[] entrees)
        {
            AjouterSortieReseau();

            //on ajoute la nouvelle categorie dans les échantillons à apprendre
            double[] newSortie = new double[this.GetNbrSorties() + 1];

            for (int i = 0; i < this.GetNbrSorties(); i++)
            {
                newSortie[i] = 0;
            }
            newSortie[this.GetNbrSorties()] = 1;

            this.ApprentissageSuper.AddEchantillons(entrees, newSortie);
            this.ListEchantillons.Add(new Echantillon()
            {
                Entrees = entrees,
                Sortie = newSortie
            });
        }

        /// <summary>
        /// analyse les résultats du réseau de neurone pour savoir si il peut utiliser les sorties calculées comme base d'un nouveau apprentissage
        /// </summary>
        /// <param name="sortie">sortie</param>
        /// <param name="entrees">entrées à partir duquel a été généré la sortie</param>
        private void InjectionResultat(double[] sortie, double[] entrees)
        {
            double[] ToutOuRien, Apprentiss;
            if (ConvertirToR(sortie, out ToutOuRien))
            {
                if (IsExisted(entrees, ToutOuRien) == false)
                {
                    if (this.ListEchantillons.Count == 1) //si il n'y a qu'un echantillon on ajoute une catégorie quand même
                    {
                        AjouterSortieReseau();

                        //on ajoute la nouvelle categorie dans les échantillons à apprendre
                        double[] newSortie = new double[sortie.Length + 1];

                        for (int i = 0; i < sortie.Length; i++)
                        {
                            newSortie[i] = 0;
                        }
                        newSortie[sortie.Length] = 1;

                        this.ApprentissageSuper.AddEchantillons(entrees, newSortie);
                        this.ListEchantillons.Add(new Echantillon()
                        {
                            Entrees = entrees,
                            Sortie = newSortie
                        });
                    }
                    else
                    {
                        if (ConvertirApprentissage(sortie, out Apprentiss))
                        {
                            this.ApprentissageSuper.AddEchantillons(entrees, Apprentiss);
                            this.ListEchantillons.Add(new Echantillon()
                            {
                                Entrees = entrees,
                                Sortie = Apprentiss
                            });
                        }
                    }
                }
            }
            else
            {
                AjouterSortieReseau();

                //on ajoute la nouvelle categorie dans les échantillons à apprendre
                double[] newSortie = new double[sortie.Length + 1];

                for (int i = 0; i < sortie.Length; i++)
                {
                    newSortie[i] = 0;
                }
                newSortie[sortie.Length] = 1;

                this.ApprentissageSuper.AddEchantillons(entrees, newSortie);
                this.ListEchantillons.Add(new Echantillon()
                {
                    Entrees = entrees,
                    Sortie = newSortie
                });
            }
        }

        /// <summary>
        /// converti la matrice de sortie en matrice pour l'apprentissage
        /// </summary>
        /// <param name="sortie">matrice calculé</param>
        /// <param name="Apprentiss">matrice pour l'apprentissage</param>
        /// <returns>true: la matrice peut être servit pour l'apprentissage</returns>
        private bool ConvertirApprentissage(double[] sortie, out double[] Apprentiss)
        {
            bool reponse = false;
            Apprentiss = new double[sortie.Length];
            for (int i = 0; i < sortie.Length; i++)
            {
                if (sortie[i] >= this.CoeffApprenti)
                {
                    Apprentiss[i] = 1;
                    reponse = true;
                }
                else if(sortie[i] >= this.CoeffCorrect)
                {
                    Apprentiss[i] = 0.5f;
                }
                else
                {
                    Apprentiss[i] = 0;
                }
            }

            return reponse;
        }

        /// <summary>
        /// compare l'échantillon avec ceux enregistrés
        /// </summary>
        /// <param name="entrees">matrice d'entrées</param>
        /// <param name="sorties">matrice de sortie</param>
        /// <returns>true: l'echantillon existe déjà</returns>
        private bool IsExisted(double[] entrees, double[] sorties)
        {
            bool entreesCorrect;
            bool sortieCorrect;
            foreach (Echantillon item in this.ListEchantillons)
            {
                sortieCorrect = entreesCorrect = true;

                for (int i = 0; i < item.Entrees.Length; i++)
                {
                    if(item.Entrees[i] != entrees[i])
                    {
                        entreesCorrect = false;
                        break;
                    }
                }

                if(entreesCorrect)
                {
                    for (int i = 0; i < item.Sortie.Length; i++)
                    {
                        if (item.Sortie[i] != sorties[i])
                        {
                            sortieCorrect = false;
                            break;
                        }
                    }

                    if (sortieCorrect)
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// ajoute une sortie au réseau de neurones et à tous les échantillons connus
        /// </summary>
        private void AjouterSortieReseau()
        {
            this.ApprentissageSuper.StopperApprentissage(); //on stop en premier pour laisser le temps au thread de gérer
            
            List<Echantillon> NewList = new List<Echantillon>();
            
            for (int i = 0; i < this.ListEchantillons.Count; i++)
            {
                NewList.Add(AjouterSortieEchantillon(this.ListEchantillons[i]));
            }

            this.ListEchantillons = NewList;

            Reseau reseau = this.ApprentissageSuper.GetReseau();
            reseau.AjouterSortie();

            this.ApprentissageSuper = new Superviseur(reseau);

            
            this.ApprentissageSuper.AddEchantillons(NewList);
        }

        /// <summary>
        /// sauvegarde le reseau de neurone et les echantillons
        /// </summary>
        /// <param name="pathReseau">chemin complet du reseau</param>
        /// <param name="pathEchantillon">chemin des echantillons</param>
        public void Sauvegarder(string pathReseau, string pathEchantillon)
        {
            Parser.SauvegarderReseau(this.ApprentissageSuper.GetReseau(), pathReseau);

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(pathEchantillon, FileMode.Create, FileAccess.Write, FileShare.None);
            formatter.Serialize(stream, this.ListEchantillons);
            stream.Close();
        }

        /// <summary>
        /// ajoute une sortie à l'échantillon
        /// </summary>
        /// <param name="old">ancien echantillon</param>
        /// <returns>nouvel echantillon</returns>
        private Echantillon AjouterSortieEchantillon(Echantillon old)
        {
            //on ajoute une sortie à la matrice de sortie
            double[] newSortie;
            newSortie = new double[old.Sortie.Length + 1];
            for (int j = 0; j < old.Sortie.Length; j++)
            {
                newSortie[j] = old.Sortie[j];
            }
            newSortie[old.Sortie.Length] = 0;

            //on recopie l'entrée
            double[] newEntree = new double[old.Entrees.Length];
            Array.Copy(old.Entrees, newEntree, old.Entrees.Length);

            return new Echantillon()
            {
                Entrees = newEntree,
                Sortie = newSortie
            };
        }

        /// <summary>
        /// convertit la matrice de flotant en matrice  tout ou rien suivant un coefficient
        /// </summary>
        /// <returns>true: la matrice possède des "1"</returns>
        private bool ConvertirToR(double[] entrees, out double[] sortie)
        {
            bool reponse = false;
            sortie = new double[entrees.Length];
            for (int i = 0; i < entrees.Length; i++)
            {
                if(entrees[i] >= this.CoeffCorrect)
                {
                    sortie[i] = 1;
                    reponse = true;
                }
                else
                {
                    sortie[i] = 0;
                }
            }

            return reponse;
        }

        /// <summary>
        /// Ajoute un echantillon dans le cas d'un apprentissage d'un synonyme (entrées différente mais sortie identique)
        /// </summary>
        /// <param name="entreesConnues">entrées connues</param>
        /// <param name="entreesNouvelles">la matrice d'entrées à apprendre</param>
        /// <returns>matrice de sortie</returns>
        public double[] ApprendreSynonyme(double[] entreesConnues, double[] entreesNouvelles)
        {
            double[] Sortie = this.ApprentissageSuper.Calculer(entreesConnues);

            double[] Apprentiss;
            ConvertirApprentissage(Sortie, out Apprentiss);

            this.ApprentissageSuper.AddEchantillons(entreesNouvelles, Apprentiss);

            return Sortie;
        }

        /// <summary>
        /// retourne le nombre de sortie que possède le reseau
        /// </summary>
        /// <returns></returns>
        public int GetNbrSorties()
        {
            return this.ApprentissageSuper.GetReseau().NbrSorties;
        }

        /// <summary>
        /// retourne l'erreur quadratique moyenne du reseau de neurone
        /// </summary>
        /// <returns>erreur quadratique moyenne</returns>
        public double GetErreurQuadratique()
        {
            return this.ApprentissageSuper.GetErreurQuadratique();
        }

        /// <summary>
        /// retourne le nombre total de neurone du reseau
        /// </summary>
        /// <returns>nombre de neurones</returns>
        public double GetNbrNeurones()
        {
            return this.ApprentissageSuper.GetNbrNeurones();
        }

        /// <summary>
        /// retourne le nombre de couche du reseau
        /// </summary>
        /// <returns>nombre de couche</returns>
        public double GetNbrCouches()
        {
            return this.ApprentissageSuper.GetNbrCouches();
        }

        /// <summary>
        /// charge le reseau de neurone est les echantillons
        /// </summary>
        /// <param name="pathReseau">chemin du reseau</param>
        /// <param name="pathEchantillons">chemin des echantillons</param>
        public void Charger(string pathReseau, string pathEchantillons)
        {
            this.ApprentissageSuper.StopperApprentissage();
            this.ApprentissageSuper = new Superviseur(Parser.ChargerReseau(pathReseau));

            IFormatter formatter = new BinaryFormatter();
            Stream stream = new FileStream(pathEchantillons, FileMode.Open, FileAccess.Read, FileShare.Read);
            this.ListEchantillons = (List<Echantillon>)formatter.Deserialize(stream);
            stream.Close();

            this.ApprentissageSuper.AddEchantillons(this.ListEchantillons);
        }

        /// <summary>
        /// arrete l'apprentissage
        /// </summary>
        public void StopApprentissage()
        {
            this.ApprentissageSuper.StopperApprentissage();
        }
    }
}
