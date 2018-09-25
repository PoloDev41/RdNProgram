using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdN
{
    /// <summary>
    /// couche d'un reseau de neurone
    /// </summary>
    [Serializable]
    public class Couche
    {
        /// <summary>
        /// couche de neurone
        /// </summary>
        public Neurone[] CoucheNeurones { get; set; }

        /// <summary>
        /// cree une nouvelle couche dans le réseau
        /// </summary>
        /// <param name="nbrNeurone">nombre de neurone dans la couche</param>
        /// <param name="nbrEntrees">nombre d'entrées par neurone</param>
        /// <param name="fctActivation">fonction d'activation du neurone</param>
        public Couche(int nbrNeurone, int nbrEntrees, FctActivation_Type fctActivation = FctActivation_Type.TANGENTE_HYPER)
        {
            this.CoucheNeurones = new Neurone[nbrNeurone];
            for (int i = 0; i < nbrNeurone; i++)
            {
                this.CoucheNeurones[i] = new Neurone(nbrEntrees, fctActivation);
            }
        }

        /// <summary>
        /// crée une couche vide
        /// </summary>
        private Couche()
        {

        }

        /// <summary>
        /// retourne la matrice de sortie de la couche
        /// </summary>
        /// <returns>matrice de sortie</returns>
        public double[] SortieCouche()
        {
            double[] sortie = new double[this.CoucheNeurones.Length];
            for (int i = 0; i < sortie.Length; i++)
            {
                sortie[i] = this.CoucheNeurones[i].Sortie;
            }
            return sortie;
        }

        /// <summary>
        /// calcul la matrice de sortie de la couche
        /// </summary>
        /// <param name="entrees">entrees</param>
        public void CalculSortie(double[] entrees)
        {
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                this.CoucheNeurones[i].CalculSortie(entrees);
            }
        }

        /// <summary>
        /// retourne la somme des RetroValue pondéré par leur poids de l'entree données
        /// </summary>
        /// <param name="numEntree">numéro de l'entrée</param>
        /// <returns>somme</returns>
        public double SommeSortie(int numEntree)
        {
            double somme = 0f;
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                somme = this.CoucheNeurones[i].RetroValue * this.CoucheNeurones[i].Poids[numEntree];
            }
            return somme;
        }

        /// <summary>
        /// retourne le clone de la couche
        /// </summary>
        /// <returns>clone</returns>
        public Couche Clone()
        {
            Couche clone = new Couche();
            clone.CoucheNeurones = new Neurone[this.CoucheNeurones.Length];
            for (int i = 0; i < clone.CoucheNeurones.Length; i++)
            {
                clone.CoucheNeurones[i] = this.CoucheNeurones[i].Clone();
            }

            return clone;
        }

        /// <summary>
        /// ajoute un neurone à la couche
        /// </summary>
        /// <param name="fctActivation">fonction d'activation du neurone</param>
        public void AjoutNeurone(FctActivation_Type fctActivation)
        {
            List<Neurone> list = this.CoucheNeurones.ToList();
            list.Add(new Neurone(this.CoucheNeurones[0].Poids.Length, fctActivation));
            this.CoucheNeurones = list.ToArray();
        }

        /// <summary>
        /// ajoute une entrée à tous les neurones
        /// </summary>
        public void AjoutEntree()
        {
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                this.CoucheNeurones[i].AjoutEntree();
            }
        }

        /// <summary>
        /// supprime une entrée à tous les neurones
        /// </summary>
        public void SupprimerEntree()
        {
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                this.CoucheNeurones[i].SupprimerEntree();
            }
        }

        /// <summary>
        /// retourne le nombre de neurone dans la couche
        /// </summary>
        /// <returns></returns>
        public int nbrNeurone()
        {
            return this.CoucheNeurones.Length;
        }

        /// <summary>
        /// supprime un neurone spécific dans la couche
        /// </summary>
        /// <param name="numNeurone">numéro du neurone</param>
        public void SupprimerNeurone(int numNeurone)
        {
            List<Neurone> list = this.CoucheNeurones.ToList();
            list.RemoveAt(numNeurone);
            this.CoucheNeurones = list.ToArray();
        }

        /// <summary>
        /// supprime une entrée spécific à tous les neurones de la couche
        /// </summary>
        /// <param name="num">numéro de l'entrée</param>
        public void SupprimerEntree(int num)
        {
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                this.CoucheNeurones[i].SupprimerEntree(num);
            }
        }

        /// <summary>
        /// retourne la somme des poids pour l'entrée donnée
        /// </summary>
        /// <param name="numEntree">numéro de l'entrée</param>
        /// <returns>somme des poids</returns>
        public double SommePoidsEntree(int numEntree)
        {
            double somme = 0f;
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                somme += this.CoucheNeurones[i].Poids[numEntree];
            }

            return somme;
        }

        /// <summary>
        /// change la fonction d'activation de la couche
        /// </summary>
        /// <param name="fctActivation_Type">fct d'activation</param>
        public void ChangerFonctionActivation(FctActivation_Type fctActivation_Type)
        {
            for (int i = 0; i < this.CoucheNeurones.Length; i++)
            {
                this.CoucheNeurones[i].SetFonctionActivation(fctActivation_Type);
            }
        }
    }
}
