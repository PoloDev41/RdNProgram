using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RdN
{
    public enum FctActivation_Type
    {
        SIGMOIDE,
        TANGENTE_HYPER,
        GAUSSIENNE
    }

    /// <summary>
    /// classe d'un neurone
    /// </summary>
    [Serializable]
    public class Neurone
    {
        /// <summary>
        /// delegate pour la fonction d'activation
        /// </summary>
        /// <param name="x">x</param>
        /// <returns>valeur de retour</returns>
        public delegate double FonctionActivation_d(double x);

        /// <summary>
        /// type de la fonction d'activation
        /// </summary>
        public FctActivation_Type TypeFctActivation { get; private set; }

        /// <summary>
        /// fonction d'activation du neurone
        /// </summary>
        public FonctionActivation_d FctActivation { get; set; }

        /// <summary>
        /// dérivé de la fonction d'activation
        /// </summary>
        public FonctionActivation_d DerivFctAction { get; set; }

        /// <summary>
        /// generateur de nombre aléatoire
        /// </summary>
        private static Random Generateur = new Random();

        /// <summary>
        /// poids du neurone
        /// </summary>
        public double[] Poids { get; set; }

        /// <summary>
        /// sortie du neurone après la fonction d'activation
        /// </summary>
        public double Sortie { get; set; }

        /// <summary>
        /// Sortie du neurone avant la fonction d'activation
        /// </summary>
        public double SommePoids { get; set; }

        /// <summary>
        /// Valeur de retropopagation à appliquer pour l'apprentissage
        /// </summary>
        public double RetroValue { get; set; }

        /// <summary>
        /// offset du neurone
        /// </summary>
        public double Offset { get; set; }

        /// <summary>
        /// valeur de l'ancien offset
        /// </summary>
        public double AncienOffset { get; set; }

        /// <summary>
        /// ancien poids (utilisé pour l'inertie)
        /// </summary>
        public double[] AncienPoids { get; set; }

        /// <summary>
        /// retourne la dérivé sigmoide de la sortie (sans fonction d'activation)
        /// </summary>
        /// <returns></returns>
        public double GetDeriveSortie()
        {
            return this.DerivFctAction(SommePoids);
        }

        /// <summary>
        /// Cree un nouveau neurone
        /// </summary>
        /// <param name="NbrEntre">nombre d'entrée que le neurone attendra</param>
        /// <param name="fctActivation">fonction d'activation du neurone</param>
        public Neurone(int NbrEntre, FctActivation_Type fctActivation)
        {
            this.SetFonctionActivation(fctActivation);

            this.AncienOffset = 0f;
            this.AncienPoids = new double[NbrEntre];
            this.Poids = new double[NbrEntre];
            for (int i = 0; i < this.Poids.Length; i++)
            {
                this.AncienPoids[i] = 0;
                //this.Poids[i] = Generateur.NextDouble() * 2f - 1f;
                this.Poids[i] = 0;
            }
            this.Offset = Generateur.NextDouble();
            this.RetroValue = 0f;
            this.SommePoids = 0f;
            this.Sortie = 0f;
        }

        /// <summary>
        /// set la fonction d'activation et sa dérivé au neurone
        /// </summary>
        /// <param name="fctActivation">fonction activation</param>
        public void SetFonctionActivation(FctActivation_Type fctActivation)
        {
            this.TypeFctActivation = fctActivation;
            switch (fctActivation)
            {
                case FctActivation_Type.TANGENTE_HYPER:
                    this.FctActivation = Neurone.TangenteHyper;
                    this.DerivFctAction = Neurone.dTangenteHyper;
                    break;
                case FctActivation_Type.GAUSSIENNE:
                    this.FctActivation = Neurone.Gaussienne;
                    this.DerivFctAction = Neurone.dGaussienne;
                    break;
                case FctActivation_Type.SIGMOIDE:
                default:
                    this.FctActivation = Neurone.Sigmoide;
                    this.DerivFctAction = Neurone.dSigmoide;
                    break;
            }
        }

        /// <summary>
        /// cree un neurone vide
        /// </summary>
        private Neurone()
        {

        }

        /// <summary>
        /// calcul la sortie du neurone
        /// </summary>
        /// <param name="entrees">matrice d'entrées du neurone</param>
        /// <returns>valeur de la sortie après la fonction d'activation</returns>
        public double CalculSortie(double[] entrees)
        {
            if (entrees.Length != this.Poids.Length)
                throw new Exception("nombre d'entrées différentes de nombre de poids");

            double Somme = 0f;
            for (int i = 0; i < this.Poids.Length; i++)
            {
                Somme += this.Poids[i] * entrees[i];
            }
            Somme += this.Offset;

            this.SommePoids = Somme;
            this.Sortie = this.FctActivation(Somme);

            return Sortie;
        }

        /// <summary>
        /// corrige les poids par rapport à la valeur de retropropagation préalablement calculé
        /// </summary>
        /// <param name="entrees">entrees du neurone</param>
        public void CorrectionPoids(double[] entrees, double coeffApprentissage = .75f, double coeffInertie = .15f)
        {
            if (entrees.Length != this.Poids.Length)
                throw new Exception("nombre d'entrées différentes de nombre de poids");

            double nouveauPoids;
            for (int i = 0; i < this.Poids.Length; i++)
            {
                nouveauPoids = this.Poids[i] + coeffApprentissage * entrees[i] * this.RetroValue
                                    + coeffInertie * (this.Poids[i] - this.AncienPoids[i]); //ajout inertie
                this.AncienPoids[i] = this.Poids[i];
                this.Poids[i] = nouveauPoids;
            }
            nouveauPoids = this.Offset + coeffApprentissage * this.RetroValue +
                                    coeffInertie * (this.Offset - this.AncienOffset);
            this.AncienOffset = this.Offset;
            this.Offset = nouveauPoids;
        }

        public Neurone Clone()
        {
            Neurone clone = new Neurone();
            clone.SetFonctionActivation(this.TypeFctActivation);
            clone.Poids = new double[this.Poids.Length];
            clone.AncienPoids = new double[this.AncienPoids.Length];
            for (int i = 0; i < clone.Poids.Length; i++)
            {
                clone.Poids[i] = this.Poids[i];
                clone.AncienPoids[i] = this.AncienPoids[i];
            }
            clone.Sortie = this.Sortie;
            clone.SommePoids = this.SommePoids;
            clone.RetroValue = this.RetroValue;
            clone.Offset = this.Offset;
            clone.AncienOffset = this.AncienOffset;

            return clone;
        }

        /// <summary>
        /// ajoute une entrée au neurone
        /// </summary>
        public void AjoutEntree()
        {
            List<double> tmp = this.Poids.ToList();
            tmp.Add(Generateur.NextDouble() * 2f - 1f);
            this.Poids = tmp.ToArray();
            tmp = this.AncienPoids.ToList();
            tmp.Add(0);
            this.AncienPoids = tmp.ToArray();
        }

        /// <summary>
        /// supprime une entrée à un neurone
        /// </summary>
        public void SupprimerEntree()
        {
            this.SupprimerEntree(this.Poids.Length - 1);
        }

        /// <summary>
        /// supprime une entrée spécific
        /// </summary>
        /// <param name="num">numéro de l'entrée à supprimer</param>
        public void SupprimerEntree(int num)
        {
            List<double> tmp = this.Poids.ToList();
            tmp.RemoveAt(num);
            this.Poids = tmp.ToArray();
            tmp = this.AncienPoids.ToList();
            tmp.RemoveAt(num);
            this.AncienPoids = tmp.ToArray();
        }

        /// <summary>
        /// retourne la somme des poids du neurone
        /// </summary>
        /// <returns>somme des poids</returns>
        public double GetSommePoids()
        {
            double somme = 0;
            foreach (double item in this.Poids)
            {
                somme += item;
            }
            return somme;
        }

        /// <summary>
        /// calcul la sigmoide de la valeur donnée
        /// </summary>
        /// <param name="x">valeur</param>
        /// <returns>sortie</returns>
        public static double Sigmoide(double x)
        {
            if (x > 45)
                return 1;
            else if (x < -45)
                return 0;

            return (1 / (1 + Math.Exp(-x)));
        }

        /// <summary>
        /// calcul la dérivé de la sigmoide de la valeur donnée
        /// </summary>
        /// <param name="x">valeur</param>
        /// <returns>sortie</returns>
        public static double dSigmoide(double x)
        {
            double v = Sigmoide(x);
            return v * (1 - v);
        }

        /// <summary>
        /// retourne la tangente hyperbolique du nombre
        /// </summary>
        /// <param name="x">x</param>
        /// <returns>sortie</returns>
        public static double TangenteHyper(double x)
        {
            if (x > 45)
                return 1;
            else if (x < -45)
                return -1;

            double exp = Math.Exp(-2 * x);

            return (1f - exp) / (1f + exp);
        }

        /// <summary>
        /// retourne la dérivé de la tangente hyperbolique du nombre
        /// </summary>
        /// <param name="x">x</param>
        /// <returns>sortie</returns>
        public static double dTangenteHyper(double x)
        {
            double tmp = TangenteHyper(x);
            return 1 - (tmp * tmp);
        }

        /// <summary>
        /// retourne la Gaussienne du nombre
        /// </summary>
        /// <param name="x">x</param>
        /// <returns>sortie</returns>
        public static double Gaussienne(double x)
        {
            if (x > 45)
                return 0;
            else if (x < -45)
                return 0;

            double exp = Math.Exp(-(x * x / 2f));

            return exp;
        }

        /// <summary>
        /// retourne la dérivé de la gaussienne du nombre
        /// </summary>
        /// <param name="x">x</param>
        /// <returns>sortie</returns>
        public static double dGaussienne(double x)
        {
            double tmp = Gaussienne(x);
            return -x * tmp;
        }

        /// <summary>
        /// génère une fonction d'activation au hasard
        /// </summary>
        /// <returns>fonction d'activation</returns>
        public static FctActivation_Type RandomFctActivation()
        {
            int num = Neurone.Generateur.Next(0, 3);
            switch (num)
            {
                case(0):
                    return FctActivation_Type.TANGENTE_HYPER;
                case(1):
                    return FctActivation_Type.GAUSSIENNE;
                default:
                    return FctActivation_Type.SIGMOIDE;
            }
        }
    }
}
