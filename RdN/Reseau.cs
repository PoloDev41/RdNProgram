using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdN
{
    /// <summary>
    /// cree un nouveau reseau de neurone
    /// </summary>
    [Serializable]
    public class Reseau
    {
        /// <summary>
        /// coefficien pour l'inertie
        /// </summary>
        public double CoeffInertie = .15f;

        /// <summary>
        /// coefficien d'apprentissage
        /// </summary>
        public double CoeffApprentissage = .75f;

        /// <summary>
        /// couches du reseau
        /// </summary>
        public Couche[] ReseauNeurones { get; set; }

        /// <summary>
        /// sortie du reseau
        /// </summary>
        public double[] Sortie { get; set; }

        /// <summary>
        /// erreur quadratique lors du dernier apprentissage
        /// </summary>
        public double ErreurQuadratique { get; set; }

        /// <summary>
        /// erreur quadratique moyenne
        /// </summary>
        public double ErreurQuadratiqueMoyenne { get; set; }

        /// <summary>
        /// matrice d'erreur lors du dernier apprentissage
        /// </summary>
        public double[] Erreur { get; set; }

        /// <summary>
        /// nombre d'entrées du reseau de neurone
        /// </summary>
        public int NbrEntrees { get; set; }

        /// <summary>
        /// retourne le nombre de sortie du reseau de neurone
        /// </summary>
        public int NbrSorties { get { return this.ReseauNeurones[this.ReseauNeurones.Length - 1].CoucheNeurones.Length; } }

        /// <summary>
        /// cree un nouveau réseau
        /// </summary>
        /// <param name="nbrNeurones">nombre de neurone par couche</param>
        /// <param name="nbrEntree">nombre d'entree du reseau</param>
        public Reseau(int[] nbrNeurones, int nbrEntree)
        {
            this.NbrEntrees = nbrEntree;
            this.Erreur = new double[nbrNeurones[nbrNeurones.Length - 1]];
            this.ReseauNeurones = new Couche[nbrNeurones.Length];
            this.ReseauNeurones[0] = new Couche(nbrNeurones[0], nbrEntree);
            this.Sortie = new double[this.Erreur.Length];
            this.ErreurQuadratique = double.MaxValue;
            ErreurQuadratiqueMoyenne = double.MaxValue;
            for (int i = 1; i < this.ReseauNeurones.Length; i++)
			{
                this.ReseauNeurones[i] = new Couche(nbrNeurones[i], nbrNeurones[i - 1]);
			}
        }

        /// <summary>
        /// crée un réseau de neurone vide
        /// </summary>
        private Reseau()
        {

        }

        /// <summary>
        /// calcul la sortie du reseau
        /// </summary>
        /// <param name="entrees">matrice d'entrees du reseau</param>
        /// <returns>matrice de sortie</returns>
        public double[] Calcul(double[] entrees)
        {
            this.ReseauNeurones[0].CalculSortie(entrees);
            double[] sortie = this.ReseauNeurones[0].SortieCouche();
            for (int i = 1; i < this.ReseauNeurones.Length; i++)
            {
                this.ReseauNeurones[i].CalculSortie(sortie);
                sortie = this.ReseauNeurones[i].SortieCouche();
            }
            this.Sortie = sortie;
            return sortie;
        }

        /// <summary>
        /// Calcul la rétropropagation (calculé le vecteur d'erreur avant)
        /// </summary>
        /// <param name="entrees">entrees du reseau</param>
        public void Apprentissage(double[] entrees)
        {
            this.CalculPropagationValue();
            this.MiseAJourPoids(entrees);
        }

        /// <summary>
        /// calcul la valeur de retropropagation pour tous les neurones
        /// </summary>
        private void CalculPropagationValue()
        {
            int nbrCouche = this.ReseauNeurones.Length -1;
            //on commence par la dernière couche
            for (int i = 0; i < this.ReseauNeurones[nbrCouche].CoucheNeurones.Length; i++)
            {
                this.ReseauNeurones[nbrCouche].CoucheNeurones[i].RetroValue =
                            this.ReseauNeurones[nbrCouche].CoucheNeurones[i].GetDeriveSortie() *
                            this.Erreur[i];
            }

            double sommeEntrees = 0f;
            for (nbrCouche--; nbrCouche >= 0; nbrCouche--)
            {
                for (int i = 0; i < this.ReseauNeurones[nbrCouche].CoucheNeurones.Length; i++)
                {
                    sommeEntrees = this.ReseauNeurones[nbrCouche + 1].SommeSortie(i);
                    this.ReseauNeurones[nbrCouche].CoucheNeurones[i].RetroValue =
                                sommeEntrees *
                                this.ReseauNeurones[nbrCouche].CoucheNeurones[i].GetDeriveSortie();
                }
            }
        }

        /// <summary>
        /// met à jour tous les poids
        /// </summary>
        /// <param name="entrees">matrice d'entrees pour la mise à jour</param>
        private void MiseAJourPoids(double[] entrees)
        {
            double[] coucheEntre;
            for (int i = 0; i < this.ReseauNeurones.Length; i++)
            {
                if(i == 0) coucheEntre = entrees;
                else coucheEntre = this.ReseauNeurones[i - 1].SortieCouche();

                for (int j = 0; j < this.ReseauNeurones[i].CoucheNeurones.Length; j++)
                {
                    this.ReseauNeurones[i].CoucheNeurones[j].CorrectionPoids(coucheEntre, this.CoeffApprentissage, this.CoeffInertie);
                }
            }
        }


        /// <summary>
        /// donne la sortie d'une couche donnée (faire un calcul préalable)
        /// </summary>
        /// <param name="numCouche">numero de la couche</param>
        /// <returns>matrice de sortie de la couche</returns>
        public double[] SortieCouche(int numCouche)
        {
            if (numCouche >= this.ReseauNeurones.Length)
                throw new Exception("numero de la couche inexistant");
            double[] sortie = new double[this.ReseauNeurones[numCouche].CoucheNeurones.Length];

            for (int i = 0; i < sortie.Length; i++)
            {
                sortie[i] = this.ReseauNeurones[numCouche].CoucheNeurones[i].Sortie;
            }

            return sortie;
        }

        /// <summary>
        /// clone le réseau
        /// </summary>
        /// <returns>clone du réseau</returns>
        public Reseau Clone()
        {
            Reseau clone = new Reseau();
            clone.NbrEntrees = this.NbrEntrees;
            clone.CoeffApprentissage = this.CoeffApprentissage;
            clone.CoeffInertie = this.CoeffInertie;
            clone.Erreur = new double[this.Erreur.Length];
            clone.Sortie = new double[this.Sortie.Length];
            for (int i = 0; i < clone.Erreur.Length; i++)
            {
                clone.Sortie[i] = this.Sortie[i];
                clone.Erreur[i] = this.Erreur[i];
            }
            clone.ErreurQuadratique = this.ErreurQuadratique;

            clone.ReseauNeurones = new Couche[this.ReseauNeurones.Length];
            for (int i = 0; i < clone.ReseauNeurones.Length; i++)
            {
                clone.ReseauNeurones[i] = this.ReseauNeurones[i].Clone();
            }
            clone.ErreurQuadratiqueMoyenne = this.ErreurQuadratiqueMoyenne;
            return clone;
        }

        /// <summary>
        /// ajoute un neurone au réseau
        /// </summary>
        public void AjoutNeurone()
        {
            if (this.ReseauNeurones.Length > 1)
            {
                Random gene = new Random();
                int coucheCible = gene.Next(0, this.ReseauNeurones.Length - 1);
                this.ReseauNeurones[coucheCible].AjoutNeurone(this.ReseauNeurones[coucheCible].CoucheNeurones[0].TypeFctActivation);
                this.ReseauNeurones[coucheCible + 1].AjoutEntree();
            }
            else
            {
                //pas d'ajout de neurone sinon la sortie va changer
                //Donc une couche en plus
                AjoutCouche();
            }
        }

        /// <summary>
        /// ajoute une couche au réseau
        /// </summary>
        /// <param name="fctAction">fonction d'activation de la couche</param>
        public void AjoutCouche(FctActivation_Type fctAction = FctActivation_Type.SIGMOIDE)
        {
            Random gene = new Random();
            int inser = gene.Next(0, this.ReseauNeurones.Length); //on veut pas insérer à la 1ère ou dernier couche

            //on cherche le nombre de neurone dans la future couche
            int min = int.MaxValue;
            int max = 0;
            for (int i = 0; i < this.ReseauNeurones.Length; i++)
            {
                if (this.ReseauNeurones[i].CoucheNeurones.Length > max)
                    max = this.ReseauNeurones[i].CoucheNeurones.Length;
                if (this.ReseauNeurones[i].CoucheNeurones.Length < min)
                    min = this.ReseauNeurones[i].CoucheNeurones.Length;
            }
            int nbrNeurone = gene.Next(Math.Max(1, min-1), max+2); //+2 car c'est exclusif pour la valeur max
            Couche couche;
            int difference;
            if (inser != 0)
            {
                couche = new Couche(nbrNeurone, this.ReseauNeurones[inser - 1].CoucheNeurones.Length, fctAction);
                difference = nbrNeurone - this.ReseauNeurones[inser - 1].CoucheNeurones.Length;
            }
            else
            {
                couche = new Couche(nbrNeurone, this.ReseauNeurones[0].CoucheNeurones[0].Poids.Length, fctAction);
                difference = nbrNeurone - this.ReseauNeurones[0].CoucheNeurones[0].Poids.Length;
            }
            
            
            if(difference > 0) // si il y a plus d'entrées
            {
                for (; difference > 0; difference--)
                {
                    this.ReseauNeurones[inser].AjoutEntree();
                }
            }
            else if(difference < 0) //si y en a moins
            {
                for (; difference < 0; difference++)
                {
                    this.ReseauNeurones[inser].SupprimerEntree();
                }
            }

            List<Couche> list = this.ReseauNeurones.ToList();
            list.Insert(inser, couche);
            this.ReseauNeurones = list.ToArray();
        }

        /// <summary>
        /// retourne le nombre de neurone total du réseau
        /// </summary>
        /// <returns>nombre de neurone du réseau</returns>
        public int nbrNeurone()
        {
            int nbr = 0;
            for (int i = 0; i < this.ReseauNeurones.Length; i++)
            {
                nbr += this.ReseauNeurones[i].nbrNeurone();
            }

            return nbr;
        }

        /// <summary>
        /// on supprime un neurone
        /// </summary>
        public void SupprimerNeurone()
        {
            Random gene = new Random();
            int numCouche = gene.Next(0, this.ReseauNeurones.Length - 1);
            if (this.ReseauNeurones[numCouche].CoucheNeurones.Length > 1)
            {
                int numNeurone = gene.Next(0, this.ReseauNeurones[numCouche].CoucheNeurones.Length);
                this.SupprimerNeurone(numCouche, numNeurone);
            }
            else
            {
                //y a une couche avec un neurone.... autant essayer de supprimer une couche
                this.SupprimerCouche();
            }
        }

        /// <summary>
        /// supprime un neurone spécific dans le réseau
        /// </summary>
        /// <param name="numCouche">numéro de la couche</param>
        /// <param name="numNeurone">numéro du neurone</param>
        public void SupprimerNeurone(int numCouche, int numNeurone)
        {
            this.ReseauNeurones[numCouche].SupprimerNeurone(numNeurone);
            this.ReseauNeurones[numCouche + 1].SupprimerEntree(numNeurone);
        }

        /// <summary>
        /// supprime une couche
        /// </summary>
        public void SupprimerCouche()
        {
            if (this.ReseauNeurones.Length == 1)
                return; //on fait rien si il y a qu'une couche

            Random gene = new Random();
            int numCouche = gene.Next(0, this.ReseauNeurones.Length - 1); // on supprime pas la dernière!

            int nombreEntree;
            if(numCouche == 0) //si on compte supprimer la première, les entrées de la future 1ère couche sont les entrées du réseau
                nombreEntree = this.ReseauNeurones[0].CoucheNeurones[0].Poids.Length;
            else //sinon les futures entrées de la couche qui suit celle supprimer sont les nombre de la couche n-1
                nombreEntree = this.ReseauNeurones[numCouche-1].CoucheNeurones.Length;

            int difference = nombreEntree - this.ReseauNeurones[numCouche].CoucheNeurones.Length;

            if (difference > 0) // si il y a plus d'entrées
            {
                for (; difference > 0; difference--)
                {
                    this.ReseauNeurones[numCouche +1].AjoutEntree();
                }
            }
            else if (difference < 0) //si y en a moins
            {
                for (; difference < 0; difference++)
                {
                    this.ReseauNeurones[numCouche +1].SupprimerEntree();
                }
            }

            List<Couche> list = this.ReseauNeurones.ToList();
            list.RemoveAt(numCouche);
            this.ReseauNeurones = list.ToArray();
        }

        /// <summary>
        /// supprime le neurone aillant le moins de poids dans le réseau
        /// </summary>
        public void SupprimerPetitNeurone()
        {
            int numCouche = -1;
            int numNeurone = -1;
            double SommePoidsMin = double.MaxValue;
            for (int i = 0; i < this.ReseauNeurones.Length-1; i++) //on ne regarde pas la dernière couche
            {
                if (this.ReseauNeurones[i].CoucheNeurones.Length > 1)
                {
                    for (int j = 0; j < this.ReseauNeurones[i].CoucheNeurones.Length; j++)
                    {
                        if (this.ReseauNeurones[i].CoucheNeurones[j].GetSommePoids() < SommePoidsMin)
                        {
                            numCouche = i;
                            numNeurone = j;
                        }
                    }
                }
                else
                {
                    //on fait pas la couche si il y a qu'un neurone dans cette couche
                }
            }

            if(numNeurone != -1 && numCouche != -1) //au cas ou, si problème
                this.SupprimerNeurone(numCouche, numNeurone);
        }

        /// <summary>
        /// supprime le neurone qui a le moins d'importance dans le réseau
        /// </summary>
        public void SupprimerNeuroneNegligeable()
        {
            int numCouche = -1;
            int numNeurone = -1;
            double SommePoidsMin = double.MaxValue;
            for (int i = 0; i < this.ReseauNeurones.Length - 1; i++) //on ne regarde pas la dernière couche
            {
                if (this.ReseauNeurones[i].CoucheNeurones.Length > 1)
                {
                    for (int j = 0; j < this.ReseauNeurones[i].CoucheNeurones.Length; j++)
                    {
                        if (Math.Abs(this.ReseauNeurones[i+1].SommePoidsEntree(j)) < SommePoidsMin)
                        {
                            numCouche = i;
                            numNeurone = j;
                        }
                    }
                }
                else
                {
                    //on fait pas la couche si il y a qu'un neurone dans cette couche
                }
            }

            if (numNeurone != -1 && numCouche != -1) //au cas ou, si problème
                this.SupprimerNeurone(numCouche, numNeurone);
        }


        /// <summary>
        /// change la fonction d'activation d'une couche
        /// </summary>
        /// <param name="fctActivation_Type">fonction d'activation</param>
        public void ChangerFonctionActivation(FctActivation_Type fctActivation_Type)
        {
            Random gene = new Random();
            int numCouche = gene.Next(0, this.ReseauNeurones.Length);
            this.ReseauNeurones[numCouche].ChangerFonctionActivation(fctActivation_Type);
        }

        /// <summary>
        /// change la fonction d'activation d'un neurone
        /// </summary>
        /// <param name="fctActivation_Type">nouvelle fonction d'activation</param>
        public void ChangerFonctionActivationNeurone(FctActivation_Type fctActivation_Type)
        {
            int numCouche = -1;
            int numNeurone = -1;
            Random gene = new Random();
            numCouche = gene.Next(0, this.ReseauNeurones.Length);
            numNeurone = gene.Next(0, this.ReseauNeurones[numCouche].CoucheNeurones.Length);
            this.ReseauNeurones[numCouche].CoucheNeurones[numNeurone].SetFonctionActivation(fctActivation_Type);
        }

        /// <summary>
        /// rajoute un neurone de sortie au réseau
        /// </summary>
        public void AjouterSortie()
        {
            this.ReseauNeurones[this.ReseauNeurones.Length - 1].AjoutNeurone(Neurone.RandomFctActivation());
            
        }
    }
}
